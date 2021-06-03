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
    internal class MockProviderStatus : ProviderStatusInfo
    {
        private class MockTargetStatus : TargetStatusInfo
        {
            public MockTargetStatus(string id) : base(null)
            {
                this.TargetId = id;
            }

            public override string TargetId { get; }
        }

        private string _id;

        public MockProviderStatus(global::Microsoft.Azure.Quantum.IWorkspace ws, string? id = null)
            : base(ws, null)
        {
            _id = id ?? string.Empty;
        }

        public override string ProviderId => _id;

        public override IEnumerable<TargetStatusInfo> Targets =>
            new[] { new MockTargetStatus(_id.ToLower() + "." + "target") };
    }
}