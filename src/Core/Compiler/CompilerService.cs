// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTree;


using QsReferences = Microsoft.Quantum.QsCompiler.CompilationBuilder.References;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Default implementation of ICompilerService.
    /// This service is capable of building .net core assemblies on the fly from Q# code.
    /// </summary>
    public class CompilerService : ICompilerService
    {
        private QSharpLogger Logger;
        private readonly CompilationUnitManager CompilationManager;
        private ImmutableHashSet<NonNullable<string>> CachedReferences;

        public CompilerService ()
        {
            this.Logger = new QSharpLogger(null);
            this.CompilationManager = new CompilationUnitManager(ex => this.Logger?.Log(ex));
            this.CachedReferences = ImmutableHashSet<NonNullable<string>>.Empty;
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// Each snippet code is wrapped inside the 'SNIPPETS_NAMESPACE' namespace and processed as a file
        /// with the same name as the snippet id.
        /// </summary>
        public AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName)
        {
            string WrapInNamespace(Snippet s) => $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ open Microsoft.Quantum.Intrinsic; open Microsoft.Quantum.Canon; {s.code} }}";

            var sources = snippets.ToDictionary(s => s.Uri,  WrapInNamespace);
            var syntaxTree = BuildQsSyntaxTree(sources.ToImmutableDictionary(), metadatas.QsMetadatas, logger);
            var assembly = BuildAssembly(sources.Keys.ToArray(), syntaxTree, metadatas.RoslynMetadatas, logger, dllName);

            return assembly;
        }

        /// <summary>
        /// Removes all currently tracked source files in the CompilationManager and replaces them with the given ones. 
        /// If the given references are not null, reloads the references loaded in by the CompilationManager 
        /// if the keys of the given references differ from the currently loaded ones.
        /// Returns an enumerable of all namespaces, including the content from both source files and references. 
        /// If generateFunctorSupport is set to true, replaces all auto-generation directives in the built syntax tree with the generated implementation.
        /// </summary>
        private IEnumerable<QsNamespace> UpdateCompilation(ImmutableDictionary<Uri, string> sources, QsReferences references = null, bool generateFunctorSupport = false) 
        {
            if (references != null && this.CachedReferences.SymmetricExcept(references.Declarations.Keys).Any())
            {
                this.CompilationManager.UpdateReferencesAsync(references);
                this.CachedReferences = references.Declarations.Keys.ToImmutableHashSet();
            }
            var currentSources = this.CompilationManager.GetSourceFiles();
            this.CompilationManager.TryRemoveSourceFilesAsync(currentSources);
            var newSources = CompilationUnitManager.InitializeFileManagers(sources);
            this.CompilationManager.AddOrUpdateSourceFilesAsync(newSources);

            var built = this.CompilationManager.Build();
            var compilation = built.BuiltCompilation; 
            if (generateFunctorSupport)
            {
                CodeGeneration.GenerateFunctorSpecializations(compilation, out compilation);
            }

            var diagnostics = this.CompilationManager.GetDiagnostics().SelectMany(d => d.Diagnostics);
            foreach (var msg in diagnostics)
            {
                this.Logger?.Log(msg);
            }

            return compilation.Namespaces;
        }

        /// <summary>
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails.
        /// </summary>
        public IEnumerable<QsNamespaceElement> IdentifyElements(string source)
        {
            var snippetNs = Snippets.SNIPPETS_NAMESPACE;
            this.Logger = new QSharpLogger(null);

            var sources = new Dictionary<Uri, string>() { { new Uri($"file:///temp"), $"namespace {snippetNs} {{ {source} }}" } }.ToImmutableDictionary();
            var loaded = this.UpdateCompilation(sources);
            return loaded.FirstOrDefault(ns => ns.Name.Value == snippetNs)?.Elements;
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        public AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName)
        {
            var syntaxTree = BuildQsSyntaxTree(files, metadatas.QsMetadatas, logger);
            Uri FileUri(string f) => CompilationUnitManager.TryGetUri(NonNullable<string>.New(f), out var uri) ? uri : null;
            return BuildAssembly(files.Select(FileUri).ToArray(), syntaxTree, metadatas.RoslynMetadatas, logger, dllName);
        }

        /// <summary>
        /// Builds the Q# syntax tree from the given files.
        /// The files are given as a list of filenames, as per the format expected by
        /// the <see cref="ProjectManager.LoadSourceFiles(IEnumerable{string}, Action{VisualStudio.LanguageServer.Protocol.Diagnostic}, Action{Exception})" />
        /// method.
        /// </summary>
        private QsNamespace[] BuildQsSyntaxTree(string[] files, QsReferences references, QSharpLogger logger)
        {
            var sources = ProjectManager.LoadSourceFiles(files, d => logger?.Log(d), ex => logger?.Log(ex));
            return BuildQsSyntaxTree(sources, references, logger);
        }

        /// <summary>
        /// Builds the Q# syntax tree from the given files/source paris.
        /// </summary>
        private QsNamespace[] BuildQsSyntaxTree(ImmutableDictionary<Uri, string> sources, QsReferences references, QSharpLogger logger)
        {
            this.Logger = logger;
            return this.UpdateCompilation(sources, references, true).ToArray();
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the Q# syntax tree.
        /// </summary>
        private static AssemblyInfo BuildAssembly(Uri[] fileNames, QsNamespace[] syntaxTree, IEnumerable<MetadataReference> references, QSharpLogger logger, string targetDll)
        {
            if (logger.HasErrors) return null;

            logger.LogDebug($"Compiling the following Q# files: {string.Join(",", fileNames.Select(f => f.LocalPath))}");

            try
            {
                // Generate C# simulation code from Q# syntax tree and convert it into C# syntax trees:
                var trees = new List<SyntaxTree>();
                NonNullable<string> GetFileId(Uri uri) => CompilationUnitManager.TryGetFileId(uri, out var id) ? id : NonNullable<string>.New(uri.AbsolutePath);
                foreach (var file in fileNames)
                {
                    var sourceFile = GetFileId(file);
                    var code = SimulationCode.generate(sourceFile, syntaxTree);
                    var tree = CSharpSyntaxTree.ParseText(code, encoding: UTF8Encoding.UTF8);
                    trees.Add(tree);
                    logger.LogDebug($"Generated the following C# code for {sourceFile.Value}:\n=============\n{code}\n=============\n");
                }

                // Compile the C# syntax trees:
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(targetDll),
                    trees,
                    references,
                    options);

                // Generate the assembly from the C# compilation:
                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        logger.LogError("IQS000", "Could not compile Roslyn dll from working folder.");

                        foreach (Diagnostic diagnostic in failures)
                        {
                            logger.LogError(diagnostic.Id, diagnostic.GetMessage());
                        }

                        return null;
                    }
                    else
                    {
                        logger.LogDebug($"Assembly successfully generated. Caching at {targetDll}.");
                        var data = ms.ToArray();

                        try
                        {
                            File.WriteAllBytes(targetDll, data);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("IQS001", $"Unable to save assembly cache: {e.Message}.");
                        }

                        return new AssemblyInfo(Assembly.Load(data), targetDll, syntaxTree);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("IQS002", $"Unexpected error compiling assembly: {e.Message}.");
                return null;
            }
        }
    }
}
