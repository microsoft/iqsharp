// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public static class Extensions
    {
        /// <summary>
        /// Creates a wrapper of an IChannel that adds new lines to every message
        /// sent to stdout and stderr
        /// </summary>
        public static ChannelWithNewLines WithNewLines(this IChannel original) =>
            (original is ChannelWithNewLines ch) ? ch : new ChannelWithNewLines(original);

        public static void AddIQSharpKernel(this IServiceCollection services)
        {
            services.AddSingleton<ISymbolResolver, Jupyter.SymbolResolver>();
            services.AddSingleton<IExecutionEngine, Jupyter.IQSharpEngine>();
            services.AddSingleton<IConfigurationSource, ConfigurationSource>();
        }

        internal static IConfigurationSource ApplyConfiguration<T>(
            this IConfigurationSource configurationSource,
            string keyName, Action<T> action
        )
        {
            if (configurationSource.Configuration.TryGetValue(keyName, out var value))
            {
                action(value.ToObject<T>());
            }
            return configurationSource;
        }

        public static QuantumSimulator WithJupyterDisplay(this QuantumSimulator simulator, IChannel channel, IConfigurationSource configurationSource)
        {
            simulator.DisableLogToConsole();
            simulator.OnLog += channel.Stdout;

            simulator.Register(
                typeof(Diagnostics.DumpMachine<>), typeof(JupyterDumpMachine<>),
                signature: typeof(ICallable)
            );

            var op = ((GenericCallable)simulator.GetInstance(typeof(Microsoft.Quantum.Diagnostics.DumpMachine<>)));
            var concreteOp = op.FindCallable(typeof(QVoid), typeof(QVoid));
            ((JupyterDumpMachine<QVoid>)concreteOp).Channel = channel;
            ((JupyterDumpMachine<QVoid>)concreteOp).ConfigurationSource = configurationSource;
            concreteOp = op.FindCallable(typeof(string), typeof(QVoid));
            ((JupyterDumpMachine<string>)concreteOp).Channel = channel;
            ((JupyterDumpMachine<string>)concreteOp).ConfigurationSource = configurationSource;

            simulator.Register(
                typeof(Diagnostics.DumpRegister<>), typeof(JupyterDumpRegister<>),
                signature: typeof(ICallable)
            );

            op = ((GenericCallable)simulator.GetInstance(typeof(Microsoft.Quantum.Diagnostics.DumpRegister<>)));
            concreteOp = op.FindCallable(typeof(QVoid), typeof(QVoid));
            ((JupyterDumpRegister<QVoid>)concreteOp).Channel = channel;
            ((JupyterDumpRegister<QVoid>)concreteOp).ConfigurationSource = configurationSource;
            concreteOp = op.FindCallable(typeof(string), typeof(QVoid));
            ((JupyterDumpRegister<string>)concreteOp).Channel = channel;
            ((JupyterDumpRegister<string>)concreteOp).ConfigurationSource = configurationSource;

            return simulator;
        }
    }
}
