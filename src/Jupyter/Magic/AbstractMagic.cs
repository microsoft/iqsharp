// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Abstract base class for IQ# magic symbols.
    /// </summary>
    public abstract class AbstractMagic : MagicSymbol
    {
        /// <summary>
        ///     Constructs a new magic symbol given its name and documentation.
        /// </summary>
        public AbstractMagic(string keyword, Documentation docs)
        {
            this.Name = $"%{keyword}";
            this.Documentation = docs;

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
        ///     A method to be run when the magic command is executed.
        /// </summary>
        public abstract ExecutionResult Run(string input, IChannel channel);
    }
}
