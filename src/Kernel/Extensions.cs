// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Common;

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

        /// <summary>
        ///     Attaches <c>ExecutionPathTracer</c> event listeners to the simulator to generate
        ///     the <c>ExecutionPath</c> of the operation performed by the simulator.
        /// </summary>
        public static T WithExecutionPathTracer<T>(this T sim, ExecutionPathTracer tracer) where T : SimulatorBase
        {
            sim.OnOperationStart += tracer.OnOperationStartHandler;
            sim.OnOperationEnd += tracer.OnOperationEndHandler;
            return sim;
        }
    }
}
