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

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        private string ConnectionString { get; set; } = string.Empty;
        private string ActiveTargetName { get; set; } = string.Empty;
        private AuthenticationResult? AuthenticationResult { get; set; }
        private IQuantumClient? QuantumClient { get; set; }
        private Azure.Quantum.Workspace? ActiveWorkspace { get; set; }

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
                var jobsList = await QuantumClient.Jobs.ListAsync();
                channel.Stdout($"Successfully connected to Azure Quantum workspace {workspaceName}.");
            }
            catch (Exception e)
            {
                channel.Stderr(e.ToString());
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            return QuantumClient.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintConnectionStatusAsync(IChannel channel) =>
            QuantumClient == null
            ? AzureClientError.NotConnected.ToExecutionResult()
            : QuantumClient.ToJupyterTable().ToExecutionResult();

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName)
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

            var operationInfo = operationResolver.Resolve(operationName);
            var entryPointInfo = new EntryPointInfo<QVoid, Result>(operationInfo.RoslynType);
            var entryPointInput = QVoid.Instance;
            var machine = Azure.Quantum.QuantumMachineFactory.CreateMachine(ActiveWorkspace, ActiveTargetName, ConnectionString);
            if (machine == null)
            {
                channel.Stderr($"Could not find an execution target for target {ActiveTargetName}.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            var job = await machine.SubmitAsync(entryPointInfo, entryPointInput);
            return job.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SetActiveTargetAsync(
            IChannel channel,
            string targetName)
        {
            // TODO: Validate that this target name is valid in the workspace.
            ActiveTargetName = targetName;
            return $"Active target is now {ActiveTargetName}".ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintTargetListAsync(
            IChannel channel)
        {
            if (QuantumClient == null)
            {
                channel.Stderr("Please call %connect before listing targets.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var providersStatus = await QuantumClient.Providers.GetStatusAsync();
            return providersStatus.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintJobStatusAsync(
            IChannel channel,
            string jobId)
        {
            if (QuantumClient == null)
            {
                channel.Stderr("Please call %connect before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobDetails = await QuantumClient.Jobs.GetAsync(jobId);
            if (jobDetails == null)
            {
                channel.Stderr($"Job ID {jobId} not found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            return jobDetails.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintJobListAsync(
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

            return jobsList.ToJupyterTable().ToExecutionResult();
        }
    }
}
