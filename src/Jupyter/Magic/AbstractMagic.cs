// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    public abstract class AbstractMagic : MagicSymbol
    {
        public AbstractMagic(string keyword, Documentation docs)
        {
            this.Name = $"%{keyword}";
            this.Documentation = docs;

            this.Kind = SymbolKind.Magic;
            this.Execute = SafeExecute(this.Run);
        }

        public Func<string, IChannel, ExecutionResult> SafeExecute(Func<string, IChannel, ExecutionResult> magic) => 
            (input, channel) =>
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

        public abstract ExecutionResult Run(string input, IChannel channel);
    }
}
