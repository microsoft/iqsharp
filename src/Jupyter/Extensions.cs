// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using Newtonsoft.Json;
using NumSharp;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///      Extension methods to be used with various IQ# and Jupyter objects.
    /// </summary>
    public static class Extensions
    {
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
                if (value.ToObject<T>() is T obj)
                    action(obj);
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

        /// <summary>
        ///     Adds functionality to a given quantum simulator to display
        ///     diagnostic output and stack traces for exceptions.
        /// </summary>
        /// <param name="simulator">
        ///     The simulator to be augmented with stack trace display functionality.
        /// </param>
        /// <param name="channel">
        ///     The Jupyter display channel to be used to display stack traces.
        /// </param>
        /// <returns>
        ///     The value of <paramref name="simulator" />.
        /// </returns>
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
        ///      Retrieves and JSON-decodes the value for the given parameter name.
        /// </summary>
        /// <typeparam name="T">
        ///      The expected type of the decoded parameter.
        /// </typeparam>
        /// <param name="parameters">
        ///     Dictionary from parameter names to JSON-encoded values.
        /// </param>
        /// <param name="parameterName">
        ///     The name of the parameter to be decoded.
        /// </param>
        /// <param name="decoded">
        ///     The returned value of the parameter once it has been decoded.
        /// </param>
        /// <param name="defaultValue">
        ///      The default value to be returned if no parameter with the
        ///      name <paramref name="parameterName"/> is present in the
        ///      dictionary.
        /// </param>
        public static bool TryDecodeParameter<T>(this Dictionary<string, string> parameters, string parameterName, out T decoded, T defaultValue = default)
        where T: struct
        {
            try
            {
                decoded = (T)(parameters.DecodeParameter(parameterName, typeof(T), defaultValue)!);
                return true;
            }
            catch
            {
                decoded = default;
                return false;
            }
        }

        /// <summary>
        ///      Retrieves and JSON-decodes the value for the given parameter name.
        /// </summary>
        /// <typeparam name="T">
        ///      The expected type of the decoded parameter.
        /// </typeparam>
        /// <param name="parameters">
        ///     Dictionary from parameter names to JSON-encoded values.
        /// </param>
        /// <param name="parameterName">
        ///     The name of the parameter to be decoded.
        /// </param>
        /// <param name="defaultValue">
        ///      The default value to be returned if no parameter with the
        ///      name <paramref name="parameterName"/> is present in the
        ///      dictionary.
        /// </param>
        public static T DecodeParameter<T>(this Dictionary<string, string> parameters, string parameterName, T defaultValue = default)
        where T: notnull =>
            // NB: We can assert that this is not null here, since the where
            //     clause ensures that T is not a nullable type, such that
            //     defaultValue cannot be null. This is not tracked by the
            //     return type of `object?`, such that we need to null-forgive.
            (T)(parameters.DecodeParameter(parameterName, typeof(T), defaultValue)!);

        /// <summary>
        ///      Retrieves and JSON-decodes the value for the given parameter name.
        /// </summary>
        /// <param name="parameters">
        ///     Dictionary from parameter names to JSON-encoded values.
        /// </param>
        /// <param name="parameterName">
        ///     The name of the parameter to be decoded.
        /// </param>
        /// <param name="type">
        ///      The expected type of the decoded parameter.
        /// </param>
        /// <param name="defaultValue">
        ///      The default value to be returned if no parameter with the
        ///      name <paramref name="parameterName"/> is present in the
        ///      dictionary.
        /// </param>
        public static object? DecodeParameter(this Dictionary<string, string> parameters, string parameterName, Type type, object? defaultValue = default)
        {
            if (!parameters.TryGetValue(parameterName, out string parameterValue))
            {
                return defaultValue!;
            }

            // If this is a JSON-formatted parameter that's being deserialized into a dictionary, remove the extra quotes and backslashes.   
            if (type == typeof(ImmutableDictionary<string, string>) && parameterValue.Length > 1)
            {
                // Jupyter wraps JSON in double quotes. Make sure that's what we have...
                if (parameterValue[0] == '"' && parameterValue[parameterValue.Length - 1] == '"')
                {
                    parameterValue = parameterValue.Substring(1, parameterValue.Length - 2);    // Strip off the enclosing quotes
                    parameterValue = parameterValue.Replace("\\\"", "\"");                      // Don't escape the interior quotes
                }
            }

            return JsonConvert.DeserializeObject(parameterValue, type) ?? defaultValue;
        }

        /// <summary>
        /// Makes the channel to start capturing the Console Output.
        /// Returns the current TextWriter in the Console so callers can set it back.
        /// </summary>
        /// <param name="channel">The channel to redirect console output to.</param>
        /// <returns>The current System.Console.Out</returns>
        public static System.IO.TextWriter? CaptureConsole(this IChannel channel)
        {
            var current = System.Console.Out;
            System.Console.SetOut(new ChannelWriter(channel));
            return current;
        }

        internal static string AsLaTeXMatrixOfComplex(this NDArray array) =>
            // NB: Assumes 𝑛 × 𝑛 × 2 array, where the trailing index is
            //     [real, imag].
            // TODO: Consolidate with logic at:
            //       https://github.com/microsoft/QuantumLibraries/blob/505fc27383c9914c3e1f60fb63d0acfe60b11956/Visualization/src/DisplayableUnitaryEncoders.cs#L43
            string.Join(
                "\\\\\n",
                Enumerable
                    .Range(0, array.Shape[0])
                    .Select(
                        idxRow => string.Join(" & ",
                            Enumerable
                                .Range(0, array.Shape[1])
                                .Select(
                                    idxCol => $"{array[idxRow, idxCol, 0]} + {array[idxRow, idxCol, 1]} i"
                                )
                        )
                    )
            );

        internal static IEnumerable<NDArray> IterateOverLeftmostIndex(this NDArray array)
        {
            foreach (var idx in Enumerable.Range(0, array.shape[0]))
            {
                yield return array[idx, Slice.Ellipsis];
            }
        }

        internal static string AsTextMatrixOfComplex(this NDArray array, string rowSep = "\n") =>
            // NB: Assumes 𝑛 × 𝑛 × 2 array, where the trailing index is
            //     [real, imag].
            // TODO: Consolidate with logic at:
            //       https://github.com/microsoft/QuantumLibraries/blob/505fc27383c9914c3e1f60fb63d0acfe60b11956/Visualization/src/DisplayableUnitaryEncoders.cs#L43
            "[" + rowSep + string.Join(
                rowSep,
                Enumerable
                    .Range(0, array.Shape[0])
                    .Select(
                        idxRow => "[" + string.Join(", ",
                            Enumerable
                                .Range(0, array.Shape[1])
                                .Select(
                                    idxCol => $"{array[idxRow, idxCol, 0]} + {array[idxRow, idxCol, 1]} i"
                                )
                        ) + "]"
                    )
            ) + rowSep + "]";

        
        internal static IEnumerable<QsDeclarationAttribute> GetAttributesByName(
            this OperationInfo operation, string attributeName,
            string namespaceName = "Microsoft.Quantum.Documentation"
        ) =>
            operation.Header.Attributes.Where(
                attribute =>
                    // Since QsNullable<UserDefinedType>.Item can be null,
                    // we use a pattern match here to make sure that we have
                    // an actual UDT to compare against.
                    attribute.TypeId.Item is UserDefinedType udt &&
                    udt.Namespace == namespaceName &&
                    udt.Name == attributeName
            );

        internal static bool TryAsStringLiteral(this TypedExpression expression, [NotNullWhen(true)] out string? value)
        {
            if (expression.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.StringLiteral literal)
            {
                value = literal.Item1;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
        internal static IEnumerable<string> GetStringAttributes(
            this OperationInfo operation, string attributeName,
            string namespaceName = "Microsoft.Quantum.Documentation"
        ) => operation
            .GetAttributesByName(attributeName, namespaceName)
            .Select(
                attribute =>
                    attribute.Argument.TryAsStringLiteral(out var value)
                    ? value : null
            )
            .Where(value => value != null)
            // The Where above ensures that all elements are non-nullable,
            // but the C# compiler doesn't quite figure that out, so we
            // need to help it with a no-op that uses the null-forgiving
            // operator.
            .Select(value => value!);

        internal static IDictionary<string?, string?> GetDictionaryAttributes(
            this OperationInfo operation, string attributeName,
            string namespaceName = "Microsoft.Quantum.Documentation"
        ) => operation
            .GetAttributesByName(attributeName, namespaceName)
            .SelectMany(
                attribute => attribute.Argument.Expression switch
                {
                    QsExpressionKind<TypedExpression, Identifier, ResolvedType>.ValueTuple tuple =>
                        tuple.Item.Length != 2
                        ? throw new System.Exception("Expected attribute to be a tuple of two strings.")
                        : ImmutableList.Create((tuple.Item[0], tuple.Item[1])),
                    _ => ImmutableList<(TypedExpression, TypedExpression)>.Empty
                }
            )
            .ToDictionary(
                attribute => attribute.Item1.TryAsStringLiteral(out var value) ? value : null,
                attribute => attribute.Item2.TryAsStringLiteral(out var value) ? value : null
            );
    }
}
