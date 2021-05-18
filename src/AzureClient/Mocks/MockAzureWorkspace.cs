// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Quantum;
using Azure.Quantum.Jobs.Models;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureWorkspace : IAzureWorkspace
    {
        public const string NameWithMockProviders = "WorkspaceNameWithMockProviders";

        public static string[] MockJobIds { get; set; } = Array.Empty<string>();

        public static string[] MockTargetIds { get; set; } = Array.Empty<string>();

        public string Name { get; private set; } = string.Empty;

        public string SubscriptionId { get; private set; } = string.Empty;

        public string ResourceGroup { get; private set; } = string.Empty;

        public string Location { get; private set; } = string.Empty;

        public List<ProviderStatus> Providers => new List<ProviderStatus>();

        public List<CloudJob> Jobs => MockJobIds.Select(jobId => new MockCloudJob(jobId)).ToList<CloudJob>();

        public MockAzureWorkspace(string workspaceName, string location)
        {
            Name = workspaceName;
            Location = location;
        }

        public async Task<CloudJob?> GetJobAsync(string jobId) => await Task.Run(() => Jobs.FirstOrDefault(job => job.Id == jobId));

        public async Task<IEnumerable<ProviderStatus>?> GetProvidersAsync() => await Task.Run(() => Providers);

        public async Task<IEnumerable<CloudJob>?> ListJobsAsync() => await Task.Run(() => Jobs);

        public async Task<IEnumerable<QuotaInfo>?> ListQuotasAsync() => await Task.Run(() => new List<QuotaInfo>());

        public IQuantumMachine? CreateQuantumMachine(string targetId, string storageAccountConnectionString) => new MockQuantumMachine(this);

        public AzureExecutionTarget? CreateExecutionTarget(string targetId) => MockAzureExecutionTarget.CreateMock(targetId);

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
            //var targets = targetIds.Select(id => new TargetStatus(id)).ToList();
            //Providers.Add(new ProviderStatus(null, null, targets));
        }
    }
}