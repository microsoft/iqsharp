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
            _id = id ?? string.Empty;
        }

        public override string Id => _id;

        private static string CreateMockOutputFileUri()
        {
            var tempFilePath = Path.GetTempFileName();
            using var outputFile = new StreamWriter(tempFilePath);
            outputFile.WriteLine(@"{'Histogram':['0',0.5,'1',0.5]}");
            return new Uri(tempFilePath).AbsoluteUri;
        }
    }
}