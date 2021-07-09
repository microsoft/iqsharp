// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Azure.Quantum;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockTargetStatus : TargetStatusInfo
    {
        public MockTargetStatus(string id) : base()
        {
            this.TargetId = id;
        }

        public override string TargetId { get; }
    }
}