﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum.Client;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Rest.Azure;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Azure.Quantum.Storage;
using Microsoft.Azure.Quantum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
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


        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(
            IChannel channel,
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

        private async Task<ExecutionResult> SubmitOrExecuteJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName,
            bool execute)
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

            var machine = Azure.Quantum.QuantumMachineFactory.CreateMachine(ActiveWorkspace, ActiveTarget.TargetName, ConnectionString);
            if (machine == null)
            {
                // We should never get here, since ActiveTarget should have already been validated at the time it was set.
                channel.Stderr($"Unexpected error while preparing job for execution on target {ActiveTarget.TargetName}.");
                return AzureClientError.InvalidTarget.ToExecutionResult();
            }

            var operationInfo = operationResolver.Resolve(operationName);
            var entryPointInfo = new EntryPointInfo<QVoid, Result>(operationInfo.RoslynType);
            var entryPointInput = QVoid.Instance;

            if (execute)
            {
                channel.Stdout($"Executing {operationName} on target {ActiveTarget.TargetName}...");
                var output = await machine.ExecuteAsync(entryPointInfo, entryPointInput);
                MostRecentJobId = output.Job.Id;

                // TODO: Add encoder to visualize IEnumerable<KeyValuePair<string, double>>
                return output.Histogram.ToExecutionResult();
            }
            else
            {
                channel.Stdout($"Submitting {operationName} to target {ActiveTarget.TargetName}...");
                var job = await machine.SubmitAsync(entryPointInfo, entryPointInput);
                channel.Stdout($"Job {job.Id} submitted successfully.");

                MostRecentJobId = job.Id;

                // TODO: Add encoder for IQuantumMachineJob rather than calling ToJupyterTable() here.
                return job.ToJupyterTable().ToExecutionResult();
            }
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName) =>
            await SubmitOrExecuteJobAsync(channel, operationResolver, operationName, execute: false);

        /// <inheritdoc/>
        public async Task<ExecutionResult> ExecuteJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName) =>
            await SubmitOrExecuteJobAsync(channel, operationResolver, operationName, execute: true);

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetActiveTargetAsync(
            IChannel channel)
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
        public async Task<ExecutionResult> SetActiveTargetAsync(
            IChannel channel,
            IReferences references,
            string targetName)
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

            channel.Stdout($"Loading package {ActiveTarget.PackageName} and dependencies...");
            await references.AddPackage(ActiveTarget.PackageName);

            return $"Active target is now {ActiveTarget.TargetName}".ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobResultAsync(
            IChannel channel,
            string jobId)
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
                channel.Stderr($"Job ID {jobId} has not completed. To check the status, use:\n   %azure.status {jobId}");
                return AzureClientError.JobNotCompleted.ToExecutionResult();
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
        public async Task<ExecutionResult> GetJobStatusAsync(
            IChannel channel,
            string jobId)
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

            // TODO: Add encoder for CloudJob which calls ToJupyterTable() for display.
            return job.Details.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobListAsync(
            IChannel channel)
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
