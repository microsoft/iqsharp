// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Jupyter.Core;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Linq;
using System.IO;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.DataPlane.Client;
using Microsoft.Azure.Quantum.DataPlane.Client.Models;
using Microsoft.Azure.Quantum.ResourceManager.Client;
using Microsoft.Azure.Quantum.ResourceManager.Client.Models;
using Microsoft.Azure.Quantum.Storage;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Providers.IonQ.Targets;
using Microsoft.Quantum.Providers.Honeywell.Targets;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    public class AzureClient : IAzureClient
    {
        private string ConnectionString { get; set; }
        private string ActiveTargetName { get; set; }
        private AuthenticationResult? AuthenticationResult { get; set; }
        private IQuantumClient? DataPlaneClient { get; set; }
        private IQuantumManagementClient? ManagementClient { get; set; }
        private QuantumWorkspace? ActiveWorkspace { get; set; }

        /// <summary>
        /// Creates an AzureClient object.
        /// </summary>
        public AzureClient()
        {
            ConnectionString = string.Empty;
            ActiveTargetName = string.Empty;
        }

        /// <summary>
        /// For testing use only. Injects <c>IQuantumClient</c> and <c>IQuantumManagementClient</c>
        /// objects for use by this AzureClient object.
        /// </summary>
        public void InjectRestClients(IQuantumClient quantumClient, IQuantumManagementClient quantumManagementClient)
        {
            // TODO: This method shouldn't exist. The AzureClient constructor should accept some kind of
            // provider which hands out IQuantumClient and IQuantumManagementClient objects.
            // Clean this up after integrating with the actual packages.
            DataPlaneClient = quantumClient;
            ManagementClient = quantumManagementClient;
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

            DataPlaneClient = new QuantumClient(credentials);
            DataPlaneClient.SubscriptionId = subscriptionId;
            DataPlaneClient.ResourceGroupName = resourceGroupName;
            DataPlaneClient.WorkspaceName = workspaceName;

            try
            {
                var jobsList = await DataPlaneClient.Jobs.ListAsync();
                channel.Stdout($"Found {jobsList.Count()} jobs in Azure Quantum workspace {workspaceName}");
            }
            catch (Exception e)
            {
                channel.Stderr(e.ToString());
                return AzureClientError.WorkspaceNotFound.ToExecutionResult();
            }

            ManagementClient = new QuantumManagementClient(credentials);
            ManagementClient.SubscriptionId = subscriptionId;

            try
            {
                ActiveWorkspace = await ManagementClient.Workspaces.GetAsync(resourceGroupName, workspaceName);
                channel.Stdout($"Valid Azure Quantum workspace {ActiveWorkspace.Name} found in region {ActiveWorkspace.Location}");
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
            if (DataPlaneClient == null)
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

            // TODO: How to correctly choose the type parameters for EntryPointInfo?
            var operationInfo = operationResolver.Resolve(operationName);
            var entryPointInfo = new EntryPointInfo<QVoid, Result>(operationInfo.RoslynType);
            var entryPointInput = QVoid.Instance;

            // TODO: Choose the correct provider
            var jobStorageHelper = new JobStorageHelper(ConnectionString);
            var workspace = new Azure.Quantum.Workspace(
                DataPlaneClient.SubscriptionId, DataPlaneClient.ResourceGroupName,
                DataPlaneClient.WorkspaceName, ConnectionString, AuthenticationResult?.AccessToken);
            var machine = new IonQQuantumMachine(ActiveTargetName, workspace, jobStorageHelper);
            var submissionContext = new IonQQuantumMachine.SubmissionContext();
            var job = await machine.SubmitAsync(entryPointInfo, entryPointInput, submissionContext);
            return job.Id.ToExecutionResult();
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

            // TODO: Is this correct, or do we need to do some kind of reflection to list the
            //       available target names?
            return ActiveWorkspace.Providers.ToJupyterTable().ToExecutionResult();
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> PrintJobStatusAsync(
            IChannel channel,
            string jobId)
        {
            if (DataPlaneClient == null)
            {
                channel.Stderr("Must call %connect before getting job status.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobDetails = await DataPlaneClient.Jobs.GetAsync(jobId);
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
            if (DataPlaneClient == null)
            {
                channel.Stderr("Must call %connect before listing jobs.");
                return AzureClientError.NotConnected.ToExecutionResult();
            }

            var jobsList = await DataPlaneClient.Jobs.ListAsync();
            if (jobsList == null || jobsList.Count() == 0)
            {
                channel.Stderr("No jobs found in current Azure Quantum workspace.");
                return AzureClientError.JobNotFound.ToExecutionResult();
            }

            return jobsList.ToJupyterTable().ToExecutionResult();
        }
    }
}
