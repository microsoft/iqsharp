// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using Microsoft.Azure.Quantum;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This service is used to create instances of classes from the AzureQuantum packages.
    /// Mostly used to support Mock instances.
    /// </summary>
    public class MocksAzureFactory : IAzureFactory
    {
        /// <inheritdoc />
        public Azure.Quantum.IWorkspace CreateWorkspace(string subscriptionId, string resourceGroup, string workspaceName, string location) =>
             new MockAzureWorkspace(
                    subscriptionId: subscriptionId,
                    resourceGroup: resourceGroup,
                    workspaceName: workspaceName,
                    location: location);

        /// <inheritdoc />
        public IQuantumMachine CreateMachine(Azure.Quantum.IWorkspace workspace, string targetName, string storageConnectionString) =>
            new MockQuantumMachine(workspace as MockAzureWorkspace);
    }
}
