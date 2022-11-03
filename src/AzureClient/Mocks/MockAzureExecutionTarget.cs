// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Quantum;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Runtime.Submitters;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal record MockAzureExecutionTarget : AzureExecutionTarget
    {
        MockAzureExecutionTarget(TargetStatusInfo target) 
            : this(target?.TargetId)
        { }

        MockAzureExecutionTarget(string? targetId) 
            : base(targetId)
        { }

        // We test using a non-QDK package name to avoid possible version conflicts.
        public override string PackageName => "Microsoft.Extensions.DependencyInjection";

        public static MockAzureExecutionTarget? CreateMock(TargetStatusInfo target) =>
            CreateMock(target?.TargetId);

        public static MockAzureExecutionTarget? CreateMock(string? targetId) =>
            IsValid(targetId)
            ? new MockAzureExecutionTarget(targetId)
            : null;


        public override bool TryGetQirSubmitter(Azure.Quantum.IWorkspace workspace, string storageConnectionString, TargetCapability? targetCapability, [NotNullWhen(true)] out IQirSubmitter? submitter)
        {
            if (this.TargetId?.EndsWith("mock-qir") ?? false)
            {
                submitter = new MockQirSubmitter(new List<Argument>());
                return true;
            }
            else
            {
                submitter = null;
                return false;
            }
        }
    }
}
