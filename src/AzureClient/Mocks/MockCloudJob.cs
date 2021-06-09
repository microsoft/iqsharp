// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Azure.Quantum;
using Azure.Quantum.Jobs.Models;
using System;
using System.IO;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockCloudJob : CloudJob
    {
        private string _id;
        private string? _outputFile;

        public MockCloudJob(string? id = null)
            : base(
                new Azure.Quantum.Workspace("mockSubscriptionId", "mockResourceGroupName", "mockWorkspaceName", "mockLocation"),
                new JobDetails(
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
    }
}