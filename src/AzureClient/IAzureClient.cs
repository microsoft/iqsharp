// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
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
        public Task<ExecutionResult> ConnectAsync(IChannel channel,
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
        public Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel);

        /// <summary>
        /// Submits the specified Q# operation as a job to the currently active target.
        /// </summary>
        /// <returns>
        /// Details of the submitted job, or an error if submission failed.
        /// </returns>
        public Task<ExecutionResult> SubmitJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? token);

        /// <summary>
        /// Executes the specified Q# operation as a job to the currently active target
        /// and waits for execution to complete before returning.
        /// </summary>
        /// <returns>
        /// The result of the executed job, or an error if execution failed.
        /// </returns>
        public Task<ExecutionResult> ExecuteJobAsync(IChannel channel, AzureSubmissionContext submissionContext, CancellationToken? token);

        /// <summary>
        /// Sets the specified target for job submission.
        /// </summary>
        /// <returns>
        /// Success if the target is valid, or an error if the target cannot be set.
        /// </returns>
        public Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetId);

        /// <summary>
        /// Gets the currently specified target for job submission.
        /// </summary>
        /// <returns>
        /// The target ID.
        /// </returns>
        public Task<ExecutionResult> GetActiveTargetAsync(IChannel channel);

        /// <summary>
        /// Gets the result of a specified job.
        /// </summary>
        /// <returns>
        /// The job result corresponding to the given job ID,
        /// or for the most recently-submitted job if no job ID is provided.
        /// </returns>
        public Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId);

        /// <summary>
        /// Gets the status of a specified job.
        /// </summary>
        /// <returns>
        /// The job status corresponding to the given job ID,
        /// or for the most recently-submitted job if no job ID is provided.
        /// </returns>
        public Task<ExecutionResult> GetJobStatusAsync(IChannel channel, string jobId);

        /// <summary>
        /// Gets a list of all jobs in the current Azure Quantum workspace.
        /// </summary>
        /// <returns>
        /// A list of all jobs in the current workspace, optionally filtered
        /// to jobs with fields containing <c>filter</c> using a case-insensitive
        /// comparison.
        /// </returns>
        public Task<ExecutionResult> GetJobListAsync(IChannel channel, string filter);
    }
}
