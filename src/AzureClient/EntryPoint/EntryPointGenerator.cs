// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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
            IEventService eventService)
        {
            Compiler = compiler;
            Workspace = workspace;
            Snippets = snippets;
            References = references;
            Logger = logger;

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

        public IEntryPoint Generate(string operationName, string? executionTarget,
            RuntimeCapability? runtimeCapability = null)
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
                        workspaceAssemblies.Add(Compiler.BuildFiles(
                            project.SourceFiles.ToArray(),
                            compilerMetadata.WithAssemblies(workspaceAssemblies.ToArray()),
                            logger,
                            Path.Combine(Workspace.CacheFolder, $"__entrypoint{project.CacheDllName}"),
                            executionTarget,
                            runtimeCapability));
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
                    throw new CompilationErrorsException(logger.Errors.ToArray());
                }

                WorkspaceAssemblies = workspaceAssemblies.ToArray();
                compilerMetadata = compilerMetadata.WithAssemblies(WorkspaceAssemblies);
            }

            // Compile the snippets against the provided execution target
            var snippets = Snippets.Items.ToArray();
            if (snippets.Any())
            {
                Logger?.LogDebug($"{snippets.Length} items found in snippets. Compiling.");
                SnippetsAssemblyInfo = Compiler.BuildSnippets(
                    snippets, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__snippets__.dll"));
                if (SnippetsAssemblyInfo == null || logger.HasErrors)
                {
                    Logger?.LogError($"Error compiling snippets.");
                    throw new CompilationErrorsException(logger.Errors.ToArray());
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

            EntryPointAssemblyInfo = Compiler.BuildEntryPoint(
                operationInfo, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__.dll"), executionTarget, runtimeCapability);
            if (EntryPointAssemblyInfo == null || logger.HasErrors)
            {
                Logger?.LogError($"Error compiling entry point for operation {operationName}.");
                throw new CompilationErrorsException(logger.Errors.ToArray());
            }

            if (EntryPointAssemblyInfo.Operations.Count() <= 1)
            {
                Logger?.LogWarning("Entry point assembly contained zero or one operations; this may indicate that C# code is not being correctly regenerated. At least two operations (the entry point and the operation called from the entry point) are expected.");
            }

            var entryPointOperationInfo = EntryPointAssemblyInfo
                .Operations
                .Where(op => op.Header.QualifiedName.Namespace == "ENTRYPOINT" && op.Header.QualifiedName.Name == operationName)
                .SingleOrDefault();

            if (entryPointOperationInfo == null)
            {
                throw new Exception("Entry point assembly either contained multiple entry point operations, or contained zero.");
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
            Type entryPointOutputType = entryPointOperationInfo.ReturnType;

            Type entryPointInfoType = typeof(EntryPointInfo<,>).MakeGenericType(new Type[] { entryPointInputType, entryPointOutputType });
            var entryPointInfo = entryPointInfoType.GetConstructor(new Type[] { typeof(Type) })
                .Invoke(new object[] { entryPointOperationInfo.RoslynType });

            return new EntryPoint(entryPointInfo, entryPointInputType, entryPointOutputType, entryPointOperationInfo);
        }
    }
}
