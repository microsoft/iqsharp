﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
            IMagicSymbolResolver magicSymbolResolver
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
            RegisterJsonEncoder(JsonConverters.AllConverters);

            RegisterSymbolResolver(this.SymbolsResolver);
            RegisterSymbolResolver(this.MagicResolver);

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

