// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class EntryPointOperationResolver : IOperationResolver
    {
        private IEntryPointGenerator EntryPointGenerator { get; }

        public EntryPointOperationResolver(IEntryPointGenerator entryPointGenerator) =>
            EntryPointGenerator = entryPointGenerator;

        public OperationInfo Resolve(string name) => OperationResolver.ResolveFromAssemblies(name, RelevantAssemblies());

        private IEnumerable<AssemblyInfo> RelevantAssemblies()
        {
            if (EntryPointGenerator.SnippetsAssemblyInfo is not null) yield return EntryPointGenerator.SnippetsAssemblyInfo;
            foreach (var asm in EntryPointGenerator.WorkspaceAssemblies) yield return asm;
            foreach (var asm in EntryPointGenerator.References.Assemblies) yield return asm;
        }
    }
}
