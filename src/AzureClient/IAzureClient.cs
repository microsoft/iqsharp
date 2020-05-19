// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Describes possible error results from <see cref="IAzureClient"/> methods.
    /// </summary>
    public enum AzureClientError
    {
        /// <summary>
        /// Method completed with an unknown error.
        /// </summary>
        [Description(Resources.AzureClientErrorUnknownError)]
        UnknownError = 0,

        /// <summary>
        /// No connection has been made to any Azure Quantum workspace.
        /// </summary>
        [Description(Resources.AzureClientErrorNotConnected)]
        NotConnected = 1,

        /// <summary>
        /// A target has not yet been configured for job submission.
        /// </summary>
        [Description(Resources.AzureClientErrorNoTarget)]
        NoTarget = 2,

        /// <summary>
        /// A job meeting the specified criteria was not found.
        /// </summary>
        [Description(Resources.AzureClientErrorJobNotFound)]
        JobNotFound = 3,

        /// <summary>
        /// No Q# operation name was provided where one was required.
        /// </summary>
        [Description(Resources.AzureClientErrorNoOperationName)]
        NoOperationName = 4,

        /// <summary>
        /// Authentication with the Azure service failed.
        /// </summary>
        [Description(Resources.AzureClientErrorAuthenticationFailed)]
        AuthenticationFailed = 5,

        /// <summary>
        /// A workspace meeting the specified criteria was not found.
        /// </summary>
        [Description(Resources.AzureClientErrorWorkspaceNotFound)]
        WorkspaceNotFound = 6,
    }

    /// <summary>
    /// This service is capable of connecting to Azure Quantum workspaces
    /// and submitting jobs.
    /// </summary>
    public interface IAzureClient
    {
        /// <summary>
        /// Connects to the specified Azure Quantum workspace, first logging into Azure if necessary.
        /// </summary>
        public Task<ExecutionResult> ConnectAsync(
            IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool forceLogin = false);

        /// <summary>
        /// Prints a string describing the current connection status.
        /// </summary>
        public Task<ExecutionResult> PrintConnectionStatusAsync(
            IChannel channel);

        /// <summary>
        /// Submits the specified Q# operation as a job to the currently active target.
        /// </summary>
        public Task<ExecutionResult> SubmitJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName);

        /// <summary>
        /// Sets the specified target for job submission.
        /// </summary>
        public Task<ExecutionResult> SetActiveTargetAsync(
            IChannel channel,
            string targetName);

        /// <summary>
        /// Prints the list of targets currently provisioned in the current workspace.
        /// </summary>
        public Task<ExecutionResult> PrintTargetListAsync(
            IChannel channel);

        /// <summary>
        /// Prints the job status corresponding to the given job ID.
        /// </summary>
        public Task<ExecutionResult> PrintJobStatusAsync(
            IChannel channel, 
            string jobId);

        /// <summary>
        /// Prints a list of all jobs in the current workspace.
        /// </summary>
        public Task<ExecutionResult> PrintJobListAsync(
            IChannel channel);
    }
}
