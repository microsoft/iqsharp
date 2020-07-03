// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.Core.ExecutionPathTracer
{
    /// <summary>
    /// Extension methods to be used with and by <see cref="ExecutionPathTracer">.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Attaches <c>ExecutionPathTracer</c> event listeners to the simulator to generate
        /// the <c>ExecutionPath</c> of the operation performed by the simulator.
        /// </summary>
        public static T WithExecutionPathTracer<T>(this T sim, ExecutionPathTracer tracer)
            where T : SimulatorBase
        {
            sim.OnOperationStart += tracer.OnOperationStartHandler;
            sim.OnOperationEnd += tracer.OnOperationEndHandler;
            return sim;
        }

        /// <summary>
        /// Gets the value associated with the specified key and creates a new entry with the <c>defaultVal</c> if
        /// the key doesn't exist.
        /// </summary>
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultVal)
        {
            TValue val;
            if (!dict.TryGetValue(key, out val))
            {
                val = defaultVal;
                dict.Add(key, val);
            }
            return val;
        }

        /// <summary>
        /// Gets the value associated with the specified key and creates a new entry of the default type if
        /// the key doesn't exist.
        /// </summary>
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            return dict.GetOrCreate(key, new TValue());
        }

        /// <summary>
        /// Given a <see cref="Type"/>, format its non-qubit arguments into a string.
        /// Returns null if no arguments found.
        /// </summary>
        public static string? ArgsToString(this Type t, object args)
        {
            var argsStrings = t.GetFields()
                .Select(f =>
                {
                    var argString = null as string;

                    // If field is a tuple, recursively extract its inner arguments and format as a tuple string.
                    if (f.FieldType.IsTuple())
                    {
                        var nestedArgs = f.GetValue(args);
                        if (nestedArgs != null) argString = f.FieldType.ArgsToString(nestedArgs);
                    }
                    // Add field as an argument if it is not a Qubit type
                    else if (!f.FieldType.IsQubitsContainer())
                    {
                        argString = f.GetValue(args)?.ToString();
                    }

                    return argString;
                })
                .WhereNotNull();

            return argsStrings.Any()
                ? $"({string.Join(",", argsStrings)})"
                : null;
        }
    }
}
