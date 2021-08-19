// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Quantum.Authentication;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// List of arguments for the event that is triggered when the user tries 
    /// to connect to an Azure Quantum Workspace
    /// </summary>
    public class ConnectToWorkspaceEventArgs : EventArgs
    {
        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="status">The status of the connection after calling Connect</param>
        /// <param name="error">If an error happen, the error code.</param>
        /// <param name="location">Location of the workspace connecting to.</param>
        /// <param name="useCustomStorage">True if the user provides a custom storage connection.</param>
        /// <param name="credentialType">The type of credentials used to authenticate with Azure.</param>
        /// <param name="duration">How long the action took.</param>
        public ConnectToWorkspaceEventArgs(ExecuteStatus status, AzureClientError? error, string location, bool useCustomStorage, CredentialType credentialType, TimeSpan duration)
        {
            this.Status = status;
            this.Error = error;
            this.Location =location;
            this.UseCustomStorage = useCustomStorage;
            this.CredentialType = credentialType;
            this.Duration = duration;
        }

        /// <summary>
        /// The connection status. Can be "success" or "error"
        /// </summary>
        public ExecuteStatus Status { get; }

        /// <summary>
        /// If an error happened during connection, the error code.
        /// Otherwise, <c>null</c>.
        /// </summary>
        public AzureClientError? Error { get; }

        /// <summary>
        /// The region (location) we tried to connect to.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// True if the user provides a custom storage connection.
        /// </summary>
        public bool UseCustomStorage { get; }

        /// <summary>
        /// The type of credentials used to authenticate with Azure.
        /// </summary>
        public CredentialType CredentialType { get; }

        /// <summary>
        /// The total time it took to connect.
        /// </summary>
        public TimeSpan Duration { get; }
    }

    /// <summary>
    /// This service is capable of connecting to Azure Quantum workspaces
    /// and submitting jobs.
    /// </summary>
    public interface IAzureClient
    {
        /// <summary>
        /// This event is triggered when a user connects to an Azure Quantum Workspace.
        /// </summary>
        event EventHandler<ConnectToWorkspaceEventArgs> ConnectToWorkspace;

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
            string location,
            CredentialType credentialType,
            CancellationToken? cancellationToken = null);

        /// <summary>
        /// Gets the current connection status to an Azure Quantum workspace.
        /// </summary>
        /// <returns>
        /// The list of execution targets available in the Azure Quantum workspace,
        /// or an error if the Azure Quantum workspace connection has not yet been created.
        /// </returns>
        public Task<ExecutionResult> GetConnectionStatusAsync(IChannel channel, CancellationToken? token);

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
        public Task<ExecutionResult> SetActiveTargetAsync(IChannel channel, string targetId, CancellationToken? token);

        /// <summary>
        /// Gets the currently specified target for job submission.
        /// </summary>
        /// <returns>
        /// The target ID.
        /// </returns>
        public Task<ExecutionResult> GetActiveTargetAsync(IChannel channel, CancellationToken? token);

        /// <summary>
        /// Gets the result of a specified job.
        /// </summary>
        /// <returns>
        /// The job result corresponding to the given job ID,
        /// or for the most recently-submitted job if no job ID is provided.
        /// </returns>
        public Task<ExecutionResult> GetJobResultAsync(IChannel channel, string jobId, CancellationToken? token);

        /// <summary>
        /// Gets the status of a specified job.
        /// </summary>
        /// <returns>
        /// The job status corresponding to the given job ID,
        /// or for the most recently-submitted job if no job ID is provided.
        /// </returns>
        public Task<ExecutionResult> GetJobStatusAsync(IChannel channel, string jobId, CancellationToken? token);

        /// <summary>
        /// Gets a list of all jobs in the current Azure Quantum workspace.
        /// </summary>
        /// <returns>
        /// A list of all jobs in the current workspace, optionally filtered
        /// to jobs with fields containing <c>filter</c> using a case-insensitive
        /// comparison.
        /// </returns>
        public Task<ExecutionResult> GetJobListAsync(IChannel channel, string filter, CancellationToken? token);

        /// <summary>
        /// Gets a list of all jobs in the current Azure Quantum workspace.
        /// </summary>
        /// <returns>
        /// A list of all jobs in the current workspace, optionally filtered
        /// to jobs with fields containing <c>filter</c> using a case-insensitive
        /// comparison.
        /// </returns>
        public Task<ExecutionResult> GetQuotaListAsync(IChannel channel, CancellationToken? token);

        /// <summary>
        ///      Returns a string indicating the current target ID if one is
        ///      set, or <c>null</c> if no target is set.
        /// </summary>
        string? ActiveTargetId { get; }

        /// <summary>
        ///     Returns the active workspace connected to this client, or
        ///     <c>null</c> if none is set.
        /// </summary>
        Microsoft.Azure.Quantum.IWorkspace? ActiveWorkspace { get; }
    }
}
