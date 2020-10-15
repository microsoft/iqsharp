// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    ///     Resolves Q# symbols into Jupyter Core symbols to be used for
    ///     documentation requests.
    /// </summary>
    public class OperationResolver : IOperationResolver
    {
        private readonly ISnippets snippets;
        private readonly IWorkspace workspace;
        private readonly IReferences references;

        /// <summary>
        ///     Constructs a new resolver that looks for symbols in a given
        ///     collection of snippets.
        /// </summary>
        /// <param name="snippets">
        ///     The collection of snippets to be used when resolving symbols.
        /// </param>
        public OperationResolver(ISnippets snippets, IWorkspace workspace, IReferences references)
        {
            this.snippets = snippets;
            this.workspace = workspace;
            this.references = references;
        }

        /// <summary>
        ///     Enumerates over all assemblies that should be searched
        ///     when resolving symbols.
        /// </summary>
        private IEnumerable<AssemblyInfo> RelevantAssemblies()
        {
            if (snippets?.AssemblyInfo != null) yield return snippets.AssemblyInfo;
            foreach (var asm in workspace?.GetAssembliesAsync().Result) yield return asm;
            foreach (var asm in references.Assemblies) yield return asm;
        }

        /// <summary>
        ///     Resolves a given symbol name into a Q# symbol
        ///     by searching through all relevant assemblies.
        /// </summary>
        /// <returns>
        ///     The symbol instance if resolution is successful, otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        ///     If the symbol to be resolved contains a dot,
        ///     it is treated as a fully qualified name, and will
        ///     only be resolved to a symbol whose name matches exactly.
        ///     Symbol names without a dot are resolved to the first symbol
        ///     whose base name matches the given name.
        /// </remarks>
        public OperationInfo Resolve(string name) => ResolveFromAssemblies(name, RelevantAssemblies());

        public static OperationInfo ResolveFromAssemblies(string name, IEnumerable<AssemblyInfo> assemblies)
        {
            var isQualified = name.Contains('.');
            foreach (var operation in assemblies.SelectMany(asm => asm.Operations))
            {
                if (name == (isQualified ? operation.FullName : operation.Header.QualifiedName.Name.Value))
                {
                    return operation;
                }
            }

            return null;
        }
    }
}
