// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Quantum.IQSharp.Common;

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
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails.
        /// </summary>
        IEnumerable<QsCompiler.SyntaxTree.QsNamespaceElement> IdentifyElements(string source);
    }
}
