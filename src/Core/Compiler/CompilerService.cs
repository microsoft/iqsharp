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
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
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
        public IEnumerable<string> AutoOpenNamespaces { get; set; } = new String[]
        {
            "Microsoft.Quantum.Intrinsic",
            "Microsoft.Quantum.Canon"
        };

        /// <summary>
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails.
        /// </summary>
        public IEnumerable<QsNamespaceElement> IdentifyElements(string source)
        {
            var uri = new Uri(Path.GetFullPath("__CODE_SNIPPET__.qs"));
            var ns = NonNullable<string>.New(Snippets.SNIPPETS_NAMESPACE);
            var sources = new Dictionary<Uri, string>() { { uri, $"namespace {ns.Value} {{ {source} }}" } }.ToImmutableDictionary();
            var loadOptions = new CompilationLoader.Configuration();
            var loaded = new CompilationLoader(_ => sources, _ => QsReferences.Empty, loadOptions);
            if (loaded.VerifiedCompilation == null) { return ImmutableArray<QsNamespaceElement>.Empty; }
            return loaded.VerifiedCompilation.SyntaxTree.TryGetValue(ns, out var tree)
                   ? tree.Elements
                   : ImmutableArray<QsNamespaceElement>.Empty;
        }

        /// <summary> 
        /// Compiles the given Q# code and returns the list of elements found in it. 
        /// Removes all currently tracked source files in the CompilationManager and replaces them with the given ones.  
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails. 
        /// If the given references are not null, reloads the references loaded in by the CompilationManager  
        /// if the keys of the given references differ from the currently loaded ones. 
        /// Returns an enumerable of all namespaces, including the content from both source files and references.  
        /// </summary> 
        private QsCompilation UpdateCompilation(ImmutableDictionary<Uri, string> sources, QsReferences references = null, QSharpLogger logger = null, bool compileAsExecutable = false)
        {
            var loadOptions = new CompilationLoader.Configuration
            {
                GenerateFunctorSupport = true,
                IsExecutable = compileAsExecutable
            };
            var loaded = new CompilationLoader(_ => sources, _ => references, loadOptions, logger);
            return loaded.CompilationOutput;
        }

        /// <inheritdoc/>
        public AssemblyInfo BuildEntryPoint(OperationInfo operation, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null)
        {
            var signature = operation.Header.PrintSignature();
            var argumentTuple = SyntaxTreeToQsharp.ArgumentTuple(operation.Header.ArgumentTuple, type => type.ToString(), symbolsOnly: true);

            var entryPointUri = new Uri(Path.GetFullPath(Path.Combine("/", $"entrypoint.qs")));
            var entryPointSnippet = @$"namespace ENTRYPOINT
                {{
                    open {operation.Header.QualifiedName.Namespace.Value};
                    @{BuiltIn.EntryPoint.FullName}()
                    operation {signature}
                    {{
                        return {operation.Header.QualifiedName}{argumentTuple};
                    }}
                }}";

            var sources = new Dictionary<Uri, string>() {{ entryPointUri, entryPointSnippet }}.ToImmutableDictionary();
            return BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: true, executionTarget: executionTarget);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// Each snippet code is wrapped inside the 'SNIPPETS_NAMESPACE' namespace and processed as a file
        /// with the same name as the snippet id.
        /// </summary>
        public AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null)
        {
            string open_stmtms = string.Join("", AutoOpenNamespaces.Select(ns => $"open {ns};"));
                
            string WrapInNamespace(Snippet s) =>
                $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ {open_stmtms} {s.code} }}";

            var sources = snippets.ToImmutableDictionary(s => s.Uri, WrapInNamespace);
            return BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: false, executionTarget: executionTarget);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        public AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string executionTarget = null)
        {
            var sources = ProjectManager.LoadSourceFiles(files, d => logger?.Log(d), ex => logger?.Log(ex));
            return BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: false, executionTarget: executionTarget);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the Q# syntax tree.
        /// </summary>
        private AssemblyInfo BuildAssembly(ImmutableDictionary<Uri, string> sources, CompilerMetadata metadata, QSharpLogger logger, string dllName, bool compileAsExecutable, string executionTarget)
        {
            logger.LogDebug($"Compiling the following Q# files: {string.Join(",", sources.Keys.Select(f => f.LocalPath))}");

            // Ignore any @EntryPoint() attributes found in libraries.
            logger.ErrorCodesToIgnore.Add(QsCompiler.Diagnostics.ErrorCode.EntryPointInLibrary);
            var qsCompilation = this.UpdateCompilation(sources, metadata.QsMetadatas, logger, compileAsExecutable);
            logger.ErrorCodesToIgnore.Remove(QsCompiler.Diagnostics.ErrorCode.EntryPointInLibrary);

            if (logger.HasErrors) return null;

            try
            {
                // Generate C# simulation code from Q# syntax tree and convert it into C# syntax trees:
                var trees = new List<CodeAnalysis.SyntaxTree>();
                foreach (var file in sources.Keys)
                {
                    var sourceFile = CompilationUnitManager.GetFileId(file);
                    var codegenContext = string.IsNullOrEmpty(executionTarget)
                        ? CodegenContext.Create(qsCompilation.Namespaces)
                        : CodegenContext.Create(qsCompilation.Namespaces, new Dictionary<string, string>() { { AssemblyConstants.ExecutionTarget, executionTarget } });
                    var code = SimulationCode.generate(sourceFile, codegenContext);
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
