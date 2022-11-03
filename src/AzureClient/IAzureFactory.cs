// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Azure.Core;
using Azure.Quantum;

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
        Azure.Quantum.IWorkspace CreateWorkspace(string subscriptionId, string resourceGroup, string workspaceName, string location, TokenCredential credential, QuantumJobClientOptions options);

        /// <summary>
        /// Creates an instance of an Azure Quantum Machine
        /// </summary>
        Runtime.IQuantumMachine? CreateMachine(Azure.Quantum.IWorkspace workspace, string targetName, string storageConnectionString);
    }
}
