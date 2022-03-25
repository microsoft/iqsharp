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
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Microsoft.Quantum.QsCompiler.Transformations.SyntaxTreeTrimming;
using Microsoft.Quantum.QsCompiler.Transformations.Targeting;
using QsReferences = Microsoft.Quantum.QsCompiler.CompilationBuilder.References;
using System.Threading.Tasks;

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


            /// <summary>
            ///     If <c>true</c>, loads and caches compiler dependencies (e.g.: Roslyn
            ///     and Q# code generation) on startup.
            ///     This has a significant performance advantage, especially
            ///     when a kernel is started in the background, but can cause
            ///     more RAM to be used.
            /// </summary>
            public bool CacheCompilerDependencies { get; set; } = false;
        }

        /// <inheritdoc/>
        public IDictionary<string, string?> AutoOpenNamespaces { get; set; } = new Dictionary<string, string?>
        {
            ["Microsoft.Quantum.Intrinsic"] = null,
            ["Microsoft.Quantum.Canon"] = null
        };

        private readonly Task DependenciesInitialized;
        private readonly ILogger? Logger;

        // Note to future IQ# developers: This service should start ★fast★.
        // Please be judicious when adding parameters to this constructor, and
        // defer those dependencies to tasks if at all possible.
        public CompilerService(
            ILogger<CompilerService>? logger,
            IOptions<Settings>? options,
            IEventService? eventService,
            IServiceProvider serviceProvider
        )
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Logger = logger;
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

            eventService?.TriggerServiceInitialized<ICompilerService>(this);
            if (options?.Value.CacheCompilerDependencies ?? false)
            {
                DependenciesInitialized = InitializeDependencies(serviceProvider.GetRequiredServiceInBackground<IReferences>(logger));
                Task.Run(async () =>
                {
                    await DependenciesInitialized;
                    stopwatch.Stop();
                    logger?.LogInformation(
                        "Initialized dependencies for compiler service {Elapsed} after service start.",
                        stopwatch.Elapsed
                    );
                });
            }
            else
            {
                DependenciesInitialized = Task.CompletedTask;
            }
        }

        // We take references as a task so that it can load in the background
        // without placing a hard dependency at the level of a constructor.
        // That in turn allows this service to begin initializing sooner during
        // kernel startup.
        private async Task InitializeDependencies(Task<IReferences> referencesTask) => await
            // Force types that we'll depend on later to initialize by
            // calling trivial methods now.
            Task.WhenAll(
                Task.Run(
                    () =>
                    {
                        CreateTemporaryLoader("");
                    }
                ),
                Task.Run(
                    async () =>
                    {
                        // See https://github.com/dotnet/roslyn/issues/46340
                        // for why this works. We need to compile something
                        // that actually depends on something in System so
                        // that the assembly references we're trying to cache
                        // don't get optimized away.
                        var compilation = CSharpCompilation.Create(
                            Path.GetRandomFileName(),
                            syntaxTrees: new List<CodeAnalysis.SyntaxTree>
                            {
                                CSharpSyntaxTree.ParseText(@"
                                    using System;
                                    namespace Placeholder
                                    {
                                        public class Placeholder
                                        {
                                            public void DoSomething()
                                            {
                                                Console.WriteLine(""hi!"");
                                            }
                                        }
                                    }
                                ")
                            },
                            references: (await referencesTask).CompilerMetadata.RoslynMetadatas,
                            options: new CSharpCompilationOptions(
                                OutputKind.DynamicallyLinkedLibrary,
                                optimizationLevel: OptimizationLevel.Release,
                                allowUnsafe: true
                            )
                        );
                        try
                        {
                            using var ms = new MemoryStream();
                            var result = compilation.Emit(ms);
                            if (!result.Success)
                            {
                                var failures = string.Join("\n",
                                    result.Diagnostics
                                    .Select(diag => diag.ToString())
                                );
                                Logger?.LogWarning(
                                    "C# compilation failure or warnings while initializing Roslyn dependencies:\n{Failures}",
                                    failures
                                );
                            }
                            else
                            {
                                // Do nothing — the compilation worked, so we
                                // should have initialized things correctly.
                            }
                        }
                        catch (Exception ex)
                        {
                            // Exceptions here are noncritical, but could have
                            // perf impact, so log them accordingly.
                            Logger?.LogWarning(ex, "Exception encountered while initializing Roslyn dependencies.");
                        }
                    }
                ),
                Task.Run(
                    () =>
                    {
                        var codegenContext = CodegenContext.Create(ImmutableArray<QsNamespace>.Empty);
                        SimulationCode.generate("foo", codegenContext);
                    }
                ),
                Task.Run(
                    () =>
                    {
                        try
                        {
                            var syntaxTree = new QsCompilation(
                                ImmutableArray<QsNamespace>.Empty,
                                ImmutableArray<QsQualifiedName>.Empty
                            );
                            using var serializedCompilation = new MemoryStream();
                            CompilationLoader.WriteBinary(syntaxTree, serializedCompilation);
                        }
                        // Ignore errors, since we don't care about the result;
                        // we just want to force static constructors to load.
                        catch {}
                    }
                )
            );

        private CompilationLoader CreateTemporaryLoader(string source, ITaskReporter? perfTask = null)
        {
            var uri = new Uri(Path.GetFullPath("__CODE_SNIPPET__.qs"));
            var sources = new Dictionary<Uri, string>() { { uri, $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ {source} }}" } }.ToImmutableDictionary();
            var loadOptions = new CompilationLoader.Configuration();
            perfTask?.ReportStatus("Ready to create compilation loader.", "ready-loader");
            return new CompilationLoader(_ => sources, _ => QsReferences.Empty, loadOptions);
        }

        /// <inheritdoc/>
        public IEnumerable<QsNamespaceElement> IdentifyElements(string source, ITaskReporter? parent = null)
        {
            using var perfTask = parent?.BeginSubtask("Identifying namespace elements", "identify-elements");
            var loader = CreateTemporaryLoader(source, perfTask);
            perfTask?.ReportStatus("Created loader.", "created-loader");
            if (loader.VerifiedCompilation == null) { return ImmutableArray<QsNamespaceElement>.Empty; }
            return loader.VerifiedCompilation.SyntaxTree.TryGetValue(Snippets.SNIPPETS_NAMESPACE, out var tree)
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
                .Select(fragment => ((QsFragmentKind.OpenDirective)fragment.Kind!))
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
        private QsCompilation? UpdateCompilation(
            ImmutableDictionary<Uri, string> sources,
            QsReferences references,
            QSharpLogger? logger = null,
            bool compileAsExecutable = false,
            string? executionTarget = null,
            RuntimeCapability? runtimeCapability = null,
            ITaskReporter? parent = null
        )
        {
            using var perfTask = parent?.BeginSubtask("Updating compilation", "update-compilation");
            var loadOptions = new CompilationLoader.Configuration
            {
                GenerateFunctorSupport = true,
                LoadReferencesBasedOnGeneratedCsharp = false, // deserialization of resources in references is only needed if there is an execution target
                IsExecutable = compileAsExecutable,
                AssemblyConstants = new Dictionary<string, string> { [AssemblyConstants.ProcessorArchitecture] = executionTarget ?? string.Empty },
                RuntimeCapability = runtimeCapability ?? RuntimeCapability.FullComputation
            };
            var loaded = new CompilationLoader(_ => sources, _ => references, loadOptions, logger);
            return loaded.CompilationOutput;
        }

        /// <inheritdoc/>
        public async Task<AssemblyInfo?> BuildEntryPoint(OperationInfo operation, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string? executionTarget = null,
            RuntimeCapability? runtimeCapability = null)
        {
            var signature = operation.Header.PrintSignature();
            var argumentTuple = SyntaxTreeToQsharp.ArgumentTuple(operation.Header.ArgumentTuple, type => type.ToString(), symbolsOnly: true);

            var entryPointUri = new Uri(Path.GetFullPath(Path.Combine("/", $"entrypoint.qs")));
            var entryPointSnippet = @$"namespace ENTRYPOINT
                {{
                    open {operation.Header.QualifiedName.Namespace};
                    @{BuiltIn.EntryPoint.FullName}()
                    operation {signature}
                    {{
                        return {operation.Header.QualifiedName}{argumentTuple};
                    }}
                }}";

            var sources = new Dictionary<Uri, string>() {{ entryPointUri, entryPointSnippet }}.ToImmutableDictionary();
            return await BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: true, executionTarget, runtimeCapability, regenerateAll: true);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// Each snippet code is wrapped inside the 'SNIPPETS_NAMESPACE' namespace and processed as a file
        /// with the same name as the snippet id.
        /// </summary>
        public async Task<AssemblyInfo?> BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string? executionTarget = null,
            RuntimeCapability? runtimeCapability = null, ITaskReporter? parent = null)
        {
            using var perfTask = parent?.BeginSubtask("Building snippets.", "build-snippets");
            string openStatements = string.Join("", AutoOpenNamespaces.Select(
                entry => string.IsNullOrEmpty(entry.Value) 
                    ? $"open {entry.Key};"
                    : $"open {entry.Key} as {entry.Value};"
                ));
            string WrapInNamespace(Snippet s) =>
                $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ {openStatements}\n{s.code}\n}}";

            var sources = snippets.ToImmutableDictionary(s => s.Uri, WrapInNamespace);

            // Ignore some warnings about already-open namespaces and aliases when compiling snippets.
            var warningCodesToIgnore = new List<QsCompiler.Diagnostics.WarningCode>()
            {
                QsCompiler.Diagnostics.WarningCode.NamespaceAleadyOpen,
                QsCompiler.Diagnostics.WarningCode.NamespaceAliasIsAlreadyDefined,
            };

            warningCodesToIgnore.ForEach(code => logger.WarningCodesToIgnore.Add(code));
            perfTask?.ReportStatus("About to build assembly.", "build-assembly");
            var assembly = await BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: false, 
            executionTarget, runtimeCapability, parent: perfTask);
            perfTask?.ReportStatus("Built assembly.", "built-assembly");
            warningCodesToIgnore.ForEach(code => logger.WarningCodesToIgnore.Remove(code));

            return assembly;
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        public async Task<AssemblyInfo?> BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName, string? executionTarget = null,
            RuntimeCapability? runtimeCapability = null)
        {
            var sources = ProjectManager.LoadSourceFiles(files, d => logger?.Log(d), ex => logger?.Log(ex));
            return await BuildAssembly(sources, metadatas, logger, dllName, compileAsExecutable: false, executionTarget, runtimeCapability);
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the Q# syntax tree.
        /// </summary>
        private async Task<AssemblyInfo?> BuildAssembly(ImmutableDictionary<Uri, string> sources, CompilerMetadata metadata, QSharpLogger logger, string dllName, bool compileAsExecutable, string? executionTarget,
            RuntimeCapability? runtimeCapability = null, bool regenerateAll = false, ITaskReporter? parent = null)
        {
            using var perfTask = parent?.BeginSubtask("Building assembly.", "build-assembly");
            logger.LogDebug($"Compiling the following Q# files: {string.Join(",", sources.Keys.Select(f => f.LocalPath))}");

            // Ignore any @EntryPoint() attributes found in libraries.
            logger.WarningCodesToIgnore.Add(QsCompiler.Diagnostics.WarningCode.EntryPointInLibrary);
            var qsCompilation = this.UpdateCompilation(sources, metadata.QsMetadatas, logger, compileAsExecutable, executionTarget, runtimeCapability, parent: perfTask);
            logger.WarningCodesToIgnore.Remove(QsCompiler.Diagnostics.WarningCode.EntryPointInLibrary);

            if (logger.HasErrors || qsCompilation == null) return null;

            try
            {
                using var codeGenTask = perfTask?.BeginSubtask("Code generation", "code-generation");

                var fromSources = regenerateAll
                                ? qsCompilation.Namespaces
                                : qsCompilation.Namespaces.Select(ns => FilterBySourceFile.Apply(ns, s => s.EndsWith(".qs")));

                // Async, get manifest resources by writing syntax trees to memory.
                var manifestResources = Task.Run(() =>
                {
                    using var qstTask = codeGenTask?.BeginSubtask("Serializing Q# syntax tree.", "serialize-qs-ast");
                    var syntaxTree = new QsCompilation(fromSources.ToImmutableArray(), qsCompilation.EntryPoints);

                    using var serializedCompilation = new MemoryStream();
                    if (!CompilationLoader.WriteBinary(syntaxTree, serializedCompilation))
                    {
                        logger.LogError("IQS005", "Failed to write compilation to binary stream.");
                        return null;
                    }

                    return new List<ResourceDescription>()
                    {
                        new ResourceDescription(
                            resourceName: DotnetCoreDll.SyntaxTreeResourceName,
                            dataProvider: () => new MemoryStream(serializedCompilation.ToArray()),
                            isPublic: true
                        )
                    };
                });

                // In the meanwhile...
                // Generate C# simulation code from Q# syntax tree and convert it into C# syntax trees:
                var allSources = regenerateAll
                    ? GetSourceFiles.Apply(qsCompilation.Namespaces)
                    : sources.Keys.Select(
                        file => CompilationUnitManager.GetFileId(file)
                      );

                CodeAnalysis.SyntaxTree createTree(string file)
                {
                    codeGenTask?.ReportStatus($"Creating codegen context for file {file}", "generate-one-file");
                    var codegenContext = string.IsNullOrEmpty(executionTarget)
                        ? CodegenContext.Create(qsCompilation.Namespaces)
                        : CodegenContext.Create(qsCompilation.Namespaces, new Dictionary<string, string>() { { AssemblyConstants.ExecutionTarget, executionTarget } });
                    codeGenTask?.ReportStatus($"Generating C# for file {file}", "generate-one-file");
                    var code = SimulationCode.generate(file, codegenContext);
                    var filename = Path.Combine(".", "obj", new FileInfo(file).Name + ".cs");
                    File.WriteAllText(filename, code);
                    codeGenTask?.ReportStatus($"Parsing generated C# for file {file}", "generate-one-file");
                    logger.LogDebug($"Generated the following C# code for {file}:\n=============\n{code}\n=============\n");
                    return CSharpSyntaxTree.ParseText(code, encoding: UTF8Encoding.UTF8);
                }

                var trees = allSources.Select(createTree).ToImmutableList();
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);
                var parsedTrees = trees;
                using var csCompileTask = codeGenTask?.BeginSubtask("Compiling generated C#.", "compile-csharp");

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(dllName),
                    parsedTrees,
                    metadata.RoslynMetadatas,
                    options);

                // Generate the assembly from the C# compilation and the manifestResources once we have
                // both.
                using var ms = new MemoryStream();
                var result = compilation.Emit(ms, manifestResources: await manifestResources);

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
            catch (Exception e)
            {
                // Log to the IQ# logger...
                Logger?.LogError(e, $"Unexpected error compiling assembly.");
                // ...and to the Q# compiler log.
                logger.LogError("IQS002", $"Unexpected error compiling assembly: {e.Message}.");
                return null;
            }
        }
    }
}
