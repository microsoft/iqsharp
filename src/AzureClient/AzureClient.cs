// Copyright (c) Microsoft Corporation. All rights reserved.
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

        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(
            IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool forceLoginPrompt = false)
        {
            ConnectionString = storageAccountConnectionString;

            var clientId = "84ba0947-6c53-4dd2-9ca9-b3694761521b"; // Microsoft Quantum Development Kit
            var authority = "https://login.microsoftonline.com/common";
            var msalApp = PublicClientApplicationBuilder.Create(clientId).WithAuthority(authority).Build();

            // Register the token cache for serialization
            var cacheFileName = "aad.bin";
            var cacheDirectoryEnvVarName = "AZURE_QUANTUM_TOKEN_CACHE";
            var cacheDirectory = System.Environment.GetEnvironmentVariable(cacheDirectoryEnvVarName);
            if (string.IsNullOrEmpty(cacheDirectory))
            {
                cacheDirectory = Path.Join(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".azure-quantum");
            }

            var storageCreationProperties = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory, clientId).Build();
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageCreationProperties);
            cacheHelper.RegisterCache(msalApp.UserTokenCache);

            var scopes = new List<string>() { "https://quantum.microsoft.com/Jobs.ReadWrite" };

            bool shouldShowLoginPrompt = forceLoginPrompt;
            if (!shouldShowLoginPrompt)
            { 
                try
                {
                    var accounts = await msalApp.GetAccountsAsync();
                    AuthenticationResult = await msalApp.AcquireTokenSilent(
                        scopes, accounts.FirstOrDefault()).WithAuthority(msalApp.Authority).ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    shouldShowLoginPrompt = true;
                }
            }

            if (shouldShowLoginPrompt)
            {
                AuthenticationResult = await msalApp.AcquireTokenWithDeviceCode(scopes,
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
                WorkspaceName = workspaceName
            };
            ActiveWorkspace = new Azure.Quantum.Workspace(
                QuantumClient.SubscriptionId, QuantumClient.ResourceGroupName,
                QuantumClient.WorkspaceName, AuthenticationResult?.AccessToken);

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
                channel.Stderr("Please call %connect before submitting a job.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTargetName == null)
            {
                channel.Stderr("Please call %target before submitting a job.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(operationName))
            {
                channel.Stderr("Please pass a valid Q# operation name to %submit.");
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

            // TODO: check `execute` and do appropriate thing.
            var job = await machine.SubmitAsync(entryPointInfo, entryPointInput);

            // TODO: Add encoder for IQuantumMachineJob rather than calling ToJupyterTable() here.
            return job.ToJupyterTable().ToExecutionResult();
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
            if (QuantumClient == null)
            {
                channel.Stderr("Please call %connect before getting job results.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            // TODO: If jobId is empty, use the most-recently submitted job in this session.
            var jobDetails = await QuantumClient.Jobs.GetAsync(jobId);
            if (jobDetails == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            // TODO: How to get the job results? There is no API for this.
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobStatusAsync(
            IChannel channel,
            string jobId)
        {
            if (QuantumClient == null)
            {
                channel.Stderr("Please call %connect before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            // TODO: If jobId is empty, use the most-recently submitted job in this session.
            var jobDetails = await QuantumClient.Jobs.GetAsync(jobId);
            if (jobDetails == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            // TODO: Add encoder for JobDetails rather than calling ToJupyterTable() here directly.
            return jobDetails.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetJobListAsync(
            IChannel channel)
        {
            if (QuantumClient == null)
            {
                channel.Stderr("Please call %connect before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobsList = await QuantumClient.Jobs.ListAsync();
            if (jobsList == null || jobsList.Count() == 0)
            {
                channel.Stderr("No jobs found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            // TODO: Add encoder for IPage<JobDetails> rather than calling ToJupyterTable() here directly.
            return jobsList.ToJupyterTable().ToExecutionResult();
        }
    }
}
