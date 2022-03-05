// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// This service is capable of building .net core assemblies on the fly from Q# code.
    /// </summary>
    public interface ICompilerService
    {
        /// <summary>
        /// Dictionary of auto-opened namespaces when compiling Q# snippets.
        /// Key is the full namespace name, value (if non-null) is the name under which the namespace is opened.
        /// </summary>
        public IDictionary<string, string> AutoOpenNamespaces { get; set; }

        /// <summary>
        /// Builds an executable assembly with an entry point that invokes the Q# operation specified
        /// by the provided <see cref="OperationInfo"/> object.
        /// </summary>
        AssemblyInfo BuildEntryPoint(OperationInfo operation, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null,
            RuntimeCapability runtimeCapability = null, bool forceQir = false);

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// </summary>
        AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null,
            RuntimeCapability runtimeCapabilities = null);

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null,
            RuntimeCapability runtimeCapability = null);

        /// <summary>
        /// Returns the names of all declared callables and types. 
        /// The compiler does this on a best effort basis, and in particular without relying on any context and/or type information, 
        /// so it will return the elements even if the compilation fails.
        /// </summary>
        IEnumerable<QsNamespaceElement> IdentifyElements(string source);

        /// <summary>
        /// Returns a dictionary of all opened namespaces. The key is the full name, and the value (if non-null) is the alias
        /// under which the namespace is opened.
        /// The compiler does this on a best effort basis, so it will return the elements even if the compilation fails. 
        /// </summary>
        IDictionary<string, string> IdentifyOpenedNamespaces(string source) => throw new NotImplementedException();
    }
}
