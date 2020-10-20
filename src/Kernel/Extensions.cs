// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp.Jupyter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///      Extension methods to be used with various IQ# and Jupyter objects.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Adds services required for the IQ# kernel to a given service
        ///     collection.
        /// </summary>
        public static void AddIQSharpKernel(this IServiceCollection services)
        {
            services.AddSingleton<ISymbolResolver, Kernel.SymbolResolver>();
            services.AddSingleton<IMagicSymbolResolver, Kernel.MagicSymbolResolver>();
            services.AddSingleton<IExecutionEngine, Kernel.IQSharpEngine>();
            services.AddSingleton<IConfigurationSource, ConfigurationSource>();
        }

        internal static void RenderExecutionPath(this ExecutionPathTracer.ExecutionPathTracer tracer,
            IChannel channel,
            string executionPathDivId,
            int renderDepth,
            TraceVisualizationStyle style)
        {
            // Retrieve the `ExecutionPath` traced out by the `ExecutionPathTracer`
            var executionPath = tracer.GetExecutionPath();

            // Convert executionPath to JToken for serialization
            var executionPathJToken = JToken.FromObject(executionPath,
                new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore });

            // Send execution path to JavaScript via iopub for rendering
            channel.SendIoPubMessage(
                new Message
                {
                    Header = new MessageHeader
                    {
                        MessageType = "render_execution_path"
                    },
                    Content = new ExecutionPathVisualizerContent
                    (
                        executionPathJToken,
                        executionPathDivId,
                        renderDepth,
                        style
                    )
                }
            );
        }
    }
}
