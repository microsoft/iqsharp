// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class AzureWorkspace : IAzureWorkspace
    {
        public string? Name => AzureQuantumClient?.WorkspaceName;
        public string? SubscriptionId => AzureQuantumClient?.SubscriptionId;
        public string? ResourceGroup => AzureQuantumClient?.ResourceGroupName;
        public string? Location { get; private set; }

        private Azure.Quantum.IWorkspace AzureQuantumWorkspace { get; set; }
        private QuantumClient AzureQuantumClient { get; set; }
        private ILogger<AzureWorkspace> Logger { get; } = new LoggerFactory().CreateLogger<AzureWorkspace>();

        public AzureWorkspace(QuantumClient azureQuantumClient, Azure.Quantum.Workspace azureQuantumWorkspace, string location)
        {
            AzureQuantumClient = azureQuantumClient;
            AzureQuantumWorkspace = azureQuantumWorkspace;
            Location = location;
        }

        public async Task<IEnumerable<ProviderStatus>?> GetProvidersAsync()
        {
            try
            {
                return await AzureQuantumClient.Providers.GetStatusAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to retrieve the providers list from the Azure Quantum workspace: {e.Message}");
            }

            return null;
        }

        public async Task<CloudJob?> GetJobAsync(string jobId)
        {
            try
            {
                return await AzureQuantumWorkspace.GetJobAsync(jobId);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to retrieve the specified Azure Quantum job: {e.Message}");
            }

            return null;
        }

        public async Task<IEnumerable<CloudJob>?> ListJobsAsync()
        {
            try
            {
                return await AzureQuantumWorkspace.ListJobsAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to retrieve the list of jobs from the Azure Quantum workspace: {e.Message}");
            }

            return null;
        }

        public IQuantumMachine? CreateQuantumMachine(string targetId, string storageAccountConnectionString) =>
            QuantumMachineFactory.CreateMachine(AzureQuantumWorkspace, targetId, storageAccountConnectionString);

        public AzureExecutionTarget? CreateExecutionTarget(string targetId) =>
            AzureExecutionTarget.Create(targetId);
    }
}
