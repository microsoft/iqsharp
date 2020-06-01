// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Azure.Quantum.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        // TODO: Factor compilation and EntryPoint-related properties and code to a separate class.
        private ICompilerService Compiler { get; }
        private IOperationResolver OperationResolver { get; }
        private IWorkspace Workspace { get; }
        private ISnippets Snippets { get; }
        private IReferences References { get; }
        private ILogger<AzureClient> Logger { get; }
        private Lazy<CompilerMetadata> CompilerMetadata { get; set; }
        private AssemblyInfo EntryPointAssembly { get; set; } = new AssemblyInfo(null);
        private string ConnectionString { get; set; } = string.Empty;
        private AzureExecutionTarget? ActiveTarget { get; set; }
        private AuthenticationResult? AuthenticationResult { get; set; }
        private IQuantumClient? QuantumClient { get; set; }
        private Azure.Quantum.IWorkspace? ActiveWorkspace { get; set; }
        private string MostRecentJobId { get; set; } = string.Empty;
        private IPage<ProviderStatus>? AvailableProviders { get; set; }
        private IEnumerable<TargetStatus>? AvailableTargets { get => AvailableProviders?.SelectMany(provider => provider.Targets); }
        private IEnumerable<TargetStatus>? ValidExecutionTargets { get => AvailableTargets?.Where(target => AzureExecutionTarget.IsValid(target.Id)); }
        private string ValidExecutionTargetsDisplayText
        {
            get => ValidExecutionTargets == null
                ? "(no execution targets available)"
                : string.Join(", ", ValidExecutionTargets.Select(target => target.Id));
        }

        public AzureClient(
            ICompilerService compiler,
            IOperationResolver operationResolver,
            IWorkspace workspace,
            ISnippets snippets,
            IReferences references,
            ILogger<AzureClient> logger,
            IEventService eventService)
        {
            Compiler = compiler;
            OperationResolver = operationResolver;
            Workspace = workspace;
            Snippets = snippets;
            References = references;
            Logger = logger;
            CompilerMetadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);

            Workspace.Reloaded += OnWorkspaceReloaded;
            References.PackageLoaded += OnGlobalReferencesPackageLoaded;

            AssemblyLoadContext.Default.Resolving += Resolve;

            eventService?.TriggerServiceInitialized<IAzureClient>(this);
        }

        private void OnGlobalReferencesPackageLoaded(object sender, PackageLoadedEventArgs e) =>
            CompilerMetadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);

        private void OnWorkspaceReloaded(object sender, ReloadedEventArgs e) =>
            CompilerMetadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);

        private CompilerMetadata LoadCompilerMetadata() =>
            Workspace.HasErrors
                    ? References?.CompilerMetadata.WithAssemblies(Snippets.AssemblyInfo)
                    : References?.CompilerMetadata.WithAssemblies(Snippets.AssemblyInfo, Workspace.AssemblyInfo);

        /// <summary>
        /// Because the assemblies are loaded into memory, we need to provide this method to the AssemblyLoadContext
        /// such that the Workspace assembly or this assembly is correctly resolved when it is executed for simulation.
        /// </summary>
        public Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            if (name.Name == Path.GetFileNameWithoutExtension(EntryPointAssembly?.Location))
            {
                return EntryPointAssembly.Assembly;
            }
            if (name.Name == Path.GetFileNameWithoutExtension(Snippets?.AssemblyInfo?.Location))
            {
                return Snippets.AssemblyInfo.Assembly;
            }
            else if (name.Name == Path.GetFileNameWithoutExtension(Workspace?.AssemblyInfo?.Location))
            {
                return Workspace.AssemblyInfo.Assembly;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool refreshCredentials = false)
        {
            ConnectionString = storageAccountConnectionString;

            var azureEnvironmentEnvVarName = "AZURE_QUANTUM_ENV";
            var azureEnvironmentName = System.Environment.GetEnvironmentVariable(azureEnvironmentEnvVarName);
            var azureEnvironment = AzureEnvironment.Create(azureEnvironmentName, subscriptionId);

            var msalApp = PublicClientApplicationBuilder
                .Create(azureEnvironment.ClientId)
                .WithAuthority(azureEnvironment.Authority)
                .Build();

            // Register the token cache for serialization
            var cacheFileName = "aad.bin";
            var cacheDirectoryEnvVarName = "AZURE_QUANTUM_TOKEN_CACHE";
            var cacheDirectory = System.Environment.GetEnvironmentVariable(cacheDirectoryEnvVarName);
            if (string.IsNullOrEmpty(cacheDirectory))
            {
                cacheDirectory = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".azure-quantum");
            }

            var storageCreationProperties = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory, azureEnvironment.ClientId).Build();
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
            cacheHelper.RegisterCache(msalApp.UserTokenCache);

            bool shouldShowLoginPrompt = refreshCredentials;
            if (!shouldShowLoginPrompt)
            {
                try
                {
                    var accounts = await msalApp.GetAccountsAsync();
                    AuthenticationResult = await msalApp.AcquireTokenSilent(
                        azureEnvironment.Scopes, accounts.FirstOrDefault()).WithAuthority(msalApp.Authority).ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    shouldShowLoginPrompt = true;
                }
            }

            if (shouldShowLoginPrompt)
            {
                AuthenticationResult = await msalApp.AcquireTokenWithDeviceCode(
                    azureEnvironment.Scopes,
                    deviceCodeResult =>
                    {
                        channel.Stdout(deviceCodeResult.Message);
                        return Task.FromResult(0);
                    }).WithAuthority(msalApp.Authority).ExecuteAsync();
            }

            if (AuthenticationResult == null)
            {
                return AzureClientError.AuthenticationFailed.ToExecutionResult();
            }

            var credentials = new Rest.TokenCredentials(AuthenticationResult.AccessToken);
            QuantumClient = new QuantumClient(credentials)
            {
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                WorkspaceName = workspaceName,
                BaseUri = azureEnvironment.BaseUri,
            };
            ActiveWorkspace = new Azure.Quantum.Workspace(
                QuantumClient.SubscriptionId,
                QuantumClient.ResourceGroupName,
                QuantumClient.WorkspaceName,
                AuthenticationResult?.AccessToken,
                azureEnvironment.BaseUri);

            try
            {
                AvailableProviders = await QuantumClient.Providers.GetStatusAsync();
            }
            catch (Exception e)
            {
                channel.Stderr(e.ToString());
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            channel.Stdout($"Connected to Azure Quantum workspace {QuantumClient.WorkspaceName}.");

            // TODO: Add encoder for IEnumerable<TargetStatus> rather than calling ToJupyterTable() here directly.
            return ValidExecutionTargets.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel)
        {
            if (QuantumClient == null || AvailableProviders == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            channel.Stdout($"Connected to Azure Quantum workspace {QuantumClient.WorkspaceName}.");

            // TODO: Add encoder for IEnumerable<TargetStatus> rather than calling ToJupyterTable() here directly.
            return ValidExecutionTargets.ToJupyterTable().ToExecutionResult();
        }

        private async Task<ExecutionResult> SubmitOrExecuteJobAsync(IChannel channel, string operationName, Dictionary<string, string> inputParameters, bool execute)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before submitting a job.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel.Stderr("Please call %azure.target before submitting a job.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(operationName))
            {
                var commandName = execute ? "%azure.execute" : "%azure.submit";
                channel.Stderr($"Please pass a valid Q# operation name to {commandName}.");
                return AzureClientError.NoOperationName.ToExecutionResult();
            }

            var machine = QuantumMachineFactory.CreateMachine(ActiveWorkspace, ActiveTarget.TargetName, ConnectionString);
            if (machine == null)
            {
                // We should never get here, since ActiveTarget should have already been validated at the time it was set.
                channel.Stderr($"Unexpected error while preparing job for execution on target {ActiveTarget.TargetName}.");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // TODO: Factor compilation and EntryPoint-related properties and code to a separate class.
            var operationInfo = OperationResolver.Resolve(operationName);
            var logger = new QSharpLogger(Logger);
            EntryPointAssembly = Compiler.BuildEntryPoint(operationInfo, CompilerMetadata.Value, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__.dll"));
            var entryPointOperationInfo = EntryPointAssembly.Operations.Single();

            // TODO: Need these two lines to construct the Type objects correctly.
            Type entryPointInputType = entryPointOperationInfo.RoslynParameters.Select(p => p.ParameterType).DefaultIfEmpty(typeof(QVoid)).First(); // .Header.Signature.ArgumentType.GetType();
            Type entryPointOutputType = typeof(Result); // entryPointOperationInfo.Header.Signature.ReturnType.GetType();

            var entryPointInputOutputTypes = new Type[] { entryPointInputType, entryPointOutputType };
            Type entryPointInfoType = typeof(EntryPointInfo<,>).MakeGenericType(entryPointInputOutputTypes);
            var entryPointInfo = entryPointInfoType.GetConstructor(
                new Type[] { typeof(Type) }).Invoke(new object[] { entryPointOperationInfo.RoslynType });

            var typedParameters = new List<object>();
            foreach (var parameter in entryPointOperationInfo.RoslynParameters)
            {
                typedParameters.Add(System.Convert.ChangeType(inputParameters.DecodeParameter<string>(parameter.Name), parameter.ParameterType));
            }

            // TODO: Need to use all of the typed parameters, not just the first one.
            var entryPointInput = typedParameters.DefaultIfEmpty(QVoid.Instance).First();

            channel.Stdout($"Submitting {operationName} to target {ActiveTarget.TargetName}...");

            var method = typeof(IQuantumMachine).GetMethod("SubmitAsync").MakeGenericMethod(entryPointInputOutputTypes);
            var job = await (method.Invoke(machine, new object[] { entryPointInfo, entryPointInput }) as Task<IQuantumMachineJob>);
            MostRecentJobId = job.Id;
            channel.Stdout("Job submission successful.");
            channel.Stdout($"To check the status, run:\n    %azure.status {MostRecentJobId}");
            channel.Stdout($"To see the results, run:\n    %azure.output {MostRecentJobId}");

            //if (execute)
            //{
            //    // TODO: wait for job completion
            //}

            // TODO: Add encoder for IQuantumMachineJob rather than calling ToJupyterTable() here.
            return job.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(IChannel channel, string operationName, Dictionary<string, string> inputParameters) =>
            await SubmitOrExecuteJobAsync(channel, operationName, inputParameters, execute: false);

        /// <inheritdoc/>
        public async Task<ExecutionResult> ExecuteJobAsync(IChannel channel, string operationName, Dictionary<string, string> inputParameters) =>
            await SubmitOrExecuteJobAsync(channel, operationName, inputParameters, execute: true);

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetActiveTargetAsync(IChannel channel)
        {
            if (AvailableProviders == null)
            {
                channel.Stderr("Please call %azure.connect before getting the execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel.Stderr("No execution target has been specified. To specify one, run:\n%azure.target <target name>");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            channel.Stdout($"Current execution target: {ActiveTarget.TargetName}");
            channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
            return ActiveTarget.TargetName.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetName)
        {
            if (AvailableProviders == null)
            {
                channel.Stderr("Please call %azure.connect before setting an execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            // Validate that this target name is valid in the workspace.
            if (!AvailableTargets.Any(target => targetName == target.Id))
            {
                channel.Stderr($"Target name {targetName} is not available in the current Azure Quantum workspace.");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Validate that we know which package to load for this target name.
            var executionTarget = AzureExecutionTarget.Create(targetName);
            if (executionTarget == null)
            {
                channel.Stderr($"Target name {targetName} does not support executing Q# jobs.");
                channel.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Set the active target and load the package.
            ActiveTarget = executionTarget;
            await References.AddPackage(ActiveTarget.PackageName);

            return $"Active target is now {ActiveTarget.TargetName}".ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before getting job results.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(jobId))
            {
                if (string.IsNullOrEmpty(MostRecentJobId))
                {
                    channel.Stderr("No job ID was specified. Please submit a job first or specify a job ID.");
                    return AzureClientError.JobNotFound.ToExecutionResult();
                }

                jobId = MostRecentJobId;
            }

            var job = ActiveWorkspace.GetJob(jobId);
            if (job == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            if (!job.Succeeded || string.IsNullOrEmpty(job.Details.OutputDataUri))
            {
                channel.Stderr($"Job ID {jobId} has not completed. Displaying the status instead.");
                // TODO: Add encoder for CloudJob rather than calling ToJupyterTable() here directly.
                return job.Details.ToJupyterTable().ToExecutionResult();
            }

            var stream = new MemoryStream();
            await new JobStorageHelper(ConnectionString).DownloadJobOutputAsync(jobId, stream);
            stream.Seek(0, SeekOrigin.Begin);
            var output = new StreamReader(stream).ReadToEnd();
            var deserializedOutput = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(output);
            var histogram = new Dictionary<string, double>();
            foreach (var entry in deserializedOutput["histogram"] as JObject)
            {
                histogram[entry.Key] = entry.Value.ToObject<double>();
            }

            // TODO: Add encoder to visualize IEnumerable<KeyValuePair<string, double>>
            return histogram.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobStatusAsync(IChannel channel, string jobId)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(jobId))
            {
                if (string.IsNullOrEmpty(MostRecentJobId))
                {
                    channel.Stderr("No job ID was specified. Please submit a job first or specify a job ID.");
                    return AzureClientError.JobNotFound.ToExecutionResult();
                }

                jobId = MostRecentJobId;
            }

            var job = ActiveWorkspace.GetJob(jobId);
            if (job == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            // TODO: Add encoder for CloudJob rather than calling ToJupyterTable() here directly.
            return job.Details.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobListAsync(IChannel channel)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Please call %azure.connect before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobs = ActiveWorkspace.ListJobs();
            if (jobs == null || jobs.Count() == 0)
            {
                channel.Stderr("No jobs found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            // TODO: Add encoder for IEnumerable<CloudJob> rather than calling ToJupyterTable() here directly.
            return jobs.Select(job => job.Details).ToJupyterTable().ToExecutionResult();
        }
    }
}
