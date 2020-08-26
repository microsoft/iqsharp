﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
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
        /// <summary>
        ///     Settings for controlling how the compiler service creates
        ///     assemblies from snippets.
        /// </summary>
        public class Settings
        {
            /// <summary>
            ///     A list of namespaces to be automatically opened in snippets,
            ///     separated by <c>,</c>. If <c>"$null"</c>, then no namespaces
            ///     are opened. Aliases can be provided by using <c>=</c>.
            /// </summary>
            public string? AutoOpenNamespaces { get; set; }
        }

        /// <inheritdoc/>
        public IDictionary<string, string?> AutoOpenNamespaces { get; set; } = new Dictionary<string, string?>
        {
            ["Microsoft.Quantum.Intrinsic"] = null,
            ["Microsoft.Quantum.Canon"] = null
        };

        public CompilerService(ILogger<CompilerService>? logger, IOptions<Settings>? options)
        {
            if (options?.Value?.AutoOpenNamespaces is string namespaces)
            {
                logger?.LogInformation(
                    "Auto-open namespaces overridden by startup options: \"{0}\"",
                    namespaces
                );
                AutoOpenNamespaces =
                    namespaces.Trim() == "$null"
                    ? new Dictionary<string, string?>()
                    : namespaces
                      .Split(",")
                      .Select(ns => ns.Split("=", 2).Select(part => part.Trim()).ToArray())
                      .ToDictionary(
                          nsParts => nsParts[0],
                          nsParts => nsParts.Length > 1 ? nsParts[1] : null
                      );
            }
        }

        private CompilationLoader CreateTemporaryLoader(string source)
        {
            var uri = new Uri(Path.GetFullPath("__CODE_SNIPPET__.qs"));
            var sources = new Dictionary<Uri, string>() { { uri, $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ {source} }}" } }.ToImmutableDictionary();
            var loadOptions = new CompilationLoader.Configuration();
            return new CompilationLoader(_ => sources, _ => QsReferences.Empty, loadOptions);
        }

        /// <inheritdoc/>
        public IEnumerable<QsNamespaceElement> IdentifyElements(string source)
        {
            var loader = CreateTemporaryLoader(source);
            if (loader.VerifiedCompilation == null) { return ImmutableArray<QsNamespaceElement>.Empty; }
            var ns = NonNullable<string>.New(Snippets.SNIPPETS_NAMESPACE);
            return loader.VerifiedCompilation.SyntaxTree.TryGetValue(ns, out var tree)
                   ? tree.Elements
                   : ImmutableArray<QsNamespaceElement>.Empty;
        }

        /// <inheritdoc/>
        public IDictionary<string, string?> IdentifyOpenedNamespaces(string source)
        {
            var loader = CreateTemporaryLoader(source);
            if (loader.VerifiedCompilation == null) { return ImmutableDictionary<string, string?>.Empty; }
            return loader.VerifiedCompilation.Tokenization.Values
                .SelectMany(tokens => tokens.SelectMany(fragments => fragments))
                .Where(fragment => fragment.Kind != null && fragment.Kind.IsOpenDirective)
                .Select(fragment => ((QsFragmentKind.OpenDirective)fragment.Kind))
                .Where(openDirective => !string.IsNullOrEmpty(openDirective.Item1.Symbol?.AsDeclarationName(null)))
                .ToDictionary(
                    openDirective => openDirective.Item1.Symbol.AsDeclarationName(null),
                    openDirective => openDirective.Item2.ValueOr(null)?.Symbol?.AsDeclarationName(null));
        }

        /// <summary> 
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// Removes all currently tracked source files in the CompilationManager and replaces them with the given ones.
        /// The compiler does this on a best effort basis, so it will return the elements even if the compilation fails.
        /// If the given references are not null, reloads the references loaded in by the CompilationManager
        /// if the keys of the given references differ from the currently loaded ones.
        /// Returns an enumerable of all namespaces, including the content from both source files and references.
        /// </summary> 
        private QsCompilation UpdateCompilation(
            ImmutableDictionary<Uri, string> sources,
            QsReferences? references = null,
            QSharpLogger? logger = null,
            bool compileAsExecutable = false,
            string? executionTarget = null,
            AssemblyConstants.RuntimeCapabilities runtimeCapabilities = AssemblyConstants.RuntimeCapabilities.Unknown)
        {
            var loadOptions = new CompilationLoader.Configuration
            {
                GenerateFunctorSupport = true,
                IsExecutable = compileAsExecutable,
                AssemblyConstants = new Dictionary<string, string> { [AssemblyConstants.ProcessorArchitecture] = executionTarget ?? string.Empty },
                RuntimeCapabilities = runtimeCapabilities
            };
            var loaded = new CompilationLoader(_ => sources, _ => references, loadOptions, logger);
            return loaded.CompilationOutput;
        }

        /// <inheritdoc/>
        public AssemblyInfo? BuildEntryPoint(OperationInfo operation, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string? executionTarget = null,
            AssemblyConstants.RuntimeCapabilities runtimeCapabilities = AssemblyConstants.RuntimeCapabilities.Unknown)
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
            return BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: true, executionTarget, runtimeCapabilities);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// Each snippet code is wrapped inside the 'SNIPPETS_NAMESPACE' namespace and processed as a file
        /// with the same name as the snippet id.
        /// </summary>
        public AssemblyInfo? BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string? executionTarget = null,
            AssemblyConstants.RuntimeCapabilities runtimeCapabilities = AssemblyConstants.RuntimeCapabilities.Unknown)
        {
            string openStatements = string.Join("", AutoOpenNamespaces.Select(
                entry => string.IsNullOrEmpty(entry.Value) 
                    ? $"open {entry.Key};"
                    : $"open {entry.Key} as {entry.Value};"
                ));
            string WrapInNamespace(Snippet s) =>
                $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ {openStatements}\n{s.code}\n}}";

            var sources = snippets.ToImmutableDictionary(s => s.Uri, WrapInNamespace);

            // Ignore some warnings about already-open namespaces and aliases when compiling snippets.
            var errorCodesToIgnore = new List<QsCompiler.Diagnostics.ErrorCode>()
            {
                QsCompiler.Diagnostics.ErrorCode.TypeRedefinition,
                QsCompiler.Diagnostics.ErrorCode.TypeConstructorOverlapWithCallable,
            };

            errorCodesToIgnore.ForEach(code => logger.ErrorCodesToIgnore.Add(code));
            var assembly = BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: false, executionTarget, runtimeCapabilities);
            errorCodesToIgnore.ForEach(code => logger.ErrorCodesToIgnore.Remove(code));

            return assembly;
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        public AssemblyInfo? BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string? executionTarget = null,
            AssemblyConstants.RuntimeCapabilities runtimeCapabilities = AssemblyConstants.RuntimeCapabilities.Unknown)
        {
            var sources = ProjectManager.LoadSourceFiles(files, d => logger?.Log(d), ex => logger?.Log(ex));
            return BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: false, executionTarget, runtimeCapabilities);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the Q# syntax tree.
        /// </summary>
        private AssemblyInfo? BuildAssembly(ImmutableDictionary<Uri, string> sources, CompilerMetadata metadata, QSharpLogger logger, string dllName, bool compileAsExecutable, string? executionTarget,
            AssemblyConstants.RuntimeCapabilities runtimeCapabilities)
        {
            logger.LogDebug($"Compiling the following Q# files: {string.Join(",", sources.Keys.Select(f => f.LocalPath))}");

            // Ignore any @EntryPoint() attributes found in libraries.
            logger.ErrorCodesToIgnore.Add(QsCompiler.Diagnostics.ErrorCode.EntryPointInLibrary);
            var qsCompilation = this.UpdateCompilation(sources, metadata.QsMetadatas, logger, compileAsExecutable, executionTarget, runtimeCapabilities);
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
