// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    internal class EntryPointGenerator : IEntryPointGenerator
    {
        private ICompilerService Compiler { get; }
        private ILogger<EntryPointGenerator> Logger { get; }
        private IWorkspace Workspace { get; }
        private ISnippets Snippets { get; }
        private IServiceProvider ServiceProvider { get; }
        public IReferences References { get; }
        public AssemblyInfo[] WorkspaceAssemblies { get; set; } = Array.Empty<AssemblyInfo>();
        public AssemblyInfo? SnippetsAssemblyInfo { get; set; }
        public AssemblyInfo? EntryPointAssemblyInfo { get; set; }

        public EntryPointGenerator(
            ICompilerService compiler,
            IWorkspace workspace,
            ISnippets snippets,
            IReferences references,
            ILogger<EntryPointGenerator> logger,
            IServiceProvider serviceProvider,
            IEventService eventService)
        {
            Compiler = compiler;
            Workspace = workspace;
            Snippets = snippets;
            References = references;
            Logger = logger;
            ServiceProvider = serviceProvider;

            AssemblyLoadContext.Default.Resolving += Resolve;

            eventService?.TriggerServiceInitialized<IEntryPointGenerator>(this);
        }

        /// <summary>
        /// Because the assemblies are loaded into memory, we need to provide this method to the AssemblyLoadContext
        /// such that the Workspace assembly or this assembly is correctly resolved when it is executed for simulation.
        /// </summary>
        public Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            if (name.Name == Path.GetFileNameWithoutExtension(EntryPointAssemblyInfo?.Location))
            {
                return EntryPointAssemblyInfo?.Assembly;
            }

            if (name.Name == Path.GetFileNameWithoutExtension(SnippetsAssemblyInfo?.Location))
            {
                return SnippetsAssemblyInfo?.Assembly;
            }

            foreach (var asm in WorkspaceAssemblies)
            {
                if (name.Name == Path.GetFileNameWithoutExtension(asm?.Location))
                {
                    return asm?.Assembly;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IEntryPoint> Generate(string operationName, string? executionTarget,
            TargetCapability? capability = null, bool generateQir = false, bool generateCSharp = true)
        {
            Logger?.LogDebug($"Generating entry point: operationName={operationName}, executionTarget={executionTarget}");

            var logger = new QSharpLogger(Logger);
            var compilerMetadata = References.CompilerMetadata;

            // Clear references to previously-built assemblies
            WorkspaceAssemblies = Array.Empty<AssemblyInfo>();
            SnippetsAssemblyInfo = null;
            EntryPointAssemblyInfo = null;

            // Compile the workspace against the provided execution target
            var workspaceFiles = Workspace.SourceFiles.ToArray();
            if (workspaceFiles.Any())
            {
                Logger?.LogDebug($"{workspaceFiles.Length} files found in workspace. Compiling.");

                var workspaceAssemblies = new List<AssemblyInfo>();
                foreach (var project in Workspace.Projects.Where(p => p.SourceFiles.Any()))
                {
                    try
                    {
                        var asm = await Compiler.BuildFiles(
                            project.SourceFiles.ToArray(),
                            compilerMetadata.WithAssemblies(workspaceAssemblies.ToArray()),
                            logger,
                            Path.Combine(Workspace.CacheFolder, $"__entrypoint{project.CacheDllName}"));
                        if (asm is not null)
                        {
                            workspaceAssemblies.Add(asm);
                        }
                        else
                        {
                            Logger.LogCritical("Got empty assembly when building entry point, but no compilation error was raised.");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(
                            "IQS004",
                            $"Error compiling project {project.ProjectFile} for execution target {executionTarget}: {e.Message}");
                    }
                }

                if (!workspaceAssemblies.Any() || logger.HasErrors)
                {
                    Logger?.LogError($"Error compiling workspace.");
                    throw new CompilationErrorsException(logger);
                }

                WorkspaceAssemblies = workspaceAssemblies.ToArray();
                compilerMetadata = compilerMetadata.WithAssemblies(WorkspaceAssemblies);
            }

            // Compile the snippets against the provided execution target
            var snippets = Snippets.Items.ToArray();
            if (snippets.Any())
            {
                Logger?.LogDebug($"{snippets.Length} items found in snippets. Compiling.");
                SnippetsAssemblyInfo = await Compiler.BuildSnippets(
                    snippets, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__snippets__.dll"));
                if (SnippetsAssemblyInfo is null || logger.HasErrors)
                {
                    Logger?.LogError($"Error compiling snippets.");
                    throw new CompilationErrorsException(logger);
                }

                compilerMetadata = compilerMetadata.WithAssemblies(SnippetsAssemblyInfo);
            }

            // Build the entry point assembly
            var operationInfo = new EntryPointOperationResolver(this).Resolve(operationName);
            if (operationInfo == null)
            {
                Logger?.LogError($"{operationName} is not a recognized Q# operation name.");
                throw new UnsupportedOperationException(operationName);
            }

            EntryPointAssemblyInfo = await Compiler.BuildEntryPoint(
                operationInfo, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__.dll"), executionTarget, capability,
                generateQir: generateQir,
                generateCSharp: generateCSharp);
            if (EntryPointAssemblyInfo is null || logger.HasErrors)
            {
                Logger?.LogError($"Error compiling entry point for operation {operationName}.");
                throw new CompilationErrorsException(logger);
            }

            if (EntryPointAssemblyInfo.Operations.Count() <= 1)
            {
                // Entry point assembly contained zero or one operations; this
                // may indicate that C# code is not being correctly
                // regenerated. At least two operations (the entry point and
                // the operation called from the entry point) are expected.
                Logger?.LogWarning(
                    "Internal error compiling entry point for operation {OperationName}; entry point assembly did not contain the right number of operations. This should never happen, and most likely indicates a bug in IQ#. ",
                    operationName
                );
            }

            var entryPointOperations = EntryPointAssemblyInfo
                .Operations
                .Where(op => op.Header.Attributes.Any(
                    attr =>
                    {
                        var qName = attr.TypeId.ValueOr(null);
                        return qName != null &&
                               qName.Name == "EntryPoint" &&
                               qName.Namespace == "Microsoft.Quantum.Core";
                    }
                ));
            var entryPointOperationInfo = entryPointOperations
                .SingleOrDefault();

            if (entryPointOperationInfo == null)
            {
                throw new Exception($"Entry point assembly contained {entryPointOperations.Count()}, but expected 1.");
            }

            // Construct the EntryPointInfo<,> object
            var parameterTypes = entryPointOperationInfo.RoslynParameters.Select(p => p.ParameterType).ToArray();
            var typeCount = parameterTypes.Length;
            Type entryPointInputType = typeCount switch
            {
                0 => typeof(QVoid),
                1 => parameterTypes.Single(),
                _ => PartialMapper.TupleTypes[typeCount].MakeGenericType(parameterTypes)
            };
            var entryPointOutputType = entryPointOperationInfo.ReturnType;

            Type entryPointInfoType = typeof(EntryPointInfo<,>).MakeGenericType(new Type[] { entryPointInputType, entryPointOutputType });
            var entryPointInfo = entryPointInfoType.GetConstructor(new Type[] { typeof(Type) })
                .Invoke(new object[] { entryPointOperationInfo.RoslynType });

            return new EntryPoint(
                entryPointInfo, entryPointInputType, entryPointOutputType, entryPointOperationInfo,
                logger: ServiceProvider.GetService<ILogger<EntryPoint>>(), EntryPointAssemblyInfo.QirBitcode
            );
        }
    }
}
