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
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <inheritdoc/>
    internal class EntryPointGenerator : IEntryPointGenerator
    {
        private ICompilerService Compiler { get; }
        private IOperationResolver OperationResolver { get; }
        private IWorkspace Workspace { get; }
        private ISnippets Snippets { get; }
        private IReferences References { get; }
        private ILogger<EntryPointGenerator> Logger { get; }
        private Lazy<CompilerMetadata> CompilerMetadata { get; set; }
        private AssemblyInfo AssemblyInfo { get; set; } = new AssemblyInfo(null);

        public EntryPointGenerator(
            ICompilerService compiler,
            IOperationResolver operationResolver,
            IWorkspace workspace,
            ISnippets snippets,
            IReferences references,
            ILogger<EntryPointGenerator> logger,
            IEventService eventService)
        {
            Compiler = compiler;
            OperationResolver = operationResolver;
            Workspace = workspace;
            Snippets = snippets;
            References = references;
            Logger = logger;
            CompilerMetadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);

            Workspace.Reloaded += OnWorkspaceReloaded;
            References.PackageLoaded += OnGlobalReferencesPackageLoaded;

            AssemblyLoadContext.Default.Resolving += Resolve;

            eventService?.TriggerServiceInitialized<IEntryPointGenerator>(this);
        }
        private void OnGlobalReferencesPackageLoaded(object sender, PackageLoadedEventArgs e) =>
            CompilerMetadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);

        private void OnWorkspaceReloaded(object sender, ReloadedEventArgs e) =>
            CompilerMetadata = new Lazy<CompilerMetadata>(LoadCompilerMetadata);

        private CompilerMetadata LoadCompilerMetadata() =>
            Workspace.HasErrors
                    ? References?.CompilerMetadata.WithAssemblies(Snippets.AssemblyInfo)
                    : References?.CompilerMetadata.WithAssemblies(Snippets.AssemblyInfo, Workspace.AssemblyInfo);

        /// <summary>
        /// Because the assemblies are loaded into memory, we need to provide this method to the AssemblyLoadContext
        /// such that the Workspace assembly or this assembly is correctly resolved when it is executed for simulation.
        /// </summary>
        public Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            if (name.Name == Path.GetFileNameWithoutExtension(AssemblyInfo?.Location))
            {
                return AssemblyInfo.Assembly;
            }
            if (name.Name == Path.GetFileNameWithoutExtension(Snippets?.AssemblyInfo?.Location))
            {
                return Snippets.AssemblyInfo.Assembly;
            }
            else if (name.Name == Path.GetFileNameWithoutExtension(Workspace?.AssemblyInfo?.Location))
            {
                return Workspace.AssemblyInfo.Assembly;
            }

            return null;
        }

        public IEntryPoint Generate(string operationName)
        {
            var operationInfo = OperationResolver.Resolve(operationName);
            var logger = new QSharpLogger(Logger);
            AssemblyInfo = Compiler.BuildEntryPoint(operationInfo, CompilerMetadata.Value, logger, Path.Combine(Workspace.CacheFolder, "__entrypoint__.dll"));
            var entryPointOperationInfo = AssemblyInfo.Operations.Single();

            // TODO: Need these two lines to construct the Type objects correctly.
            Type entryPointInputType = entryPointOperationInfo.RoslynParameters.Select(p => p.ParameterType).DefaultIfEmpty(typeof(QVoid)).First(); // .Header.Signature.ArgumentType.GetType();
            Type entryPointOutputType = typeof(Result); // entryPointOperationInfo.Header.Signature.ReturnType.GetType();

            var entryPointInputOutputTypes = new Type[] { entryPointInputType, entryPointOutputType };
            Type entryPointInfoType = typeof(EntryPointInfo<,>).MakeGenericType(entryPointInputOutputTypes);
            var entryPointInfo = entryPointInfoType.GetConstructor(
                new Type[] { typeof(Type) }).Invoke(new object[] { entryPointOperationInfo.RoslynType });

            return new EntryPoint(entryPointInfo, entryPointInputOutputTypes, entryPointOperationInfo);
        }
    }
}
