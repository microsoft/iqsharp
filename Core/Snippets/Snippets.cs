// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Default implementation of ISnippets.
    ///  Snippets represent pieces of Q# code provided by the user.
    ///  These snippets are efemeral thus not part of the Workspace.
    ///  This service keeps track of the Snippets provided by the user and
    ///  compiles all of them into a single Assembly that can then be used for execution.
    /// </summary>
    public class Snippets : ISnippets
    {
        /// <summary>
        /// Namespace that all Snippets gets comipled into.
        /// </summary>
        public static readonly string SNIPPETS_NAMESPACE = "SNIPPET";

        public Snippets(ICompilerService compiler, IWorkspace workspace, IReferences references, ILogger<Snippets> logger)
        {
            Compiler = compiler;
            Workspace = workspace;
            GlobalReferences = references;
            Logger = logger;
            AssemblyInfo = new AssemblyInfo(null);
            Items = new Snippet[0];

            AssemblyLoadContext.Default.Resolving += Resolve;
        }

        /// <summary>
        /// This event is triggered when after a Snippet finishes compilation.
        /// </summary>
        public event EventHandler<SnippetCompiledEventArgs> SnippetCompiled;

        /// <summary>
        /// The information of the assembly compiled from all the given snippets
        /// </summary>
        public AssemblyInfo AssemblyInfo { get; set; }

        /// <summary>
        ///  The Workspace these Snippets depend on. Snippets may call operations
        ///  defined in this Workspace.
        /// </summary>
        internal IWorkspace Workspace { get; }

        /// <summary>
        ///  The Workspace these Snippets depend on. Snippets may call operations
        ///  defined in this Workspace.
        /// </summary>
        internal IReferences GlobalReferences { get; }

        /// <summary>
        /// The service that takes care of compiling code.
        /// </summary>
        internal ICompilerService Compiler { get; }

        /// <summary>
        /// Logger instance used for .net core logging.
        /// </summary>
        internal ILogger Logger { get; }

        /// <summary>
        /// The list of current available snippets 
        /// </summary>
        internal IEnumerable<Snippet> Items { get; set; }

        /// <summary>
        /// The list of Q# operations available across all snippets.
        /// </summary>
        public IEnumerable<OperationInfo> Operations =>
            (Workspace == null || Workspace.HasErrors)
            ? AssemblyInfo?.Operations
            : AssemblyInfo?.Operations
            .Concat(
                Workspace
                ?.AssemblyInfo
                ?.Operations
            );

        /// <summary>
        /// Compiles the given code. 
        /// If the operations defined in this code are already defined
        /// in existing Snippets, those Snippets are skipped. 
        /// If successful, this updates the AssemblyInfo
        /// with the new operations found in the Snippet.
        /// If errors are found during compilation, a `CompilationErrorsException` is triggered
        /// with the list of errors found.
        /// If successful, the list of snippets is updated to include those that were part of the 
        /// compilation and it will return a new Snippet with the warnings and Q# elements
        /// reported by the compiler.
        /// </summary>
        public Snippet Compile(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException(nameof(code));

            var duration = Stopwatch.StartNew();
            var logger = new QSharpLogger(Logger);

            try
            {
                var snippets = SelectSnippetsToCompile(code).ToArray();
                var references = Workspace.HasErrors
                    ? GlobalReferences.CompilerMetadata
                    : GlobalReferences?.CompilerMetadata.WithAssemblies(Workspace.AssemblyInfo);
                var assembly = Compiler.BuildSnippets(snippets, references, logger, Path.Combine(Workspace.CacheFolder, "__snippets__.dll"));

                if (logger.HasErrors)
                {
                    throw new CompilationErrorsException(logger.Errors.ToArray());
                }

                // populate the original snippet with the results of the compilation:
                Snippet populate(Snippet s) =>
                    new Snippet()
                    {
                        id = string.IsNullOrWhiteSpace(s.id) ? System.Guid.NewGuid().ToString() : s.id,
                        code = s.code,
                        warnings = logger.Logs.Where(m => m.Source == s.Uri.AbsolutePath).Select(logger.Format).ToArray(),
                        Elements = assembly?.SyntaxTree?
                            .SelectMany(ns => ns.Elements)
                            .Where(c => c.SourceFile() == s.Uri.AbsolutePath)
                            .ToArray()
                    };

                AssemblyInfo = assembly;
                Items = snippets.Select(populate).ToArray();

                return Items.Last();
            }
            finally
            {
                duration.Stop();
                var status = logger.HasErrors ? "error" : "ok";
                var errorIds = logger.ErrorIds .ToArray();
                SnippetCompiled?.Invoke(this, new SnippetCompiledEventArgs(status, errorIds, duration.Elapsed));
            }
        }

        /// <summary>
        /// Selects the list of snippets to compile. 
        /// Basically it consumes all current Snippets except those related to `newSnippet`
        /// - either because they have the same id, or because they previously defined an operation
        /// which is in the new Snippet- and replaced with `newSnippet` itself.
        /// </summary>
        private IEnumerable<Snippet> SelectSnippetsToCompile(string code)
        {
            var ops = Compiler.IdentifyElements(code).Select(Extensions.ToFullName).ToArray();
            var snippetsWithNoOverlap = Items.Where(s => !s.Elements.Select(Extensions.ToFullName).Intersect(ops).Any());

            return snippetsWithNoOverlap.Append(new Snippet { code = code });
        }

        /// <summary>
        /// Because the assemblies are loaded into memory, we need to provide this method to the AssemblyLoadContext
        /// such that the Workspace assembly or this assembly is correctly resolved when it is executed for simulation.
        /// </summary>
        public Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            if (name.Name == Path.GetFileNameWithoutExtension(this.AssemblyInfo?.Location))
            {
                return this.AssemblyInfo.Assembly;
            }
            else if (name.Name == Path.GetFileNameWithoutExtension(this.Workspace?.AssemblyInfo?.Location))
            {
                return this.Workspace.AssemblyInfo.Assembly;
            }

            return null;
        }
    }
}
