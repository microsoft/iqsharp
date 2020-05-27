﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        /// <returns>
        /// The list of execution targets available in the Azure Quantum workspace.
        /// </returns>
        public Task<ExecutionResult> ConnectAsync(
            IChannel channel,
            string subscriptionId,
            string resourceGroupName,
            string workspaceName,
            string storageAccountConnectionString,
            bool refreshCredentials = false);

        /// <summary>
        /// Gets the current connection status to an Azure Quantum workspace.
        /// </summary>
        /// <returns>
        /// The list of execution targets available in the Azure Quantum workspace,
        /// or an error if the Azure Quantum workspace connection has not yet been created.
        /// </returns>
        public Task<ExecutionResult> GetConnectionStatusAsync(
            IChannel channel);

        /// <summary>
        /// Submits the specified Q# operation as a job to the currently active target.
        /// </summary>
        /// <returns>
        /// Details of the submitted job, or an error if submission failed.
        /// </returns>
        public Task<ExecutionResult> SubmitJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName);

        /// <summary>
        /// Executes the specified Q# operation as a job to the currently active target
        /// and waits for execution to complete before returning.
        /// </summary>
        /// <returns>
        /// The result of the executed job, or an error if execution failed.
        /// </returns>
        public Task<ExecutionResult> ExecuteJobAsync(
            IChannel channel,
            IOperationResolver operationResolver,
            string operationName);

        /// <summary>
        /// Sets the specified target for job submission.
        /// </summary>
        /// <returns>
        /// Success if the target is valid, or an error if the target cannot be set.
        /// </returns>
        public Task<ExecutionResult> SetActiveTargetAsync(
            IChannel channel,
            string targetName);

        /// <summary>
        /// Gets the currently specified target for job submission.
        /// </summary>
        /// <returns>
        /// The target name.
        /// </returns>
        public Task<ExecutionResult> GetActiveTargetAsync(
            IChannel channel);

        /// <summary>
        /// Gets the result of a specified job.
        /// </summary>
        /// <returns>
        /// The job result corresponding to the given job ID,
        /// or for the most recently-submitted job if no job ID is provided.
        /// </returns>
        public Task<ExecutionResult> GetJobResultAsync(
            IChannel channel,
            string jobId);

        /// <summary>
        /// Gets the status of a specified job.
        /// </summary>
        /// <returns>
        /// The job status corresponding to the given job ID,
        /// or for the most recently-submitted job if no job ID is provided.
        /// </returns>
        public Task<ExecutionResult> GetJobStatusAsync(
            IChannel channel, 
            string jobId);

        /// <summary>
        /// Gets a list of all jobs in the current Azure Quantum workspace.
        /// </summary>
        /// <returns>
        /// A list of all jobs in the current workspace.
        /// </returns>
        public Task<ExecutionResult> GetJobListAsync(
            IChannel channel);
    }
}
