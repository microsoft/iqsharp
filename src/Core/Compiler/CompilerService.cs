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
        /// <summary>
        /// Compiles the given Q# code and returns the list of elements found in it.
        /// The compiler does this on a best effort, so it will return the elements even if the compilation fails.
        /// </summary>
        public IEnumerable<QsQualifiedName> IdentifyElements(string source)
        {
            var nsName = NonNullable<string>.New(Snippets.SNIPPETS_NAMESPACE);
            var content = $"namespace {nsName.Value} {{ {source} }}";
            var fileManager = CompilationUnitManager.InitializeFileManager(Snippets.SNIPPET_FILE_URI, content);
            var definedCallables = fileManager.GetCallableDeclarations();
            var definedTypes = fileManager.GetTypeDeclarations();
            return definedCallables.Concat(definedTypes).Select(decl => new QsQualifiedName(nsName, decl.Item1));
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
        private static AssemblyInfo BuildAssembly(ImmutableDictionary<Uri, string> sources, CompilerMetadata metadata, QSharpLogger logger, string dllName)
        {
            logger.LogDebug($"Compiling the following Q# files: {string.Join(",", sources.Keys.Select(f => f.LocalPath))}");

            var outFolder = Path.GetDirectoryName(dllName);
            var outFile = Path.Combine(outFolder, Path.GetFileNameWithoutExtension(dllName) + ".bson");
            var loadOptions = new QsCompiler.CompilationLoader.Configuration
            {
                GenerateFunctorSupport = true,
                BuildOutputFolder = ".",
                ProjectName = outFile
            };
            var loaded = new QsCompiler.CompilationLoader(_ => sources, _ => metadata.QsMetadatas, loadOptions, logger);
            var syntaxTree = loaded.GeneratedSyntaxTree?.ToArray();

            if (logger.HasErrors) return null;

            try
            {
                // Generate C# simulation code from Q# syntax tree and convert it into C# syntax trees:
                var trees = new List<SyntaxTree>();
                NonNullable<string> GetFileId(Uri uri) => CompilationUnitManager.TryGetFileId(uri, out var id) ? id : NonNullable<string>.New(uri.AbsolutePath);
                foreach (var file in sources.Keys)
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
                    Path.GetFileNameWithoutExtension(dllName),
                    trees,
                    metadata.RoslynMetadatas,
                    options);

                // Generate the assembly from the C# compilation:
                using (var ms = new MemoryStream())
                {
                    var syntaxTreeFile = Path.Combine(Path.GetDirectoryName(dllName), Path.GetFileNameWithoutExtension(dllName) + ".bson");
                    var resourceDescription = new ResourceDescription
                    (
                        resourceName: QsCompiler.ReservedKeywords.DotnetCoreDll.ResourceName,
                        dataProvider: () => File.OpenRead(syntaxTreeFile),
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

                        return new AssemblyInfo(Assembly.Load(data), dllName, syntaxTree);
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
