﻿// Copyright (c) Microsoft Corporation.
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
            TokenCredential credential,
            QuantumJobClientOptions options) =>
                new Azure.Quantum.Workspace(
                    subscriptionId: subscriptionId,
                    resourceGroupName: resourceGroup,
                    workspaceName: workspaceName,
                    location: location,
                    credential: credential,
                    options: options);

        /// <inheritdoc />
        public IQuantumMachine? CreateMachine(Azure.Quantum.IWorkspace workspace, string targetName, string storageConnectionString) =>
            QuantumMachineFactory.CreateMachine(workspace, targetName, storageConnectionString);
    }
}
