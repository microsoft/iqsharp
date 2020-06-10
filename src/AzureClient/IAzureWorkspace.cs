// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal interface IAzureWorkspace
    {
        public string Name { get; }

        public Task<IEnumerable<ProviderStatus>?> GetProvidersAsync();
        public Task<CloudJob?> GetJobAsync(string jobId);
        public Task<IEnumerable<CloudJob>?> ListJobsAsync();
        public IQuantumMachine? CreateQuantumMachine(string targetId, string storageAccountConnectionString);
    }
}