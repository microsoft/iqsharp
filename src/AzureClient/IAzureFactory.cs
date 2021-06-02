using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This service is used to create instances of classes from the AzureQuantum packages.
    /// Mostly used to support Mock instances.
    /// </summary>
    public interface IAzureFactory
    {
        /// <summary>
        /// Creates an instance of an Azure Quantum Workspace client
        /// </summary>
        Azure.Quantum.IWorkspace CreateWorkspace(string subscriptionId, string resourceGroup, string workspaceName, string location);
    }
}
