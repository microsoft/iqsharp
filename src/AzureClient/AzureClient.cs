// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum.Client;
using Microsoft.Azure.Quantum.Storage;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        private string ConnectionString { get; set; }
        private string ActiveTargetName { get; set; }
        private AuthenticationResult? AuthenticationResult { get; set; }
        private IJobsClient? QuantumClient { get; set; }
        private Azure.Quantum.IWorkspace? ActiveWorkspace { get; set; }

        /// <summary>
        /// Creates an AzureClient object.
        /// </summary>
        public AzureClient()
        {
            ConnectionString = string.Empty;
            ActiveTargetName = string.Empty;
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> ConnectAsync(
            IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool forceLogin = false)
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

            bool promptForLogin = forceLogin;
            if (!promptForLogin)
            { 
                try
                {
                    var accounts = await msalApp.GetAccountsAsync();
                    AuthenticationResult = await msalApp.AcquireTokenSilent(
                        scopes, accounts.FirstOrDefault()).WithAuthority(msalApp.Authority).ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    promptForLogin = true;
                }
            }

            if (promptForLogin)
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

            QuantumClient = new JobsClient(); // TODO: pass (credentials) after updating package.
            QuantumClient.SubscriptionId = subscriptionId;
            QuantumClient.ResourceGroupName = resourceGroupName;
            QuantumClient.WorkspaceName = workspaceName;

            ActiveWorkspace = new Azure.Quantum.Workspace(
                QuantumClient.SubscriptionId, QuantumClient.ResourceGroupName,
                QuantumClient.WorkspaceName, AuthenticationResult?.AccessToken);

            try
            {
                var jobsList = await QuantumClient.ListJobsAsync(); // QuantumClient.Jobs.ListAsync();
                channel.Stdout($"Found {jobsList.Value.Count()} jobs in Azure Quantum workspace {workspaceName}");
            }
            catch (Exception e)
            {
                channel.Stderr(e.ToString());
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            return ActiveWorkspace.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintConnectionStatusAsync(IChannel channel)
        {
            if (ActiveWorkspace == null)
            {
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            return ActiveWorkspace.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> SubmitJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName)
        {
            if (QuantumClient == null)
            {
                channel.Stderr("Must call %connect before submitting a job.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            if (ActiveTargetName == null)
            {
                channel.Stderr("Must call %target before submitting a job.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            if (string.IsNullOrEmpty(operationName))
            {
                channel.Stderr("Must pass a valid Q# operation name to %submit.");
                return AzureClientError.NoOperationName.ToExecutionResult();
            }

            // TODO: Generate the appropriate EntryPointInfo for the given operation.
            var operationInfo = operationResolver.Resolve(operationName);
            var entryPointInfo = new EntryPointInfo<QVoid, Result>(operationInfo.RoslynType);
            var entryPointInput = QVoid.Instance;

            // TODO: Create the appropriate QuantumMachine object using ActiveTargetName
            var jobStorageHelper = new JobStorageHelper(ConnectionString);
            //var machine = new QuantumMachine(ActiveTargetName, Workspace, jobStorageHelper);
            //var submissionContext = new QuantumMachine.SubmissionContext();
            //var job = await machine.SubmitAsync(entryPointInfo, entryPointInput, submissionContext);
            //return job.Id.ToExecutionResult();
            return operationName.ToExecutionResult();
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
        public async Task<ExecutionResult> PrintActiveTargetAsync(
            IChannel channel)
        {
            if (string.IsNullOrEmpty(ActiveTargetName))
            {
                channel.Stderr("No active target has been set for the current Azure Quantum workspace.");
                return AzureClientError.NoTarget.ToExecutionResult();
            }

            return $"Active target is {ActiveTargetName}.".ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintTargetListAsync(
            IChannel channel)
        {
            if (ActiveWorkspace == null)
            {
                channel.Stderr("Must call %connect before listing targets.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            // TODO: Get the list of targets from the updated Azure.Quantum.Client API
            //return ActiveWorkspace.Targets.ToJupyterTable().ToExecutionResult();
            return AzureClientError.NoTarget.ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintJobStatusAsync(
            IChannel channel,
            string jobId)
        {
            if (QuantumClient == null)
            {
                channel.Stderr("Must call %connect before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobDetails = await QuantumClient.GetJobAsync(jobId); // QuantumClient.Jobs.GetAsync(jobId);
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
                channel.Stderr("Must call %connect before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobsList = await QuantumClient.ListJobsAsync(); // QuantumClient.Jobs.ListAsync();
            if (jobsList.Value == null || jobsList.Value.Count() == 0)
            {
                channel.Stderr("No jobs found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            return jobsList.Value.ToJupyterTable().ToExecutionResult();
        }
    }
}
