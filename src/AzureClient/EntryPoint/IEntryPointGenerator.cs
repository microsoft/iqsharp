// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Quantum.QsCompiler;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This service is capable of generating entry points for
    /// job submission to Azure Quantum.
    /// </summary>
    public interface IEntryPointGenerator
    {
        /// <summary>
        /// Gets the compiled workspace assemblies for the most recently-generated entry point.
        /// </summary>
        public AssemblyInfo[] WorkspaceAssemblies { get; }

        /// <summary>
        /// Gets the compiled snippets assembly for the most recently-generated entry point.
        /// </summary>
        public AssemblyInfo? SnippetsAssemblyInfo { get; }

        /// <summary>
        /// Gets the compiled entry point assembly for the most recently-generated entry point.
        /// </summary>
        public AssemblyInfo? EntryPointAssemblyInfo { get; }

        /// <summary>
        /// Gets the references used for compilation of the entry point assembly.
        /// </summary>
        public IReferences References { get; }

        /// <summary>
        /// Compiles an assembly and returns the <see cref="EntryPoint"/> object
        /// representing an entry point that wraps the specified operation.
        /// </summary>
        /// <param name="operationName">The name of the operation to wrap in an entry point.</param>
        /// <param name="executionTarget">The intended execution target for the compiled entry point.</param>
        /// <param name="runtimeCapabilities">The runtime capabilities of the intended execution target.</param>
        /// <returns>The generated entry point.</returns>
        public IEntryPoint Generate(string operationName, string? executionTarget,
            RuntimeCapability? runtimeCapabilities = null);
    }
}
