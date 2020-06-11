// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Microsoft.Azure.Quantum.Client.Models;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureWorkspace : IAzureWorkspace
    {
        public string Name { get; private set; }

        public List<ProviderStatus> Providers { get; } = new List<ProviderStatus>();
        
        public List<CloudJob> Jobs { get; } = new List<CloudJob>();

        public MockAzureWorkspace(string workspaceName) => Name = workspaceName;

        public async Task<CloudJob?> GetJobAsync(string jobId) => Jobs.FirstOrDefault(job => job.Id == jobId);

        public async Task<IEnumerable<ProviderStatus>?> GetProvidersAsync() => Providers;

        public async Task<IEnumerable<CloudJob>?> ListJobsAsync() => Jobs;

        public IQuantumMachine? CreateQuantumMachine(string targetId, string storageAccountConnectionString) => new MockQuantumMachine(this);

        public void AddMockJobs(params string[] jobIds)
        {
            foreach (var jobId in jobIds)
            {
                var mockJob = new MockCloudJob();
                mockJob.Details.Id = jobId;
                Jobs.Add(mockJob);
            }
        }

        public void AddMockTargets(params string[] targetIds)
        {
            var targets = targetIds.Select(id => new TargetStatus(id)).ToList();
            Providers.Add(new ProviderStatus(null, null, targets));
        }
    }
}