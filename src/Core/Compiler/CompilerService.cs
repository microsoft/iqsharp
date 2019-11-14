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
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.CsharpGeneration;
using Microsoft.Quantum.QsCompiler.DataTypes;


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
        /// Builds the corresponding .net core assembly from the code in the given Q# Snippets.
        /// Each snippet code is wrapped inside the 'SNIPPETS_NAMESPACE' namespace and processed as a file
        /// with the same name as the snippet id.
        /// </summary>
        public AssemblyInfo BuildSnippets(Snippet[] snippets, CompilerMetadata metadatas, QSharpLogger logger, string dllName)
        {
            string WrapInNamespace(Snippet s) => $"namespace {Snippets.SNIPPETS_NAMESPACE} {{ open Microsoft.Quantum.Intrinsic; open Microsoft.Quantum.Canon; {s.code} }}";

            var sources = snippets.ToDictionary(s => s.Uri, WrapInNamespace);
            var syntaxTree = BuildQsSyntaxTree(sources.ToImmutableDictionary(), metadatas.QsMetadatas, logger, dllName);
            var assembly = BuildAssembly(sources.Keys.ToArray(), syntaxTree, metadatas.RoslynMetadatas, logger, dllName);

            return assembly;
        }

        /// <summary>
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails.
        /// </summary>
        public IEnumerable<QsCompiler.SyntaxTree.QsNamespaceElement> IdentifyElements(string source)
        {
            var ns = NonNullable<string>.New(Snippets.SNIPPETS_NAMESPACE);
            var logger = new QSharpLogger(null);

            var sources = new Dictionary<Uri, string>() { { new Uri($"file:///temp"), $"namespace {ns.Value} {{ {source} }}" } }.ToImmutableDictionary();
            var references = QsReferences.Empty;

            var loadOptions = new QsCompiler.CompilationLoader.Configuration(); // do not generate functor support
            var loaded = new QsCompiler.CompilationLoader(_ => sources, _ => references, loadOptions, logger);
            return loaded.VerifiedCompilation?.SyntaxTree[ns].Elements;
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the code in the given files.
        /// </summary>
        public AssemblyInfo BuildFiles(string[] files, CompilerMetadata metadatas, QSharpLogger logger, string dllName)
        {
            var syntaxTree = BuildQsSyntaxTree(files, metadatas.QsMetadatas, logger, dllName);
            Uri FileUri(string f) => CompilationUnitManager.TryGetUri(NonNullable<string>.New(f), out var uri) ? uri : null;
            var assembly = BuildAssembly(files.Select(FileUri).ToArray(), syntaxTree, metadatas.RoslynMetadatas, logger, dllName);

            return assembly;
        }

        /// <summary>
        /// Builds the Q# syntax tree from the given files.
        /// The files are given as a list of filenames, as per the format expected by
        /// the <see cref="Microsoft.Quantum.QsCompiler.CompilationBuilder.ProjectManager.LoadSourceFiles(IEnumerable{string}, Action{VisualStudio.LanguageServer.Protocol.Diagnostic}, Action{Exception})" />
        /// method.
        /// </summary>
        private static QsCompiler.SyntaxTree.QsNamespace[] BuildQsSyntaxTree(string[] files, QsReferences references, QSharpLogger logger, string dllName)
        {
            var sources = ProjectManager.LoadSourceFiles(files, d => logger?.Log(d), ex => logger?.Log(ex));
            return BuildQsSyntaxTree(sources, references, logger, dllName);
        }

        /// <summary>
        /// Builds the Q# syntax tree from the given files/source paris.
        /// </summary>
        private static QsCompiler.SyntaxTree.QsNamespace[] BuildQsSyntaxTree(ImmutableDictionary<Uri, string> sources, QsReferences references, QSharpLogger logger, string dllName)
        {
            var outFolder = Path.GetDirectoryName(dllName);
            var outFile = Path.Combine(outFolder, Path.GetFileNameWithoutExtension(dllName) + ".bson");
            var loadOptions = new QsCompiler.CompilationLoader.Configuration
            {
                GenerateFunctorSupport = true,
                BuildOutputFolder = ".",
                ProjectName = outFile
            };
            var loaded = new QsCompiler.CompilationLoader(_ => sources, _ => references, loadOptions, logger);
            return loaded.GeneratedSyntaxTree?.ToArray();
        }

        /// <summary>
        /// Builds the corresponding .net core assembly from the Q# syntax tree.
        /// </summary>
        private static AssemblyInfo BuildAssembly(Uri[] fileNames, QsCompiler.SyntaxTree.QsNamespace[] syntaxTree, IEnumerable<MetadataReference> references, QSharpLogger logger, string targetDll)
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
                    var syntaxTreeFile = Path.Combine(Path.GetDirectoryName(targetDll), Path.GetFileNameWithoutExtension(targetDll) + ".bson");
                    var resourceDescription = new ResourceDescription
                    (
                        resourceName: QsCompiler.ReservedKeywords.DotnetCoreDll.ResourceName,
                        dataProvider: () => File.OpenRead(syntaxTreeFile),
                        isPublic: true
                    );


                    EmitResult result = compilation.Emit(ms, manifestResources: new[] { resourceDescription });

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
