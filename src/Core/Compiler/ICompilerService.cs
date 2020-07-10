// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler.SyntaxTree;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// This service is capable of building .net core assemblies on the fly from Q# code.
    /// </summary>
    public interface ICompilerService
    {
        /// <summary>
        /// List of auto-opened namespaces when compiling Q# snippets.
        /// </summary>
        public ISet<string> AutoOpenNamespaces { get; set; }

        /// <summary>
        /// Builds an executable assembly with an entry point that invokes the Q# operation specified
        /// by the provided <see cref="OperationInfo"/> object.
        /// </summary>
        AssemblyInfo BuildEntryPoint(OperationInfo operation, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null);

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// </summary>
        AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null);

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null);

        /// <summary>
        /// Returns the names of all declared callables and types. 
        /// The compiler does this on a best effort basis, and in particular without relying on any context and/or type information, 
        /// so it will return the elements even if the compilation fails.
        /// </summary>
        IEnumerable<QsNamespaceElement> IdentifyElements(string source);

        /// <summary>
        /// Returns the names of all opened namespaces.
        /// The compiler does this on a best effort basis, so it will return the elements even if the compilation fails. 
        /// </summary>
        IEnumerable<string> IdentifyOpenedNamespaces(string source) => throw new NotImplementedException();
    }
}
