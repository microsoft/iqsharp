// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Azure.Quantum.Jobs.Models;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal interface IAzureWorkspace
    {
        public string? Name { get; }
        public string? SubscriptionId { get; }
        public string? ResourceGroup { get; }
        public string? Location { get; }

        public Task<IEnumerable<ProviderStatus>?> GetProvidersAsync();
        public Task<CloudJob?> GetJobAsync(string jobId);
        public IAsyncEnumerable<CloudJob> ListJobsAsync();
        public IAsyncEnumerable<QuotaInfo> ListQuotasAsync();
        public IQuantumMachine? CreateQuantumMachine(string targetId, string storageAccountConnectionString);
        public AzureExecutionTarget? CreateExecutionTarget(string targetId);
    }
}