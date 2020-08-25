﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.Quantum.QsCompiler.CompilationBuilder;

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
        // caches the Q# compiler metadata
        private Lazy<CompilerMetadata> _metadata;

        /// <summary>
        /// Namespace that all Snippets gets compiled into.
        /// </summary>
        public static readonly string SNIPPETS_NAMESPACE = "SNIPPET";

        public Snippets(
            ICompilerService compiler, 
            IWorkspace workspace, 
            IReferences references, 
            ILogger<Snippets> logger,
            IEventService eventService)
        {
            Compiler = compiler;
            Workspace = workspace;
            GlobalReferences = references;
            Logger = logger;
            AssemblyInfo = new AssemblyInfo(null);
            Items = new Snippet[0];

            _metadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);
            Workspace.Reloaded += OnWorkspaceReloaded;
            GlobalReferences.PackageLoaded += OnGlobalReferencesPackageLoaded; ;

            AssemblyLoadContext.Default.Resolving += Resolve;

            eventService?.TriggerServiceInitialized<ISnippets>(this);
        }

        /// <summary>
        /// Triggered when a new Package has been reloaded. Needs to reset the CompilerMetadata
        /// </summary>
        private void OnGlobalReferencesPackageLoaded(object sender, PackageLoadedEventArgs e)
        {
            _metadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);
        }

        /// <summary>
        /// Triggered when the Workspace has been reloaded. Needs to reset the CompilerMetadata
        /// </summary>
        private void OnWorkspaceReloaded(object sender, ReloadedEventArgs e)
        {
            _metadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);
        }

        /// <summary>
        /// This event is triggered when a Snippet finishes compilation.
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
        public IWorkspace Workspace { get; }

        /// <summary>
        /// The assembly references that should be provided to the compiler when
        /// building all snippets.
        /// </summary>
        public IReferences GlobalReferences { get; }

        /// <summary>
        /// The service that takes care of compiling code.
        /// </summary>
        public ICompilerService Compiler { get; }

        /// <summary>
        /// Logger instance used for .net core logging.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// The list of currently available snippets.
        /// </summary>
        public IEnumerable<Snippet> Items { get; set; }

        /// <summary>
        /// The list of Q# operations available across all snippets.
        /// </summary>
        public IEnumerable<OperationInfo> Operations =>
            (Workspace == null || Workspace.HasErrors)
            ? AssemblyInfo?.Operations
            : AssemblyInfo?.Operations
            .Concat(
                Workspace?
                .Assemblies?
                .SelectMany(asm => asm?.Operations)
            );

        /// <summary>
        /// Loads the compiler metadata, either from the GlobalReferences or includes the Workspace if available
        /// </summary>
        private CompilerMetadata LoadCompilerMetadata() =>
            Workspace.HasErrors
                    ? GlobalReferences?.CompilerMetadata
                    : GlobalReferences?.CompilerMetadata.WithAssemblies(Workspace.Assemblies.ToArray());

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

            // We add exactly one line of boilerplate code at the beginning of each snippet,
            // so tell the logger to subtract one from all displayed line numbers.
            var logger = new QSharpLogger(Logger, lineNrOffset: -1);

            try
            {
                var snippets = SelectSnippetsToCompile(code).ToArray();
                var assembly = Compiler.BuildSnippets(snippets, _metadata.Value, logger, Path.Combine(Workspace.CacheFolder, "__snippets__.dll"));

                if (logger.HasErrors)
                {
                    throw new CompilationErrorsException(logger.Errors.ToArray());
                }

                foreach (var entry in Compiler.IdentifyOpenedNamespaces(code))
                {
                    Compiler.AutoOpenNamespaces[entry.Key] = entry.Value;
                }

                // populate the original snippet with the results of the compilation:
                Snippet populate(Snippet s) =>
                    new Snippet()
                    {
                        id = string.IsNullOrWhiteSpace(s.id) ? Guid.NewGuid().ToString() : s.id,
                        code = s.code,
                        warnings = logger.Logs
                            .Where(m => m.Source == CompilationUnitManager.GetFileId(s.Uri).Value)
                            .Select(logger.Format)
                            .ToArray(),
                        Elements = assembly?.SyntaxTree?
                            .SelectMany(ns => ns.Elements)
                            .Where(c => c.SourceFile() == CompilationUnitManager.GetFileId(s.Uri).Value)
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
                SnippetCompiled?.Invoke(this, new SnippetCompiledEventArgs(status, errorIds, Compiler.AutoOpenNamespaces.Keys.ToArray(), duration.Elapsed));
            }
        }

        /// <summary>
        /// Selects the list of snippets to compile. 
        /// Basically it consumes all current Snippets except those related to `newSnippet`
        /// - either because they have the same id, or because they previously defined an operation
        /// which is in the new Snippet - and replaces them with `newSnippet` itself.
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

            foreach (var asm in this.Workspace?.Assemblies)
            {
                if (name.Name == Path.GetFileNameWithoutExtension(asm?.Location))
                {
                    return asm.Assembly;
                }
            }

            return null;
        }
    }
}
