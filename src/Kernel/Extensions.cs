// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Microsoft.Quantum.IQSharp.Kernel
{
    /// <summary>
    ///      Extension methods to be used with various IQ# and Jupyter objects.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Creates a wrapper of an IChannel that adds new lines to every message
        /// sent to stdout and stderr
        /// </summary>
        public static ChannelWithNewLines WithNewLines(this IChannel original) =>
            (original is ChannelWithNewLines ch) ? ch : new ChannelWithNewLines(original);

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
        ///     Given a configuration source, applies an action if that
        ///     configuration source defines a value for a particular
        ///     configuration key.
        /// </summary>
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

        private static readonly IImmutableList<(long, string)> byteSuffixes = new List<(long, string)>
        {
            (1L << 50, "PiB"),
            (1L << 40, "TiB"),
            (1L << 30, "GiB"),
            (1L << 20, "MiB"),
            (1L << 10, "KiB")
        }.ToImmutableList();

        /// <summary>
        ///      Given a number of bytes, formats that number as a human
        ///      readable string by appending unit suffixes (i.e.: indicating
        ///      kilobytes, megabytes, etc.).
        /// </summary>
        /// <param name="nBytes">A number of bytes to be formatted.</param>
        /// <returns>
        ///     The number of bytes formatted as a human-readable string.
        /// </returns>
        public static string ToHumanReadableBytes(this long nBytes)
        {
            foreach (var (scale, suffix) in byteSuffixes)
            {
                if (nBytes >= scale)
                {
                    var coefficient = ((double)nBytes) / scale;
                    return $"{coefficient.ToString("0.###")} {suffix}";
                }
            }
            // Fall through to just bytes.
            return $"{nBytes} B";
        }

        /// <summary>
        ///     Adds functionality to a given quantum simulator to display
        ///     diagnostic output with rich Jupyter formatting.
        /// </summary>
        /// <param name="simulator">
        ///     The simulator to be augmented with Jupyter display
        ///     functionality.
        /// </param>
        /// <param name="channel">
        ///     The Jupyter display channel to be used to display diagnostic
        ///     output.
        /// </param>
        /// <param name="configurationSource">
        ///      A source of configuration options to be used to set display
        ///      preferences. Typically, this will be provided by the service
        ///      provider configured when an execution engine is constructed.
        /// </param>
        /// <returns>
        ///     The value of <paramref name="simulator" />.
        /// </returns>
        public static QuantumSimulator WithJupyterDisplay(this QuantumSimulator simulator, IChannel channel, IConfigurationSource configurationSource)
        {
            // First, we disable console-based logging so as to not
            // duplicate messages.
            simulator.DisableLogToConsole();
            // Next, we attach the display channel's standard output handling
            // to the log event.
            simulator.OnLog += channel.Stdout;

            // Next, we register the generic version of the DumpMachine callable
            // as an ICallable with the simulator. Below, we'll provide our
            // implementation of DumpMachine with the channel and configuration
            // source we got as arguments. At the moment, there's no direct
            // way to do this when registering an implementation, so we instead
            // get an instance of the newly registered callable and set its
            // properties accordingly.
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

            // Next, we repeat the whole process for DumpRegister instead of
            // DumpMachine.
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

        public static T WithStackTraceDisplay<T>(this T simulator, IChannel channel)
        where T: SimulatorBase
        {
            simulator.DisableExceptionPrinting();
            simulator.OnException += (exception, stackTrace) =>
            {
                channel.Display(new DisplayableException
                {
                    Exception = exception,
                    StackTrace = stackTrace
                });
            };
            return simulator;
        }

        /// <summary>
        ///      Removes common indents from each line in a string,
        ///      similarly to Python's <c>textwrap.dedent()</c> function.
        /// </summary>
        internal static string Dedent(this string text)
        {
            // First, start by finding the length of common indents,
            // disregarding lines that are only whitespace.
            var leadingWhitespaceRegex = new Regex(@"^[ \t]*");
            var minWhitespace = int.MaxValue;
            foreach (var line in text.Split("\n"))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var match = leadingWhitespaceRegex.Match(line);
                    minWhitespace = match.Success
                                ? System.Math.Min(minWhitespace, match.Value.Length)
                                : minWhitespace = 0;
                }
            }
            
            // We can use that to build a new regex that strips
            // out common indenting.
            var leftTrimRegex = new Regex(@$"^[ \t]{{{minWhitespace}}}", RegexOptions.Multiline);
            return leftTrimRegex.Replace(text, "");
        }
    }
}
