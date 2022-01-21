// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Azure.Quantum;
using Azure.Quantum.Jobs.Models;
using System;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This class is a wrapper of the Azure Quantum SDK data model class
    /// `Azure.Quantum.Jobs.Models.CostEstimate`
    /// The data model is auto-generated and the generated classes have immutable
    /// properties (with only getters), and only internal constructors.
    /// To allow us to mock the class and initialize objects for unit tests,
    /// we use the MockHelper to instantiate from an internal constructor
    /// and to set/initialize immutable properties.
    /// </summary>
    public class MockCostEstimate
    {
        public MockCostEstimate(string currencyCode, IReadOnlyList<UsageEvent> events, float? estimatedTotal)
        {
            this.InternalCostEstimate = MockHelper.CreateWithNonPublicConstructor<CostEstimate>(currencyCode, events, estimatedTotal);
        }

        public string CurrencyCode
        {
            get => this.InternalCostEstimate.CurrencyCode;
            set => this.InternalCostEstimate.SetReadOnlyProperty<CostEstimate>(nameof(CurrencyCode), value);
        }

        public IReadOnlyList<UsageEvent> Events
        {
            get => this.InternalCostEstimate.Events;
        }

        public float? EstimatedTotal
        {
            get => this.InternalCostEstimate.EstimatedTotal;
            set => this.InternalCostEstimate.SetReadOnlyProperty<CostEstimate>(nameof(EstimatedTotal), value);
        }

        public CostEstimate InternalCostEstimate { get; }
    }

    /// <summary>
    /// This class is a mock of the Azure Quantum SDK data model class
    /// `Azure.Quantum.Jobs.Models.JobDetails`
    /// The data model is auto-generated and the generated classes have immutable
    /// properties (with only getters), and no way to initialize them.
    /// To allow us to mock the class and initialize objects for unit tests,
    /// we use the MockHelper to set/initialize immutable properties.
    /// </summary>
    internal class MockJobDetails : JobDetails
    {
        public MockJobDetails(string containerUri, string inputDataFormat, string providerId, string target)
            : base(containerUri: containerUri, 
                   inputDataFormat: inputDataFormat, 
                   providerId: providerId, 
                   target: target)
        {
        }

        private MockCostEstimate? costEstimate;
        public new MockCostEstimate? CostEstimate
        {
            get => this.costEstimate;
            set
            {
                this.costEstimate = value;
                this.SetReadOnlyProperty<JobDetails>(nameof(CostEstimate), value?.InternalCostEstimate);
            }
        }

        public new DateTimeOffset? CancellationTime
        {
            get => base.CancellationTime;
            set => this.SetReadOnlyProperty<JobDetails>(nameof(CancellationTime), value);
        }

        public new DateTimeOffset? EndExecutionTime
        {
            get => base.EndExecutionTime;
            set => this.SetReadOnlyProperty<JobDetails>(nameof(EndExecutionTime), value);
        }

        public new DateTimeOffset? BeginExecutionTime
        {
            get => base.BeginExecutionTime;
            set => this.SetReadOnlyProperty<JobDetails>(nameof(BeginExecutionTime), value);
        }

        public new DateTimeOffset? CreationTime
        {
            get => base.CreationTime;
            set => this.SetReadOnlyProperty<JobDetails>(nameof(CreationTime), value);
        }

        public new JobStatus? Status
        {
            get => base.Status;
            set => this.SetReadOnlyProperty<JobDetails>(nameof(Status), value);
        }
    }

    internal class MockCloudJob : CloudJob
    {
        private string _id;
        private string? _outputFile;

        public MockCloudJob(string? id = null)
            : base(
                new MockAzureWorkspace("mockSubscriptionId", "mockResourceGroupName", "mockWorkspaceName", "mockLocation"),
                new MockJobDetails(
                    containerUri: string.Empty,
                    inputDataFormat: string.Empty,
                    providerId: string.Empty,
                    target: string.Empty
                ))
        {
            _id = id ?? Guid.NewGuid().ToString();
        }

        public override string Id => _id;

        public override string Status => JobStatus.Succeeded.ToString();

        public override Uri OutputDataUri
        {
            get
            {
                if (_outputFile == null)
                {
                    var path = Path.GetTempFileName();
                    using var outputFile = new StreamWriter(path);
                    outputFile.WriteLine(@"{'Histogram':['0',0.5,'1',0.5]}");

                    _outputFile = path;
                }

                return new Uri(_outputFile);
            }
        }

        public new MockJobDetails Details
        {
            get => (MockJobDetails)base.Details;
            set => this.SetReadOnlyProperty(nameof(Details), value);
        }
    }
}