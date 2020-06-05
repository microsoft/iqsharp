// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Common;
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
        public AssemblyInfo WorkspaceAssemblyInfo { get; set; } = new AssemblyInfo(null);
        public AssemblyInfo SnippetsAssemblyInfo { get; set; } = new AssemblyInfo(null);
        public AssemblyInfo EntryPointAssemblyInfo { get; set; } = new AssemblyInfo(null);

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
        public Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            if (name.Name == Path.GetFileNameWithoutExtension(EntryPointAssemblyInfo?.Location))
            {
                return EntryPointAssemblyInfo.Assembly;
            }
            if (name.Name == Path.GetFileNameWithoutExtension(SnippetsAssemblyInfo?.Location))
            {
                return SnippetsAssemblyInfo.Assembly;
            }
            else if (name.Name == Path.GetFileNameWithoutExtension(WorkspaceAssemblyInfo?.Location))
            {
                return WorkspaceAssemblyInfo.Assembly;
            }

            return null;
        }

        public IEntryPoint Generate(string operationName, string? executionTarget)
        {
            Logger?.LogDebug($"Generating entry point: operationName={operationName}, executionTarget={executionTarget}");

            var logger = new QSharpLogger(Logger);
            var compilerMetadata = References.CompilerMetadata;

            // Clear references to previously-built assemblies
            WorkspaceAssemblyInfo = null;
            SnippetsAssemblyInfo = null;
            EntryPointAssemblyInfo = null;

            // Compile the workspace against the provided execution target
            var workspaceFiles = Workspace.SourceFiles.ToArray();
            if (workspaceFiles.Any())
            {
                Logger?.LogDebug($"{workspaceFiles.Length} files found in workspace. Compiling.");
                WorkspaceAssemblyInfo = Compiler.BuildFiles(
                    workspaceFiles, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__workspace__.dll"), executionTarget);
                if (WorkspaceAssemblyInfo == null || logger.HasErrors)
                {
                    Logger?.LogError($"Error compiling workspace.");
                    throw new CompilationErrorsException(logger.Errors.ToArray());
                }

                compilerMetadata = compilerMetadata.WithAssemblies(WorkspaceAssemblyInfo);
            }

            // Compile the snippets against the provided execution target
            var snippets = Snippets.Items.ToArray();
            if (snippets.Any())
            {
                Logger?.LogDebug($"{snippets.Length} items found in snippets. Compiling.");
                SnippetsAssemblyInfo = Compiler.BuildSnippets(
                    snippets, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__snippets__.dll"), executionTarget);
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
                operationInfo, compilerMetadata, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__.dll"), executionTarget);
            if (EntryPointAssemblyInfo == null || logger.HasErrors)
            {
                Logger?.LogError($"Error compiling entry point for operation {operationName}.");
                throw new CompilationErrorsException(logger.Errors.ToArray());
            }

            var entryPointOperationInfo = EntryPointAssemblyInfo.Operations.Single();

            // Construct the EntryPointInfo<,> object
            var parameterTypes = entryPointOperationInfo.RoslynParameters.Select(p => p.ParameterType).ToArray();
            var typeCount = parameterTypes.Length;
            Type entryPointInputType =
                typeCount == 0 ? typeof(QVoid) :
                typeCount == 1 ? parameterTypes.Single() :
                PartialMapper.TupleTypes[typeCount].MakeGenericType(parameterTypes);
            Type entryPointOutputType = entryPointOperationInfo.ReturnType;

            Type entryPointInfoType = typeof(EntryPointInfo<,>).MakeGenericType(new Type[] { entryPointInputType, entryPointOutputType });
            var entryPointInfo = entryPointInfoType.GetConstructor(new Type[] { typeof(Type) })
                .Invoke(new object[] { entryPointOperationInfo.RoslynType });

            return new EntryPoint(entryPointInfo, entryPointInputType, entryPointOutputType, entryPointOperationInfo);
        }
    }
}
