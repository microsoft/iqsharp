// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

using Azure.Core;
using Azure.Quantum;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Common;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Supported output data formats for QIR.
    /// </summary>
    internal static class OutputFormat
    {
        public const string QirResultsV1 = "microsoft.qir-results.v1";

        public const string QuantumResultsV1 = "microsoft.quantum-results.v1";

        public const string ResourceEstimatesV1 = "microsoft.resource-estimates.v1";
    }

    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        private const string MicrosoftSimulator = "microsoft.simulator";

        private const string MicrosoftEstimator = "microsoft.estimator";

        // ToDo: Use API provided by the Service, GitHub Issue: https://github.com/microsoft/iqsharp/issues/681 
        /// <summary>
        /// Returns whether a target ID is meant for quantum execution since not all targets
        /// exposed by providers are meant for that, such as QIO targets. More
        /// specifically, the Microsoft provider exposes targets that are not meant for
        /// quantum execution and the only ones meant for that start with "microsoft.simulator".
        /// </summary>
        private static bool IsQuantumExecutionTarget(string targetId) =>
            AzureExecutionTarget.GetProvider(targetId) != AzureProvider.Microsoft
            || targetId.StartsWith(MicrosoftSimulator)
            || targetId.StartsWith(MicrosoftEstimator);

        /// <inheritdoc />
        public Microsoft.Azure.Quantum.IWorkspace? ActiveWorkspace { get; private set; }
        /// <inheritdoc />
        public AzureExecutionTarget? ActiveTarget { get; private set; }
        /// <inheritdoc />
        public TargetCapability TargetCapability { get; private set; } = TargetCapabilityModule.Top;
        private TokenCredential? Credential { get; set; }
        private ILogger<AzureClient> Logger { get; }
        private IReferences References { get; }
        private IEntryPointGenerator EntryPointGenerator { get; }
        private IMetadataController MetadataController { get; }
        private IAzureFactory AzureFactory { get; }
        private readonly IWorkspace Workspace;
        private bool IsPythonUserAgent => MetadataController?.UserAgent?.StartsWith("qsharp.py") ?? false;
        private string GetCommandDisplayName(string name) => MetadataController?.CommandDisplayName(name) ?? name;
        private string StorageConnectionString { get; set; } = string.Empty;
        private string MostRecentJobId { get; set; } = string.Empty;
        private IEnumerable<ProviderStatusInfo>? AvailableProviders { get; set; }
        private IEnumerable<TargetStatusInfo>? AvailableTargets =>
            AvailableProviders
            ?.SelectMany(provider => provider.Targets)
            ?.Where(t => t.TargetId != null && IsQuantumExecutionTarget(t.TargetId));
        private IEnumerable<TargetStatusInfo>? ValidExecutionTargets => AvailableTargets?.Where(AzureExecutionTarget.IsValid);
        private string ValidExecutionTargetsDisplayText =>
            (ValidExecutionTargets == null || ValidExecutionTargets.Count() == 0)
            ? "(no quantum computing execution targets available)"
            : string.Join(", ", ValidExecutionTargets.Select(target => target.TargetId));
        private IServiceProvider ServiceProvider { get; }
        private IConfigurationSource ConfigurationSource { get; }

        /// <summary>
        /// Creates an <see cref="AzureClient"/> object that provides methods for
        /// interacting with an Azure Quantum workspace.
        /// </summary>
        /// <param name="engine">The execution engine for interaction with Jupyter.</param>
        /// <param name="references">The references to use when compiling Q# code.</param>
        /// <param name="entryPointGenerator">The generator of entry points for Azure Quantum execution.</param>
        /// <param name="metadataController">The metadata controller to use when compiling Q# code.</param>
        /// <param name="azureFactory">A Factory class to create instance of Azure Quantum classes.</param>
        /// <param name="logger">The logger to use for diagnostic information.</param>
        /// <param name="eventService">The event service for the IQ# kernel.</param>
        /// <param name="workspace">The service for the active IQ# workspace.</param>
        /// <param name="serviceProvider">A service provider to create needed components.</param>
        /// <param name="configurationSource">The service for configuration data.</param>
        public AzureClient(
            IExecutionEngine engine,
            IReferences references,
            IEntryPointGenerator entryPointGenerator,
            IMetadataController metadataController,
            IAzureFactory azureFactory,
            ILogger<AzureClient> logger,
            IEventService eventService,
            IWorkspace workspace,
            IServiceProvider serviceProvider,
            IConfigurationSource configurationSource)
        {
            References = references;
            EntryPointGenerator = entryPointGenerator;
            MetadataController = metadataController;
            AzureFactory = azureFactory;
            Logger = logger;
            Workspace = workspace;
            ServiceProvider = serviceProvider;
            ConfigurationSource = configurationSource;

            // If we're given a target capability, start with it set.
            if (workspace.WorkspaceProject.TargetCapability is {} capability)
            {
                if (!TrySetTargetCapability(null, capability, out _))
                {
                    logger.LogWarning("Could not set target capability level {Level} from workspace project.", capability);
                }
            }

            if (engine is BaseEngine baseEngine)
            {
                baseEngine.RegisterDisplayEncoder(new CloudJobToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new CloudJobToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new TargetStatusToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new TargetStatusToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new HistogramToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new HistogramToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new AzureClientErrorToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new AzureClientErrorToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new DeviceCodeResultToHtmlEncoder());
                baseEngine.RegisterDisplayEncoder(new DeviceCodeResultToTextEncoder());
                baseEngine.RegisterDisplayEncoder(new ResourceEstimationToHtmlEncoder(configurationSource, logger));
                baseEngine.RegisterDisplayEncoder(ActivatorUtilities.CreateInstance<ResourceEstimationToHtmlEncoder>(ServiceProvider));
            }

            eventService?.TriggerServiceInitialized<IAzureClient>(this);
        }

        /// <inheritdoc />
        public string? ActiveTargetId => ActiveTarget?.TargetId;

        /// <inheritdoc/>
        public event EventHandler<ConnectToWorkspaceEventArgs>? ConnectToWorkspace;

        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(IChannel? channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            string location,
            CredentialType credentialType,
            CancellationToken? cancellationToken = null)
        {

            var duration = Stopwatch.StartNew();
            ExecutionResult? result = null;

            try
            {
                // Capture the console output, specifically for the case the user is trying to use DeviceCode credentials
                // so they can get the message for auth.
                var currentOut = channel?.CaptureConsole();
                try
                {
                    var credential = CredentialFactory.CreateCredential(credentialType, subscriptionId);

                    var connectionResult = await ConnectToWorkspaceAsync(channel, subscriptionId, resourceGroupName, workspaceName, location, credential);
                    if (connectionResult.Status != ExecuteStatus.Ok)
                    {
                        result = connectionResult;
                        return result.Value;
                    }

                    if (ActiveWorkspace == null)
                    {
                        result = AzureClientError.WorkspaceNotFound.ToExecutionResult();
                        return result.Value;
                    }

                    Credential = credential;
                }
                finally
                {
                    System.Console.SetOut(currentOut);
                }

                StorageConnectionString = storageAccountConnectionString;
                ActiveTarget = null;
                MostRecentJobId = string.Empty;

                channel?.Stdout($"Connected to Azure Quantum workspace {ActiveWorkspace.WorkspaceName} in location {ActiveWorkspace.Location}.");

                var targets = ValidExecutionTargets ?? Enumerable.Empty<TargetStatusInfo>();
                if (targets.Count() == 0)
                {
                    channel?.Stderr($"No valid quantum computing execution targets found in Azure Quantum workspace {ActiveWorkspace.WorkspaceName}.");
                }

                result = targets.ToExecutionResult();

                // If the workspace project has an active target, set it now.
                if (Workspace.WorkspaceProject.TargetId is {} targetId)
                {
                    var targetResult = await SetActiveTargetAsync(channel, targetId, cancellationToken);
                    if (targetResult.Status == ExecuteStatus.Ok)
                    {
                        // Try to set the target capability as well.
                        if (Workspace.WorkspaceProject.TargetCapability is {} capabilityName)
                        {
                            if (TrySetTargetCapability(channel, capabilityName, out _))
                            {
                                return result.Value;
                            }
                            else
                            {
                                return ExecuteStatus.Error.ToExecutionResult();
                            }

                        }
                        else
                        {
                            return result.Value;
                        }
                    }
                    else
                    {
                        return targetResult;
                    }
                }

                return result.Value;
            }
            finally
            {
                duration.Stop();

                ExecuteStatus status = result?.Status ?? ExecuteStatus.Error;
                AzureClientError? error = result?.Output as AzureClientError?;
                bool useCustomStorage = !string.IsNullOrWhiteSpace(StorageConnectionString);
                
                ConnectToWorkspace?.Invoke(this, new ConnectToWorkspaceEventArgs(status, error, location, useCustomStorage, credentialType, duration.Elapsed));
            }
        }

        private string GetNormalizedLocation(string location, IChannel? channel)
        {
            // Convert user-provided location into names recognized by Azure resource manager.
            // For example, a customer-provided value of "West US" should be converted to "westus".
            var normalizedLocation = location.ToLowerInvariant().Replace(" ", "");
            if (UriHostNameType.Unknown == Uri.CheckHostName(normalizedLocation))
            {
                channel?.Stderr($"Invalid location {location} specified.");
                return string.Empty;
            }

            return normalizedLocation;
        }

        private async Task<ExecutionResult> ConnectToWorkspaceAsync(IChannel? channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string location,
            TokenCredential credential,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                channel?.Stderr($"No location provided.");
                return AzureClientError.NoWorkspaceLocation.ToExecutionResult();
            }

            location = GetNormalizedLocation(location, channel);
            if (string.IsNullOrWhiteSpace(location))
            {
                return AzureClientError.InvalidWorkspaceLocation.ToExecutionResult();
            }

            try
            {
                var options = new QuantumJobClientOptions();

                // This value will be added as a prefix in the UserAgent when
                // calling the Azure Quantum APIs
                options.Diagnostics.ApplicationId = IsPythonUserAgent ? "IQ#/Py" : "IQ#";

                var workspace = AzureFactory.CreateWorkspace(
                    subscriptionId: subscriptionId,
                    resourceGroup: resourceGroupName,
                    workspaceName: workspaceName,
                    location: location,
                    credential: credential,
                    options: options);

                var providers = new List<ProviderStatusInfo>();
                var status = workspace.ListProvidersStatusAsync(cancellationToken);
                await foreach (var s in status)
                {
                    providers.Add(s);
                }

                ActiveWorkspace = workspace;
                AvailableProviders = providers;

                Logger.LogDebug("Connected to workspace with {NProviders} available providers.", providers.Count);

                return ExecuteStatus.Ok.ToExecutionResult();
            }
            catch (TaskCanceledException tce)
            {
                throw tce;
            }
            catch (Exception e)
            {
                var msg = $"The Azure Quantum workspace {workspaceName} in location {location} could not be reached.";
                Logger.LogError(e, msg);
                channel?.Stderr($"{msg} Please check the provided parameters and try again.");
                channel?.Stderr($"Error details:\n\n{e.Message}");

                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }
        }

        // NB: Cancellation tokens are required here to ensure that we always
        //     correctly propagate cooperative cancellation. This would be a
        //     bad public API, since we would want external callers to be able
        //     to opt-in to cooperative cancellation, but since this is a
        //     private method, we make it required here.
        private async Task<ExecutionResult> RefreshConnectionAsync(IChannel? channel, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Refreshing Azure connection.");
            if (ActiveWorkspace == null || Credential == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            return await ConnectToWorkspaceAsync(
                channel,
                ActiveWorkspace.SubscriptionId ?? string.Empty,
                ActiveWorkspace.ResourceGroupName ?? string.Empty,
                ActiveWorkspace.WorkspaceName ?? string.Empty,
                ActiveWorkspace.Location ?? string.Empty,
                Credential,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel? channel, CancellationToken? cancellationToken = default)
        {
            if (ActiveWorkspace == null || AvailableProviders == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            channel?.Stdout($"Connected to Azure Quantum workspace {ActiveWorkspace.WorkspaceName} in location {ActiveWorkspace.Location}.");

            return ValidExecutionTargets.ToExecutionResult();
        }

        private async Task<ExecutionResult> SubmitOrExecuteJobAsync(
            IChannel? channel,
            AzureSubmissionContext submissionContext,
            bool execute,
            CancellationToken cancellationToken)
        {
            if (ActiveWorkspace == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before submitting a job.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget?.TargetId == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("target")} before submitting a job.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(submissionContext.OperationName))
            {
                channel?.Stderr($"Please pass a valid Q# operation name to {GetCommandDisplayName(execute ? "execute" : "submit")}.");
                return AzureClientError.NoOperationName.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            
            // QirSubmitter and CreateMachine have return types with different base types
            // but both have a SubmitAsync method that returns an IQuantumMachineJob.
            // Thus, we can branch on whether we need a QIR submitter or a translator,
            // but can use the same task object to represent both return values.
            Func<IEntryPoint, Task<IQuantumMachineJob>>? jobTask = null;
            if (this.ActiveTarget.TryGetQirSubmitter(this.ActiveWorkspace, this.StorageConnectionString, this.TargetCapability, out var submitter))
            {
                Logger?.LogDebug("Using QIR submitter for target {Target} and capability {Capability}.", this.ActiveTarget, this.TargetCapability);
                jobTask = entryPoint => entryPoint.SubmitAsync(submitter, submissionContext);
            }
            else if (AzureFactory.CreateMachine(this.ActiveWorkspace, this.ActiveTarget.TargetId, this.StorageConnectionString) is IQuantumMachine machine)
            {
                Logger?.LogDebug("Using legacy submitter for target {Target} and capability {Capability}.", this.ActiveTarget, this.TargetCapability);
                jobTask = entryPoint => entryPoint.SubmitAsync(machine, submissionContext);
            }
            else
            {
                // We should never get here, since ActiveTarget should have already been validated at the time it was set.
                channel?.Stderr($"Unexpected error while preparing job for execution on target {ActiveTarget.TargetId}.");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            IEntryPoint? entryPoint;
            try
            {
                entryPoint = await EntryPointGenerator.Generate(
                    submissionContext.OperationName, ActiveTarget.TargetId, this.TargetCapability
                );
            }
            catch (TaskCanceledException tce)
            {
                throw tce;
            }
            catch (UnsupportedOperationException)
            {
                channel?.Stderr($"{submissionContext.OperationName} is not a recognized Q# operation name.");
                return AzureClientError.UnrecognizedOperationName.ToExecutionResult();
            }
            catch (CompilationErrorsException e)
            {
                e.Log(channel, Logger, $"The Q# operation {submissionContext.OperationName} could not be compiled as an entry point for job execution.");
                foreach (var message in e.Errors) channel?.Stderr(message);
                return AzureClientError.InvalidEntryPoint.ToExecutionResult();
            }

            channel?.Stdout($"Submitting {submissionContext.OperationName} to target {ActiveTarget.TargetId}...");

            try
            {
                Logger.LogDebug("About to submit entry point for {OperationName}.", submissionContext.OperationName);
                var job = await jobTask(entryPoint);
                channel?.Stdout($"Job successfully submitted.");
                channel?.Stdout($"   Job name: {submissionContext.FriendlyName}");
                channel?.Stdout($"   Job ID: {job.Id}");
                MostRecentJobId = job.Id;
            }
            catch (TaskCanceledException tce)
            {
                throw tce;
            }
            catch (ArgumentException e)
            {
                var msg = $"Failed to parse all expected parameters for Q# operation {submissionContext.OperationName}.";
                Logger.LogError(e, msg);

                channel?.Stderr(msg);
                channel?.Stderr(e.Message);
                return AzureClientError.JobSubmissionFailed.ToExecutionResult();
            }
            catch (Exception e)
            {
                var msg = $"Failed to submit Q# operation {submissionContext.OperationName} for execution.";
                Logger.LogError(e, msg);
                channel?.Stderr(msg);
                channel?.Stderr(e.InnerException?.Message ?? e.Message);
                #if DEBUG
                    channel?.Stderr("Stack trace:\n" + e.StackTrace);
                #endif
                return AzureClientError.JobSubmissionFailed.ToExecutionResult();
            }

            // If the command was not %azure.execute, simply return the job status.
            if (!execute)
            {
                return await GetJobStatusAsync(channel, MostRecentJobId);
            }

            // If the command was %azure.execute, wait for the job to complete and return the job output.
            channel?.Stdout($"Waiting up to {submissionContext.ExecutionTimeout} seconds for Azure Quantum job to complete...");

            using var executionTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(submissionContext.ExecutionTimeout));
            using var executionCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(executionTimeoutTokenSource.Token, cancellationToken);
            {
                try
                {
                    CloudJob? cloudJob = null;
                    while (cloudJob == null || cloudJob.InProgress)
                    {
                        executionCancellationTokenSource.Token.ThrowIfCancellationRequested();
                        await Task.Delay(TimeSpan.FromSeconds(submissionContext.ExecutionPollingInterval), executionCancellationTokenSource.Token);
                        cloudJob = await ActiveWorkspace.GetJobAsync(MostRecentJobId, executionTimeoutTokenSource.Token);
                        channel?.Stdout($"[{DateTime.Now.ToLongTimeString()}] Current job status: {cloudJob?.Status ?? "Unknown"}");
                    }
                }
                catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException)
                {
                    Logger?.LogInformation($"Operation canceled while waiting for job execution to complete: {e.Message}");
                }
                catch (Exception e)
                {
                    channel?.Stderr($"Unexpected error while waiting for the results of the Q# operation.");
                    channel?.Stderr(e.InnerException?.Message ?? e.Message);
                    return AzureClientError.JobSubmissionFailed.ToExecutionResult();
                }
            }

            return await GetJobResultAsync(channel, MostRecentJobId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? cancellationToken = null) =>
            await SubmitOrExecuteJobAsync(channel, submissionContext, execute: false, cancellationToken ?? CancellationToken.None);

        /// <inheritdoc/>
        public async Task<ExecutionResult> ExecuteJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? cancellationToken = null) =>
            await SubmitOrExecuteJobAsync(channel, submissionContext, execute: true, cancellationToken ?? CancellationToken.None);

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetActiveTargetAsync(IChannel channel, CancellationToken? cancellationToken = default)
        {
            if (AvailableProviders == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before getting the execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTarget == null)
            {
                channel?.Stderr($"No execution target has been specified. To specify one, call {GetCommandDisplayName("target")} with the target ID.");
                channel?.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            channel?.Stdout($"Current execution target: {ActiveTarget.TargetId}");
            channel?.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");

            return AvailableTargets.First(target => target.TargetId == ActiveTarget.TargetId).ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SetActiveTargetAsync(IChannel? channel, string targetId, CancellationToken? cancellationToken = default)
        {
            if (ActiveWorkspace == null || AvailableProviders == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before setting an execution target.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            // Validate that this target is valid in the workspace.
            var target = AvailableTargets.FirstOrDefault(t => targetId == t.TargetId);
            if (target == null)
            {
                channel?.Stderr($"Target {targetId} is not available in the current Azure Quantum workspace.");
                channel?.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Validate that we know which package to load for this target.
            var executionTarget = AzureExecutionTarget.Create(target);
            if (executionTarget == null)
            {
                channel?.Stderr($"Target {targetId} does not support executing Q# jobs.");
                channel?.Stdout($"Available execution targets: {ValidExecutionTargetsDisplayText}");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            // Set the active target and load the package.
            ActiveTarget = executionTarget;
            TargetCapability = executionTarget.DefaultCapability;

            channel?.Stdout($"Loading package {ActiveTarget.PackageName} and dependencies...");
            await References.AddPackage(ActiveTarget.PackageName);

            channel?.Stdout($"Active target is now {ActiveTarget.TargetId}");

            return AvailableTargets.First(target => target.TargetId == ActiveTarget.TargetId).ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobResultAsync(IChannel? channel, string? jobId, CancellationToken? cancellationToken = default)
        {
            if (ActiveWorkspace == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before getting job results.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(jobId))
            {
                if (string.IsNullOrEmpty(MostRecentJobId))
                {
                    channel?.Stderr("No job ID was specified. Please submit a job first or specify a job ID.");
                    return AzureClientError.JobNotFound.ToExecutionResult();
                }

                jobId = MostRecentJobId;
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var job = await ActiveWorkspace.GetJobAsync(jobId, cancellationToken ?? CancellationToken.None);
            if (job == null)
            {
                channel?.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            if (job.InProgress)
            {
                channel?.Stderr($"Job ID {jobId} has not completed. To check the status, call {GetCommandDisplayName("status")} with the job ID.");
                return AzureClientError.JobNotCompleted.ToExecutionResult();
            }
            else if (job.Failed)
            {
                channel?.Stderr($"Job ID {jobId} failed or was cancelled with the message: {job.Details.ErrorData.Message}");
                return AzureClientError.JobFailedOrCancelled.ToExecutionResult();
            }

            try
            {
                return await CreateOutput(job, channel, cancellationToken ?? default);
            }
            catch (Exception e)
            {
                channel?.Stderr($"Failed to retrieve results for job ID {jobId}.");
                Logger.LogError(e, $"Failed to download the job output for the specified Azure Quantum job: {e.Message}");
                return AzureClientError.JobOutputDownloadFailed.ToExecutionResult();
            }
        }

        internal async Task<ExecutionResult> CreateOutput(CloudJob job, IChannel? channel, CancellationToken cancellationToken)
        {
            async Task<Stream> ReadHttp()
            {
                var handler = new HttpClientHandler();
                handler.CheckCertificateRevocationList = true;
                var client = new HttpClient(handler);
                var request = await client.GetAsync(job.OutputDataUri, cancellationToken);
                return await request.Content.ReadAsStreamAsync();
            }

            using var stream = job.OutputDataUri.IsFile
                ? File.OpenRead(job.OutputDataUri.LocalPath)
                : await ReadHttp();

            if (job.OutputDataFormat == OutputFormat.ResourceEstimatesV1)
            {
                return stream.ToResourceEstimationResults().ToExecutionResult();
            }
            else if (job.OutputDataFormat == OutputFormat.QirResultsV1)
            {
                var (messages, result) = ParseSimulatorOutput(stream);
                channel?.Stdout(messages);
                return result.ToExecutionResult();
            }
            else if (job.OutputDataFormat == OutputFormat.QuantumResultsV1)
            {
                return stream.ToHistogram(channel, Logger).ToExecutionResult();
            }
            else
            {
                channel?.Stderr($"Job ID {job.Id} has unsupported output format: {job.OutputDataFormat}");
                return AzureClientError.JobOutputDownloadFailed.ToExecutionResult();
            }
        }

        private static (string Messages, string Result) ParseSimulatorOutput(Stream stream)
        {
            var outputLines = new List<string>();
            using (var reader = new StreamReader(stream))
            {
                var line = String.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    outputLines.Add(line.Trim());
                }
            }

            // N.B. The current simulator output format is just text and it does not distinguish
            // between the result of the operation and other kinds of output.
            // Attempt to parse the output to distinguish the result from the rest of the output
            // until the simulator output format makes it easy to do so.
            var resultStartLine = outputLines.Count() - 1;
            if (outputLines[resultStartLine].EndsWith('"'))
            {
                while (!outputLines[resultStartLine].StartsWith('"'))
                {
                    resultStartLine -= 1;
                }
            }

            var messages = String.Join('\n', outputLines.Take(resultStartLine));
            var result = String.Join(' ', outputLines.Skip(resultStartLine));
            return (messages, result);
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobStatusAsync(IChannel? channel, string? jobId, CancellationToken? cancellationToken = default)
        {
            if (ActiveWorkspace == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(jobId))
            {
                if (string.IsNullOrEmpty(MostRecentJobId))
                {
                    channel?.Stderr("No job ID was specified. Please submit a job first or specify a job ID.");
                    return AzureClientError.JobNotFound.ToExecutionResult();
                }

                jobId = MostRecentJobId;
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var job = await ActiveWorkspace.GetJobAsync(jobId, cancellationToken ?? default);
            if (job == null)
            {
                channel?.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            return job.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobListAsync(IChannel channel, string filter, int? count = default, CancellationToken? cancellationToken = default)
        {
            if (ActiveWorkspace == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var jobs = new List<CloudJob>();
            await foreach (var job in ActiveWorkspace.ListJobsAsync(cancellationToken ?? CancellationToken.None))
            {
                if (job.Matches(filter))
                {
                    jobs.Add(job);
                }

                if (count.HasValue)
                {
                    if (jobs.Count >= count)
                    {
                        channel?.Stdout($"Showing only the first {count} jobs:");
                        break;
                    }
                }
            }

            if (jobs.Count() == 0)
            {
                if (string.IsNullOrEmpty(filter))
                {
                    channel?.Stderr("No jobs found in current Azure Quantum workspace.");
                }
                else
                {
                    channel?.Stderr($"No jobs matching \"{filter}\" found in current Azure Quantum workspace.");
                }
            }

            return jobs.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetQuotaListAsync(IChannel channel, CancellationToken? cancellationToken = default)
        {
            if (ActiveWorkspace == null)
            {
                channel?.Stderr($"Please call {GetCommandDisplayName("connect")} before reading quota information.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var connectionResult = await RefreshConnectionAsync(channel, cancellationToken ?? default);
            if (connectionResult.Status != ExecuteStatus.Ok)
            {
                return connectionResult;
            }

            var quotas = new List<QuotaInfo>();
            await foreach (var q in ActiveWorkspace.ListQuotasAsync(cancellationToken ?? CancellationToken.None))
            {
                quotas.Add(q);
            }

            if (quotas.Count() == 0)
            {
                channel?.Stdout("No quota information found in current Azure Quantum workspace.");
            }

            return quotas.ToExecutionResult();
        }

        /// <inheritdoc />
        public void ClearActiveTarget()
        {
            ActiveTarget = null;
            TargetCapability = TargetCapabilityModule.Top;
        }

        /// <inheritdoc />
        public bool TrySetTargetCapability(IChannel? channel, string? capabilityName, [NotNullWhen(true)] out TargetCapability? targetCapability)
        {
            var capability = capabilityName is null
                ? ActiveTarget?.DefaultCapability ?? TargetCapabilityModule.Top
                : TargetCapabilityModule.FromName(capabilityName);
            if (!FSharpOption<TargetCapability>.get_IsSome(capability))
            {
                channel?.Stderr($"Could not parse target capability name \"{capabilityName}\".");
                targetCapability = null;
                return false;
            }

            if (ActiveTarget != null && !ActiveTarget.SupportsCapability(capability.Value))
            {
                channel?.Stderr($"Target capability {capability.Value.Name} is not supported by the active target, {ActiveTarget.TargetId}. The active target supports a maximum capability level of {ActiveTarget.DefaultCapability.Name}.");
                targetCapability = null;
                return false;
            }

            TargetCapability = capability.Value;
            targetCapability = capability.Value;
            return true;
        }
    }
}
