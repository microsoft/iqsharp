// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// </summary>
        AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName);

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName);

        /// <summary>
        /// Returns the names of all declared callables and types. 
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails. 
        /// The compiler does this on a best effort, and in particular without relying on any context and/or type information, 
        /// so it will return the elements even if the compilation fails.
        IEnumerable<QsNamespaceElement> IdentifyElements(string source);
    }
}
