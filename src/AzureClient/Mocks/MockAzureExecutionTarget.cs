// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureExecutionTarget : AzureExecutionTarget
    {
        // We test using a non-QDK package name to avoid possible version conflicts.
        public override string PackageName => "Microsoft.Extensions.DependencyInjection";

        public static MockAzureExecutionTarget? CreateMock(string targetId) =>
            IsValid(targetId)
            ? new MockAzureExecutionTarget() { TargetId = targetId }
            : null;
    }
}
