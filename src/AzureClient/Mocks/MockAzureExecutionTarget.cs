// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureExecutionTarget : AzureExecutionTarget
    {
        new public string PackageName => $"Microsoft.Quantum.Providers.{GetProvider(TargetId)}::0.12.20072031";

        new public static MockAzureExecutionTarget? Create(string targetId) =>
            IsValid(targetId)
            ? new MockAzureExecutionTarget() { TargetId = targetId }
            : null;
    }
}