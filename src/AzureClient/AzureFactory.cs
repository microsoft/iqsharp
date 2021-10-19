// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Azure.Core;

using Azure.Quantum;
using Microsoft.Azure.Quantum;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc />
    public class AzureFactory : IAzureFactory
    {
        /// <inheritdoc />
        public Azure.Quantum.IWorkspace CreateWorkspace(string subscriptionId, 
            string resourceGroup, 
            string workspaceName, 
            string location, 
            TokenCredential credential)
        {
            var options = new QuantumJobClientOptions();

            // This value will be added as a prefix in the UserAgent when
            // calling the Azure Quantum APIs
            options.Diagnostics.ApplicationId = "IQ#";

            return new Azure.Quantum.Workspace(
                    subscriptionId: subscriptionId,
                    resourceGroupName: resourceGroup,
                    workspaceName: workspaceName,
                    location: location,
                    credential: credential);
        }

        /// <inheritdoc />
        public IQuantumMachine? CreateMachine(Azure.Quantum.IWorkspace workspace, string targetName, string storageConnectionString) =>
            QuantumMachineFactory.CreateMachine(workspace, targetName, storageConnectionString);
    }
}
