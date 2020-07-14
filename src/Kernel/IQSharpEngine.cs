// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Microsoft.Jupyter.Core.Protocol;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.Quantum.IQSharp.AzureClient;

namespace Microsoft.Quantum.IQSharp.Kernel
{

    /// <summary>
    ///  The IQsharpEngine, used to expose Q# as a Jupyter kernel.
    /// </summary>
    public class IQSharpEngine : BaseEngine
    {
        private readonly PerformanceMonitor performanceMonitor;

        /// <summary>
        /// The main constructor. It expects an `ISnippets` instance that takes care
        /// of compiling and keeping track of the code Snippets provided by users.
        /// </summary>
        public IQSharpEngine(
            IShellServer shell,
            IOptions<KernelContext> context,
            ILogger<IQSharpEngine> logger,
            IServiceProvider services,
            IConfigurationSource configurationSource,
            PerformanceMonitor performanceMonitor,
            IShellRouter shellRouter,
            IEventService eventService,
            IMagicSymbolResolver magicSymbolResolver,
            IReferences references
        ) : base(shell, shellRouter, context, logger, services)
        {
            this.performanceMonitor = performanceMonitor;
            performanceMonitor.Start();

            this.Snippets = services.GetService<ISnippets>();
            this.SymbolsResolver = services.GetService<ISymbolResolver>();
            this.MagicResolver = magicSymbolResolver;

            RegisterDisplayEncoder(new IQSharpSymbolToHtmlResultEncoder());
            RegisterDisplayEncoder(new IQSharpSymbolToTextResultEncoder());
            RegisterDisplayEncoder(new TaskStatusToTextEncoder());
            RegisterDisplayEncoder(new StateVectorToHtmlResultEncoder(configurationSource));
            RegisterDisplayEncoder(new StateVectorToTextResultEncoder(configurationSource));
            RegisterDisplayEncoder(new DataTableToHtmlEncoder());
            RegisterDisplayEncoder(new DataTableToTextEncoder());
            RegisterDisplayEncoder(new DisplayableExceptionToHtmlEncoder());
            RegisterDisplayEncoder(new DisplayableExceptionToTextEncoder());
            RegisterDisplayEncoder(new ExecutionPathToHtmlEncoder());

            RegisterJsonEncoder(
                JsonConverters.AllConverters
                .Concat(AzureClient.JsonConverters.AllConverters)
                .ToArray());

            RegisterSymbolResolver(this.SymbolsResolver);
            RegisterSymbolResolver(this.MagicResolver);

            RegisterPackageLoadedEvent(services, logger, references);

            // Handle new shell messages.
            shellRouter.RegisterHandlers<IQSharpEngine>();

            // Report performance after completing startup.
            performanceMonitor.Report();
            logger.LogInformation(
                "IQ# engine started successfully as process {Process}.",
                Process.GetCurrentProcess().Id
            );


            eventService?.TriggerServiceInitialized<IExecutionEngine>(this);
        }

        /// <summary>
        ///     Registers an event handler that searches newly loaded packages
        ///     for extensions to this engine (in particular, for result encoders).
        /// </summary>
        private void RegisterPackageLoadedEvent(IServiceProvider services, ILogger logger, IReferences references)
        {
            var knownAssemblies = references
                .Assemblies
                .Select(asm => asm.Assembly.GetName())
                .ToImmutableHashSet()
                // Except assemblies known at compile-time as well.
                .Add(typeof(StateVectorToHtmlResultEncoder).Assembly.GetName())
                .Add(typeof(AzureClientErrorToHtmlEncoder).Assembly.GetName());
            foreach (var knownAssembly in knownAssemblies) System.Console.WriteLine($"{knownAssembly.FullName}");

            // Register new display encoders when packages load.
            references.PackageLoaded += (sender, args) =>
            {
                logger.LogDebug("Scanning for display encoders after loading {Package}.", args.PackageId);
                foreach (var assembly in references.Assemblies
                                                   .Select(asm => asm.Assembly)
                                                   .Where(asm => !knownAssemblies.Contains(asm.GetName()))
                )
                {
                    // Look for display encoders in the new assembly.
                    logger.LogDebug("Found new assembly {Name}, looking for display encoders.", assembly.FullName);
                    var relevantTypes = assembly
                        .GetTypes()
                        .Where(type =>
                            !type.IsAbstract &&
                            !type.IsInterface &&
                            typeof(IResultEncoder).IsAssignableFrom(type)
                        );

                    foreach (var type in relevantTypes)
                    {
                        logger.LogDebug(
                            "Found display encoder {TypeName} in {AssemblyName}; registering.",
                            type.FullName,
                            assembly.FullName
                        );

                        // Try and instantiate the new result encoder, but if it fails, that is likely
                        // a non-critical failure that should result in a warning.
                        try
                        {
                            RegisterDisplayEncoder(ActivatorUtilities.CreateInstance(services, type) as IResultEncoder);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(
                                ex,
                                "Encountered exception loading result encoder {TypeName} from {AssemblyName}.",
                                type.FullName, assembly.FullName
                            );
                        }
                    }
                    knownAssemblies = knownAssemblies.Add(assembly.GetName());
                }
            };
        }

        internal ISnippets Snippets { get; }

        internal ISymbolResolver SymbolsResolver { get; }

        internal ISymbolResolver MagicResolver { get; }

        /// <summary>
        /// This is the method used to execute Jupyter "normal" cells. In this case, a normal
        /// cell is expected to have a Q# snippet, which gets compiled and we return the name of
        /// the operations found. These operations are then available for simulation and estimate.
        /// </summary>
        public override async Task<ExecutionResult> ExecuteMundane(string input, IChannel channel)
        {
            channel = channel.WithNewLines();

            try
            {
                var code = Snippets.Compile(input);

                foreach(var m in code.warnings) { channel.Stdout(m); }

                // Gets the names of all the operations found for this snippet
                var opsNames =
                    code.Elements?
                        .Where(e => e.IsQsCallable)
                        .Select(e => e.ToFullName().WithoutNamespace(IQSharp.Snippets.SNIPPETS_NAMESPACE))
                        .ToArray();

                return opsNames.ToExecutionResult();
            }
            catch (CompilationErrorsException c)
            {
                foreach (var m in c.Errors) channel.Stderr(m);
                return ExecuteStatus.Error.ToExecutionResult();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Exception while executing mundane input: {Input}", input);
                channel.Stderr(e.Message);
                return ExecuteStatus.Error.ToExecutionResult();
            }
            finally
            {
                performanceMonitor.Report();
            }
        }
    }
}

