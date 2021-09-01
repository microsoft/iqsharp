// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// List of arguments that are part of the Compile Snippet event.
    /// </summary>
    public class SnippetCompiledEventArgs : EventArgs
    {
        public SnippetCompiledEventArgs(string status, string[] errors, string[] namespaces, TimeSpan duration)
        {
            this.Status = status;
            this.Errors = errors;
            this.Namespaces = namespaces;
            this.Duration = duration;
        }

        /// <summary>
        /// The workspace status. Can be "success" or "error"
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// The list of error ids reported by the Q# parser (if any).
        /// </summary>
        public string[] Errors { get; }

        /// <summary>
        /// The list of namespaces opened automatically when compiling the snippet.
        /// </summary>
        public string[] Namespaces { get; }

        /// <summary>
        /// The total time the reload operation took.
        /// </summary>
        public TimeSpan Duration { get; }
    }

    public record DeclarationSnippet(string Source, ImmutableList<(string Name, string? Alias)> OpenNamespaces)
    {
        public DeclarationSnippet(string Source, IEnumerable<(string Name, string? Alias)> OpenNamespaces)
        : this(Source, OpenNamespaces.ToImmutableList())
        { }
    }

    /// <summary>
    ///  Snippets represent pieces of Q# code provided by the user.
    ///  These snippets are efemeral thus not part of the Workspace.
    ///  This service keeps track of the Snippets provided by the user and
    ///  compiles all of them into a single Assembly that can then be used for execution.
    /// </summary>
    public interface ISnippets
    {
        /// <summary>
        /// This event is triggered when a Snippet finishes compilation.
        /// </summary>
        event EventHandler<SnippetCompiledEventArgs> SnippetCompiled;

        /// <summary>
        /// The information of the assembly compiled from all the given snippets
        /// </summary>
        AssemblyInfo AssemblyInfo { get; }

        /// <summary>
        /// The list of currently available snippets.
        /// </summary>
        IEnumerable<Snippet> Items { get; set; }

        Task<(bool Succeeded, List<Diagnostic> Diagnostics, IDictionary<Uri, string>? Sources)> AddOrReplaceDeclarations(IDictionary<string, DeclarationSnippet> declarationSnippets);

        ImmutableDictionary<string, DeclarationSnippet> Declarations { get; }

        /// <summary>
        /// The list of operations found in all snippets compiled successfully so far.
        /// </summary>
        IEnumerable<OperationInfo> Operations { get; }

    }
}
