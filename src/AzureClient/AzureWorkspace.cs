// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Azure.Quantum.Jobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.Runtime;
using Azure.Quantum.Jobs;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class AzureWorkspace : IAzureWorkspace
    {
        public string? Name => AzureQuantumWorkspace?.WorkspaceName;
        public string? SubscriptionId => AzureQuantumWorkspace?.SubscriptionId;
        public string? ResourceGroup => AzureQuantumWorkspace?.ResourceGroupName;
        public string? Location { get; private set; }

        private Azure.Quantum.Workspace AzureQuantumWorkspace { get; set; }
        private QuantumJobClient AzureQuantumClient { get; set; }
        private ILogger<AzureWorkspace> Logger { get; } = new LoggerFactory().CreateLogger<AzureWorkspace>();

        public AzureWorkspace(QuantumJobClient azureQuantumClient, Azure.Quantum.Workspace azureQuantumWorkspace, string location)
        {
            AzureQuantumClient = azureQuantumClient;
            AzureQuantumWorkspace = azureQuantumWorkspace;
            Location = location;
        }

        public async Task<IEnumerable<ProviderStatus>?> GetProvidersAsync()
        {
            try
            {
                var results = new List<ProviderStatus>();
                var status = AzureQuantumClient.GetProviderStatusAsync();
                await foreach(var s in status)
                {
                    results.Add(s);
                }
                return results;
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

        public async Task<IEnumerable<QuotaInfo>?> ListQuotasAsync()
        {
            try
            {
                return await AzureQuantumWorkspace.ListQuotasAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to retrieve the quota information from the Azure Quantum workspace: {e.Message}");
            }

            return null;
        }

        public IQuantumMachine? CreateQuantumMachine(string targetId, string storageAccountConnectionString) =>
            QuantumMachineFactory.CreateMachine(AzureQuantumWorkspace, targetId, storageAccountConnectionString);

        public AzureExecutionTarget? CreateExecutionTarget(string targetId) =>
            AzureExecutionTarget.Create(targetId);
    }
}
