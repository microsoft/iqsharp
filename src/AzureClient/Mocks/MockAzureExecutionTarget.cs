// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Azure.Quantum;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockAzureExecutionTarget : AzureExecutionTarget
    {
        MockAzureExecutionTarget(TargetStatusInfo target) 
            : base(target?.TargetId)
        { }

        // We test using a non-QDK package name to avoid possible version conflicts.
        public override string PackageName => "Microsoft.Extensions.DependencyInjection";

        public static MockAzureExecutionTarget? CreateMock(TargetStatusInfo target) =>
            IsValid(target)
            ? new MockAzureExecutionTarget(target)
            : null;
    }
}
