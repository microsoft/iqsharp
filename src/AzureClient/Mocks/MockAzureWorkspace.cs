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
using System.Threading;
using System.Runtime.CompilerServices;
using Azure.Quantum.Jobs;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureWorkspace : Microsoft.Azure.Quantum.IWorkspace
    {
        public const string NameWithMockProviders = "WorkspaceNameWithMockProviders";

        public static string[] MockJobIds { get; set; } = Array.Empty<string>();

        public static string[] MockTargetIds { get; set; } = Array.Empty<string>();

        public string WorkspaceName { get; private set; } = string.Empty;

        public string SubscriptionId { get; private set; } = string.Empty;

        public string ResourceGroupName { get; private set; } = string.Empty;

        public string Location { get; private set; } = string.Empty;

        public List<CloudJob> Jobs => MockJobIds.Select(jobId => new MockCloudJob(jobId)).ToList<CloudJob>();

        public QuantumJobClient Client => throw new NotImplementedException();

        public MockAzureWorkspace(string subscriptionId, string resourceGroup, string workspaceName, string location)
        {
            SubscriptionId = subscriptionId;
            ResourceGroupName = resourceGroup;
            WorkspaceName = workspaceName;
            Location = location;
        }

        public Task<CloudJob> SubmitJobAsync(CloudJob jobDefinition, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CloudJob> CancelJobAsync(string jobId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public async Task<CloudJob?> GetJobAsync(string jobId, CancellationToken cancellationToken = default) => await Task.Run(() => Jobs.FirstOrDefault(job => job.Id == jobId));

        public async IAsyncEnumerable<CloudJob> ListJobsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Factory.StartNew(() => Thread.Sleep(10));
            foreach (var j in Jobs)
            {
                yield return j;
            }
        }

        public async IAsyncEnumerable<QuotaInfo> ListQuotasAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Factory.StartNew(() => Thread.Sleep(10));

            foreach (var q in Enumerable.Empty<QuotaInfo>())  // No quotas for Mock workspaces.
            {
                yield return q;
            }
        }

        public async IAsyncEnumerable<ProviderStatusInfo> ListProvidersStatusAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Factory.StartNew(() => Thread.Sleep(10));

            foreach (var p in Enumerable.Empty<ProviderStatusInfo>()) // No quotas for Mock workspaces.
            {
                yield return p;
            }
        }

        public Task<string> GetSasUriAsync(string containerName, string? blobName = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

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
        }
    }
}