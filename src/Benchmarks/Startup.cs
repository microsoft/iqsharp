// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.IQSharp.Mocks;

namespace Microsoft.Quantum.IQSharp.Benchmarks
{
    internal class Startup
    {
        public static async Task<IQSharpEngine> StartEngine()
        {
            var services = new ServiceCollection();
            services.AddIQSharp();
            services.AddIQSharpKernel();
            // Disable logging to focus on realistic performance.
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
            });
            // Add mock implementations of Jupyter protocol services, since
            // we aren't actually running in a Jupyter context.
            services.AddMocks();
            // Disable telemetry during benchmarking.
            services.AddSingleton<ITelemetryService, NullTelemetryService>();

            var serviceProvider = services.BuildServiceProvider();
            var engine = serviceProvider.GetRequiredService<IExecutionEngine>() as IQSharpEngine;
            Debug.Assert(engine != null);
            engine.Start();
            await engine.Initialized;
            await ((serviceProvider.GetRequiredService<IWorkspace>() as Workspace)?.Initialization ?? Task.CompletedTask);
            return engine;
        }
    }
}
