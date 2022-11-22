#nullable enable

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This class contains resources that will eventually be exposed to localization.
    /// </summary>
    internal static class Resources
    {
        public const string AzureClientErrorUnknownError =
            "An unknown error occurred.";

        public const string AzureClientErrorNotConnected =
            "Not connected to any Azure Quantum workspace.";

        public const string AzureClientErrorNoTarget =
            "No execution target has been configured for Azure Quantum job submission.";

        public const string AzureClientErrorInvalidTarget =
            "The specified target is not enabled in this workspace. Please make sure the target name is valid and that the associated provider is added to your workspace. To add a provider to your quantum workspace in the Azure Portal, see https://aka.ms/AQ/Docs/AddProvider ";

        public const string AzureClientErrorJobNotFound =
            "No job with the given ID was found in the current Azure Quantum workspace.";

        public const string AzureClientErrorJobNotCompleted =
            "The specified Azure Quantum job has not yet completed.";

        public const string AzureClientErrorJobOutputDownloadFailed =
            "Failed to download results for the specified Azure Quantum job.";

        public const string AzureClientErrorJobOutputDisplayFailed =
            "Failed to parse and display downloaded results for the specified Azure Quantum job.";

        public const string AzureClientErrorNoOperationName =
            "No Q# operation name was specified for Azure Quantum job submission.";

        public const string AzureClientErrorUnrecognizedOperationName =
            "The specified Q# operation name was not recognized.";

        public const string AzureClientErrorInvalidEntryPoint =
            "The specified Q# operation cannot be used as an entry point for Azure Quantum job submission.";

        public const string AzureClientErrorJobSubmissionFailed =
            "Failed to submit the job to the Azure Quantum workspace.";

        public const string AzureClientErrorAuthenticationFailed =
            "Failed to authenticate to the specified Azure Quantum workspace.";

        public const string AzureClientErrorWorkspaceNotFound =
            "No Azure Quantum workspace was found that matches the specified criteria.";

        public const string AzureClientErrorNoWorkspaceLocation =
            "The location parameter is missing.";

        public const string AzureClientErrorInvalidWorkspaceLocation =
            "The location provided is invalid.";

        public const string AzureClientErrorJobFailedOrCancelled =
            "The specified Azure Quantum job failed or was cancelled.";
    }
}
