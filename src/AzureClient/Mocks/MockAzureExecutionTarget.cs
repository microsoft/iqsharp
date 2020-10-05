// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureExecutionTarget : AzureExecutionTarget
    {
        public override string PackageName => $"Microsoft.Quantum.Providers.{GetProvider(TargetId)}::0.12.20082414-beta";

        public static MockAzureExecutionTarget? CreateMock(string targetId) =>
            IsValid(targetId)
            ? new MockAzureExecutionTarget() { TargetId = targetId }
            : null;
    }
}
