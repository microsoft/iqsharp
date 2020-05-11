// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Abstract base class for IQ# magic symbols.
    /// </summary>
    public abstract class AbstractMagic : MagicSymbol
    {
        private string FirstArgumentName;

        /// <summary>
        ///     Constructs a new magic symbol given its name and documentation.
        /// </summary>
        public AbstractMagic(string keyword, Documentation docs, string firstArgumentName = "")
        {
            this.Name = $"%{keyword}";
            this.Documentation = docs;
            this.FirstArgumentName = firstArgumentName;

            this.Kind = SymbolKind.Magic;
            this.Execute = SafeExecute(this.Run);
        }

        /// <summary>
        ///     Given a function representing the execution of a magic command,
        ///     returns a new function that executes <paramref name="magic" />
        ///     and catches any exceptions that occur during execution. The
        ///     returned execution function displays the given exceptions to its
        ///     display channel.
        /// </summary>
        public Func<string, IChannel, Task<ExecutionResult>> SafeExecute(Func<string, IChannel, ExecutionResult> magic) =>
            async (input, channel) =>
            {
                channel = channel.WithNewLines();

                try
                {
                    return magic(input, channel);
                }
                catch (InvalidWorkspaceException ws)
                {
                    foreach (var m in ws.Errors) channel.Stderr(m);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
                catch (AggregateException agg)
                {
                    foreach (var e in agg.InnerExceptions) channel.Stderr(e?.Message);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
                catch (Exception e)
                {
                    channel.Stderr(e.Message);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
            };

        /// <summary>
        ///     Parses the input to a magic command, interpreting the input as
        ///     a name followed by a JSON-serialized dictionary.
        /// </summary>
        public static (string, Dictionary<string, string>) ParseInput(string input)
        {
            if (input == null) return (string.Empty, new Dictionary<string, string> { });
            var BLANK_SPACE = new char[1] { ' ' };

            var inputParts = input.Split(BLANK_SPACE, 2, StringSplitOptions.RemoveEmptyEntries);
            var name = inputParts.Length > 0 ? inputParts[0] : string.Empty;
            var args = inputParts.Length > 1
                    ? JsonConverters.JsonToDict(inputParts[1])
                    : new Dictionary<string, string> { };

            return (name, args);
        }

        /// <summary>
        ///     Parses the input to a magic command, interpreting the input as
        ///     a name followed by a JSON-serialized dictionary.
        /// </summary>
        public static Dictionary<string, string> JsonToDict(string input) =>
            !string.IsNullOrEmpty(input) ? JsonConverters.JsonToDict(input) : new Dictionary<string, string> { };

        /// <summary>
        ///     Parses the input parameters for a given magic symbol and returns a
        ///     <c>Dictionary</c> with the names and values of the parameters.
        /// </summary>
        public Dictionary<string, string> ParseInputParameters(string input)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();

            var args = input.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            if (args.Length > 0 &&
                !args[0].StartsWith("{") &&
                !args[0].Contains("=") &&
                !string.IsNullOrEmpty(FirstArgumentName))
            {
                keyValuePairs[FirstArgumentName] = args[0];
                args = args.Where((_, index) => index != 0).ToArray();
            }

            // See if the remaining arguments look like JSON. If so, try to parse as JSON.
            // Otherwise, try to parse as key=value pairs.
            if (args.Length > 0 && args[0].StartsWith("{"))
            {
                var jsonArgs = JsonToDict(string.Join(" ", args));
                foreach (var (key, jsonValue) in jsonArgs)
                {
                    keyValuePairs[key] = jsonValue;
                }
            }
            else
            {
                foreach (string arg in args)
                {
                    var tokens = arg.Split("=", 2);
                    var key = tokens[0].Trim();
                    object value = (tokens.Length == 1) ? true as object : tokens[1].Trim() as object;
                    var jsonValue = JObject.FromObject(value).ToString(Newtonsoft.Json.Formatting.None);
                    keyValuePairs[key] = jsonValue;
                }
            }

            return keyValuePairs;
        }

        /// <summary>
        ///     A method to be run when the magic command is executed.
        /// </summary>
        public abstract ExecutionResult Run(string input, IChannel channel);
    }
}
