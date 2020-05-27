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

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        private string ConnectionString { get; set; } = string.Empty;
        private string ActiveTargetName { get; set; } = string.Empty;
        private AuthenticationResult? AuthenticationResult { get; set; }
        private IQuantumClient? QuantumClient { get; set; }
        private IPage<ProviderStatus>? ProviderStatusList { get; set; }
        private Azure.Quantum.IWorkspace? ActiveWorkspace { get; set; }
        private string MostRecentJobId { get; set; } = string.Empty;

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
                ProviderStatusList = await QuantumClient.Providers.GetStatusAsync();
            }
            catch (Exception e)
            {
                channel.Stderr(e.ToString());
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            channel.Stdout($"Connected to Azure Quantum workspace {QuantumClient.WorkspaceName}.");

            // TODO: Add encoder for IPage<ProviderStatus> rather than calling ToJupyterTable() here directly.
            return ProviderStatusList.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel)
        {
            if (QuantumClient == null || ProviderStatusList == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            channel.Stdout($"Connected to Azure Quantum workspace {QuantumClient.WorkspaceName}.");

            // TODO: Add encoder for IPage<ProviderStatus> rather than calling ToJupyterTable() here directly.
            return ProviderStatusList.ToJupyterTable().ToExecutionResult();
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

            if (ActiveTargetName == null)
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

            var machine = Azure.Quantum.QuantumMachineFactory.CreateMachine(ActiveWorkspace, ActiveTargetName, ConnectionString);
            if (machine == null)
            {
                channel.Stderr($"Could not find an execution target for target {ActiveTargetName}.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            var operationInfo = operationResolver.Resolve(operationName);
            var entryPointInfo = new EntryPointInfo<QVoid, Result>(operationInfo.RoslynType);
            var entryPointInput = QVoid.Instance;

            if (execute)
            {
                var output = await machine.ExecuteAsync(entryPointInfo, entryPointInput);
                MostRecentJobId = output.Job.Id;
                // TODO: Add encoder for IQuantumMachineOutput rather than returning the Histogram directly
                return output.Histogram.ToExecutionResult();
            }
            else
            {
                var job = await machine.SubmitAsync(entryPointInfo, entryPointInput);
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
            // TODO: This should also print the list of available targets to the IChannel.
            return ActiveTargetName.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SetActiveTargetAsync(
            IChannel channel,
            string targetName)
        {
            // TODO: Validate that this target name is valid in the workspace.
            // TODO: Load the associated provider package.
            ActiveTargetName = targetName;
            return $"Active target is now {ActiveTargetName}".ToExecutionResult();
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
                channel.Stderr($"Job ID {jobId} has not completed. Displaying the status instead.");
                // TODO: Add encoder for CloudJob rather than calling ToJupyterTable() here directly.
                return job.Details.ToJupyterTable().ToExecutionResult();
            }

            var stream = new MemoryStream();
            var protocol = await new JobStorageHelper(ConnectionString).DownloadJobOutputAsync(jobId, stream);
            stream.Seek(0, SeekOrigin.Begin);
            var outputJson = new StreamReader(stream).ReadToEnd();

            // TODO: Deserialize this once we have a way of getting the output type
            // TODO: Add encoder for job output
            return outputJson.ToExecutionResult();
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

            // TODO: Add encoder for CloudJob rather than calling ToJupyterTable() here directly.
            return job.Details.ToJupyterTable().ToExecutionResult();
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
