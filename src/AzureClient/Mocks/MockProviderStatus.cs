// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;

using Microsoft.Azure.Quantum;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockProviderStatus : ProviderStatusInfo
    {
        private string _id;

        public MockProviderStatus(global::Microsoft.Azure.Quantum.IWorkspace ws, string? id = null)
            : base()
        {
            _id = id ?? string.Empty;
        }

        public override string ProviderId => _id;

        public override IEnumerable<TargetStatusInfo> Targets =>
            new[] { 
                new MockTargetStatus(_id.ToLower() + "." + "simulator"),
                new MockTargetStatus(_id.ToLower() + "." + "mock")
            };
    }
}