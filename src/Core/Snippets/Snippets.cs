// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.VisualStudio.LanguageServer.Protocol;

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
        private Task<CompilerMetadata> _metadata;

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

            Reset();
            Workspace.Reloaded += OnWorkspaceReloaded;
            GlobalReferences.PackageLoaded += OnGlobalReferencesPackageLoaded; ;

            AssemblyLoadContext.Default.Resolving += Resolve;

            eventService?.TriggerServiceInitialized<ISnippets>(this);
        }

        private void Reset()
        {
            _metadata = Task.Run(LoadCompilerMetadata);
        }

        /// <summary>
        /// Triggered when a new Package has been reloaded. Needs to reset the CompilerMetadata
        /// </summary>
        private void OnGlobalReferencesPackageLoaded(object sender, PackageLoadedEventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Triggered when the Workspace has been reloaded. Needs to reset the CompilerMetadata
        /// </summary>
        private void OnWorkspaceReloaded(object sender, ReloadedEventArgs e)
        {
            Reset();
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

        public ImmutableDictionary<string, DeclarationSnippet> Declarations { get; private set; }
            = ImmutableDictionary<string, DeclarationSnippet>.Empty;

        public async Task<(bool Succeeded, List<Diagnostic> Diagnostics, IDictionary<Uri, string>? Sources)> AddOrReplaceDeclarations(IDictionary<string, DeclarationSnippet> declarationSnippets)
        {
            var newDeclarations = Declarations;
            foreach (var (name, declaration) in declarationSnippets)
            {
                newDeclarations = newDeclarations.SetItem(name, declaration);
            }

            // Try to compile. If it works, update Declarations.
            return await CompileDeclarations(newDeclarations);
        }

        private async Task<(bool Succeeded, List<Diagnostic> Diagnostics, IDictionary<Uri, string>? Sources)> CompileDeclarations(ImmutableDictionary<string, DeclarationSnippet> declarationSnippets)
        {
            // We add exactly one line of boilerplate code at the beginning of each snippet,
            // so tell the logger to subtract one from all displayed line numbers.
            var logger = new QSharpLogger(Logger, lineNrOffset: -1);

            var duration = Stopwatch.StartNew();

            IDictionary<Uri, string>? sources = null;

            try
            {
                // TODO: make async and run in bg thread.
                var assembly = Compiler.BuildSnippets(declarationSnippets, _metadata.Result, logger, Path.Combine(Workspace.CacheFolder, "__snippets__.dll"));

                if (!logger.HasErrors)
                {
                    // We succeeded, so update declarations and our assembly info.
                    AssemblyInfo = assembly.Item1;
                    Declarations = declarationSnippets;
                }
                sources = assembly.Item2;
            }
            finally
            {
                duration.Stop();
                var status = logger.HasErrors ? "error" : "ok";
                var errorIds = logger.ErrorIds.ToArray();
                SnippetCompiled?.Invoke(this, new SnippetCompiledEventArgs(status, errorIds, Compiler.AutoOpenNamespaces.Keys.ToArray(), duration.Elapsed));
            }

            return (!logger.HasErrors, logger.Logs, sources);

        }

        /// <summary>
        /// Selects the list of snippets to compile. 
        /// Basically it consumes all current Snippets except those related to `newSnippet`
        /// - either because they have the same id, or because they previously defined an operation
        /// which is in the new Snippet - and replaces them with `newSnippet` itself.
        /// </summary>
        private IEnumerable<Snippet> SelectSnippetsToCompile(string code, string? ns = null)
        {
            var ops = Compiler.IdentifyElements(code, ns).Select(Extensions.ToFullName).ToArray();
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
