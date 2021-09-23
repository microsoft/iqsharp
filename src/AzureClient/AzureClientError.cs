// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.ComponentModel;

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
        UnknownError = 1000,

        /// <summary>
        /// No connection has been made to any Azure Quantum workspace.
        /// </summary>
        [Description(Resources.AzureClientErrorNotConnected)]
        NotConnected,

        /// <summary>
        /// A target has not yet been configured for job submission.
        /// </summary>
        [Description(Resources.AzureClientErrorNoTarget)]
        NoTarget,

        /// <summary>
        /// The specified target is not valid for job submission.
        /// </summary>
        [Description(Resources.AzureClientErrorInvalidTarget)]
        InvalidTarget,

        /// <summary>
        /// A job meeting the specified criteria was not found.
        /// </summary>
        [Description(Resources.AzureClientErrorJobNotFound)]
        JobNotFound,

        /// <summary>
        /// The result of a job was requested, but the job has not yet completed.
        /// </summary>
        [Description(Resources.AzureClientErrorJobNotCompleted)]
        JobNotCompleted,

        /// <summary>
        /// The job output failed to be downloaded from the Azure storage location.
        /// </summary>
        [Description(Resources.AzureClientErrorJobOutputDownloadFailed)]
        JobOutputDownloadFailed,

        /// <summary>
        /// No Q# operation name was provided where one was required.
        /// </summary>
        [Description(Resources.AzureClientErrorNoOperationName)]
        NoOperationName,

        /// <summary>
        /// The specified Q# operation name is not recognized.
        /// </summary>
        [Description(Resources.AzureClientErrorUnrecognizedOperationName)]
        UnrecognizedOperationName,

        /// <summary>
        /// The specified Q# operation cannot be used as an entry point.
        /// </summary>
        [Description(Resources.AzureClientErrorInvalidEntryPoint)]
        InvalidEntryPoint,

        /// <summary>
        /// The Azure Quantum job submission failed.
        /// </summary>
        [Description(Resources.AzureClientErrorJobSubmissionFailed)]
        JobSubmissionFailed,

        /// <summary>
        /// Authentication with the Azure service failed.
        /// </summary>
        [Description(Resources.AzureClientErrorAuthenticationFailed)]
        AuthenticationFailed,

        /// <summary>
        /// A workspace meeting the specified criteria was not found.
        /// </summary>
        [Description(Resources.AzureClientErrorWorkspaceNotFound)]
        WorkspaceNotFound,

        /// <summary>
        /// A workspace was provided without a location.
        /// </summary>
        [Description(Resources.AzureClientErrorNoWorkspaceLocation)]
        NoWorkspaceLocation,


        /// <summary>
        /// A workspace was provided without an invalid location.
        /// </summary>
        [Description(Resources.AzureClientErrorInvalidWorkspaceLocation)]
        InvalidWorkspaceLocation,

        /// <summary>
        /// The Azure Quantum job failed or was cancelled.
        /// </summary>
        [Description(Resources.AzureClientErrorJobFailedOrCancelled)]
        JobFailedOrCancelled,
    }
}
