// Copyright (c) Microsoft Corporation. All rights reserved.
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
            if (EntryPointGenerator.SnippetsAssemblyInfo != null) yield return EntryPointGenerator.SnippetsAssemblyInfo;
            if (EntryPointGenerator.WorkspaceAssemblyInfo != null) yield return EntryPointGenerator.WorkspaceAssemblyInfo;

            foreach (var asm in EntryPointGenerator.References.Assemblies)
            {
                yield return asm;
            }
        }
    }
}
