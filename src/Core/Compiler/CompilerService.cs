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
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Newtonsoft.Json.Bson;
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
        private ImmutableHashSet<Uri> LoadedFileManagers;
        private ImmutableHashSet<NonNullable<string>> LoadedReferences;
        
        public CompilerService()
        {
            this.Logger = new QSharpLogger(null);
            this.CompilationManager = new CompilationUnitManager(ex => this.Logger?.Log(ex));
        }

        /// <summary>
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails.
        /// </summary>
        public IEnumerable<QsQualifiedName> IdentifyElements(string source)
        {
            var uri = new Uri(Path.GetFullPath("__CODE_SNIPPET__.qs"));
            var nsName = NonNullable<string>.New(Snippets.SNIPPETS_NAMESPACE);
            var content = $"namespace {nsName.Value} {{ {source} }}";
            var fileManager = CompilationUnitManager.InitializeFileManager(uri, content);
            var definedCallables = fileManager.GetCallableDeclarations();
            var definedTypes = fileManager.GetTypeDeclarations();
            return definedCallables.Concat(definedTypes).Select(decl => new QsQualifiedName(nsName, decl.Item1));
        }

        /// <summary> 
        /// Compiles the given Q# code and returns the list of elements found in it. 
        /// Removes all currently tracked source files in the CompilationManager and replaces them with the given ones.  
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails. 
        /// If the given references are not null, reloads the references loaded in by the CompilationManager  
        /// if the keys of the given references differ from the currently loaded ones. 
        /// Returns an enumerable of all namespaces, including the content from both source files and references.  
        /// </summary> 
        private QsCompilation UpdateCompilation(ImmutableDictionary<Uri, string> sources, QsReferences references = null, QSharpLogger logger = null)
        {
            this.Logger = logger;
            var currentSources = this.LoadedFileManagers ?? ImmutableHashSet<Uri>.Empty;
            var currentReferences = this.LoadedReferences ?? ImmutableHashSet<NonNullable<string>>.Empty;
            var updatedRefs = references != null && currentReferences.SymmetricExcept(references.Declarations.Keys).Any();
            
            // update source files 
            var files = CompilationUnitManager.InitializeFileManagers(sources, onException: ex => this.Logger?.Log(ex));
            this.CompilationManager.TryRemoveSourceFilesAsync(currentSources.Except(sources.Keys), suppressVerification: true);
            this.CompilationManager.AddOrUpdateSourceFilesAsync(files, suppressVerification: updatedRefs);
            this.LoadedFileManagers = sources.Keys.ToImmutableHashSet();
            
            // update references
            if (updatedRefs) this.CompilationManager.UpdateReferencesAsync(references);
            var compilation = this.CompilationManager.Build();
            this.LoadedReferences = compilation.References;
            
            // generate functor support and log diagnostics
            var diagnostics = compilation.Diagnostics();
            this.Logger?.Log(diagnostics.ToArray());
            if (!CodeGeneration.GenerateFunctorSpecializations(compilation.BuiltCompilation, out var built))
            {
                this.Logger?.Log(Errors.LoadError(ErrorCode.FunctorGenerationFailed, new string[0], null));
            }
            
            return built;
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// Each snippet code is wrapped inside the 'SNIPPETS_NAMESPACE' namespace and processed as a file
        /// with the same name as the snippet id.
        /// </summary>
        public AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName)
        {
            string WrapInNamespace(Snippet s) =>
                $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ open Microsoft.Quantum.Intrinsic; open Microsoft.Quantum.Canon; {s.code} }}";

            var sources = snippets.ToImmutableDictionary(s => s.Uri, WrapInNamespace);
            return BuildAssembly(sources, metadatas, logger, dllName);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        public AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName)
        {
            var sources = ProjectManager.LoadSourceFiles(files, d => logger?.Log(d), ex => logger?.Log(ex));
            return BuildAssembly(sources, metadatas, logger, dllName);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the Q# syntax tree.
        /// </summary>
        private AssemblyInfo BuildAssembly(ImmutableDictionary<Uri, string> sources, CompilerMetadata metadata, QSharpLogger logger, string dllName)
        {
            logger.LogDebug($"Compiling the following Q# files: {string.Join(",", sources.Keys.Select(f => f.LocalPath))}");

            var qsCompilation = this.UpdateCompilation(sources, metadata.QsMetadatas, logger);
            if (logger.HasErrors) return null;

            try
            {
                // Generate C# simulation code from Q# syntax tree and convert it into C# syntax trees:
                var trees = new List<SyntaxTree>();
                NonNullable<string> GetFileId(Uri uri) => CompilationUnitManager.TryGetFileId(uri, out var id) ? id : NonNullable<string>.New(uri.AbsolutePath);
                foreach (var file in sources.Keys)
                {
                    var sourceFile = GetFileId(file);
                    var code = SimulationCode.generate(sourceFile, qsCompilation.Namespaces);
                    var tree = CSharpSyntaxTree.ParseText(code, encoding: UTF8Encoding.UTF8);
                    trees.Add(tree);
                    logger.LogDebug($"Generated the following C# code for {sourceFile.Value}:\n=============\n{code}\n=============\n");
                }

                // Compile the C# syntax trees:
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(dllName),
                    trees,
                    metadata.RoslynMetadatas,
                    options);

                // Generate the assembly from the C# compilation:
                using (var ms = new MemoryStream())
                using (var bsonStream = new MemoryStream())
                {
                    using var writer = new BsonDataWriter(bsonStream) { CloseOutput = false };
                    var fromSources = qsCompilation.Namespaces.Select(ns => FilterBySourceFile.Apply(ns, s => s.Value.EndsWith(".qs")));
                    Json.Serializer.Serialize(writer, new QsCompilation(fromSources.ToImmutableArray(), qsCompilation.EntryPoints));

                    var resourceDescription = new ResourceDescription
                    (
                        resourceName: QsCompiler.ReservedKeywords.DotnetCoreDll.ResourceName,
                        dataProvider: () => new MemoryStream(bsonStream.ToArray()), 
                        isPublic: true
                    );


                    var result = compilation.Emit(ms, manifestResources: new[] { resourceDescription });

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
                        logger.LogDebug($"Assembly successfully generated. Caching at {dllName}.");
                        var data = ms.ToArray();

                        try
                        {
                            File.WriteAllBytes(dllName, data);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("IQS001", $"Unable to save assembly cache: {e.Message}.");
                        }

                        return new AssemblyInfo(Assembly.Load(data), dllName, fromSources.ToArray());
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
