using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This class contains resources that will eventually be exposed to localization.
    /// </summary>
    internal static class Resources
    {
        public const string AzureClientError_UnknownError =
            "An unknown error occurred.";

        public const string AzureClientError_NotConnected =
            "Not connected to any Azure Quantum workspace.";

        public const string AzureClientError_NoTarget =
            "No execution target has been configured for Azure Quantum job submission.";

        public const string AzureClientError_JobNotFound =
            "No job with the given ID was found in the current Azure Quantum workspace.";

        public const string AzureClientError_NoOperationName =
            "No Q# operation name was specified for Azure Quantum job submission.";

        public const string AzureClientError_AuthenticationFailed =
            "Failed to authenticate to the specified Azure Quantum workspace.";

        public const string AzureClientError_WorkspaceNotFound =
            "No Azure Quantum workspace was found that matches the specified criteria.";
    }
}
