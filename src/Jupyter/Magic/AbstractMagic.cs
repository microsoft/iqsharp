// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler.Serialization;
// NB: The name `Documentation` can be ambiguous in this context,
//     since we rely both on Microsoft.Quantum.Documentation and on
//     the name from Jupyter Core.
using JupyterDocumentation = Microsoft.Jupyter.Core.Documentation;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Abstract base class for IQ# magic symbols.
    /// </summary>
    public abstract class AbstractMagic : CancellableMagicSymbol
    {
        private ILogger? Logger;

        /// <summary>
        ///     Constructs a new magic symbol given its name and documentation.
        /// </summary>
        public AbstractMagic(string keyword, JupyterDocumentation docs, ILogger? logger = null)
        {
            this.Name = $"%{keyword}";
            this.Documentation = docs;

            this.Kind = SymbolKind.Magic;
            this.ExecuteCancellable = this.SafeExecute(this.RunCancellable);

            this.Logger = logger;
        }

        /// <summary>
        ///     Given a function representing the execution of a magic command,
        ///     returns a new function that executes <paramref name="magic" />
        ///     and catches any exceptions that occur during execution. The
        ///     returned execution function displays the given exceptions to its
        ///     display channel.
        /// </summary>
        public Func<string, IChannel, CancellationToken, Task<ExecutionResult>> SafeExecute(
            Func<string, IChannel, CancellationToken, ExecutionResult> magic) =>
                async (input, channel, cancellationToken) =>
                {
                    channel = channel.WithNewLines();

                    try
                    {
                        return await Task.Run(() => magic(input, channel, cancellationToken));
                    }
                    catch (TaskCanceledException tce)
                    {
                        // Rethrow so that the jupyter-core library can
                        // properly handle the task cancellation.
                        throw tce;
                    }
                    catch (InvalidWorkspaceException ws)
                    {
                        foreach (var m in ws.Errors) channel.Stderr(m);
                        return ExecuteStatus.Error.ToExecutionResult();
                    }
                    catch (AggregateException agg)
                    {
                        Logger?.LogWarning(agg, "Unhandled aggregate exception in magic command {Magic}, printing as stderr.", this.Name);
                        foreach (var e in agg.InnerExceptions) channel.Stderr(e?.Message);
                        return ExecuteStatus.Error.ToExecutionResult();
                    }
                    catch (Exception e)
                    {
                        Logger?.LogWarning(e, "Unhandled exception in magic command {Magic}, printing as stderr.", this.Name);
                        channel.Stderr(e.Message);
                        return ExecuteStatus.Error.ToExecutionResult();
                    }
                };

        /// <summary>
        ///     Parses the input to a magic command, interpreting the input as
        ///     a name followed by a JSON-serialized dictionary.
        /// </summary>
        public static Dictionary<string, string> JsonToDict(string input) =>
            !string.IsNullOrEmpty(input) ? JsonConverters.JsonToDict(input) : new Dictionary<string, string> { };

        /// <summary>
        ///     Parses the input parameters for a given magic symbol and returns a
        ///     <c>Dictionary</c> with the names and values of the parameters,
        ///     where the values of the <c>Dictionary</c> are JSON-serialized objects.
        /// </summary>
        public static Dictionary<string, string> ParseInputParameters(string input, string firstParameterInferredName = "")
        {
            Dictionary<string, string> inputParameters = new Dictionary<string, string>();

            // This regex looks for four types of matches:
            // 1. (\{.*\})
            //      Matches anything enclosed in matching curly braces.
            // 2. [^\s"]+(?:\s*=\s*)(?:"[^"]*"|[^\s"]*)*
            //      Matches things that look like key=value, allowing whitespace around the equals sign,
            //      and allowing value to be a quoted string, e.g., key="value".
            // 3. [^\s"]+(?:"[^"]*"[^\s"]*)*
            //      Matches things that are single words, not inside quotes.
            // 4. (?:"[^"]*"[^\s"]*)+
            //      Matches quoted strings.
            var regex = new Regex(@"(\{.*\})|[^\s""]+(?:\s*=\s*)(?:""[^""]*""|[^\s""]*)*|[^\s""]+(?:""[^""]*""[^\s""]*)*|(?:""[^""]*""[^\s""]*)+");
            var args = regex.Matches(input).Select(match => match.Value);

            var regexBeginEndQuotes = new Regex(@"^['""]|['""]$");

            // If we are expecting a first inferred-name parameter, see if it exists.
            // If so, serialize it to the dictionary as JSON and remove it from the list of args.
            if (args.Any() &&
                !args.First().StartsWith("{") &&
                !args.First().Contains("=") &&
                !string.IsNullOrEmpty(firstParameterInferredName))
            {
                using var writer = new StringWriter();
                Json.Serializer.Serialize(writer, regexBeginEndQuotes.Replace(args.First(), string.Empty));
                inputParameters[firstParameterInferredName] = writer.ToString();
                args = args.Skip(1);
            }

            // See if the remaining arguments look like JSON. If so, parse as JSON.
            if (args.Any() && args.First().StartsWith("{"))
            {
                var jsonArgs = JsonToDict(args.First());
                foreach (var (key, jsonValue) in jsonArgs)
                {
                    inputParameters[key] = jsonValue;
                }

                return inputParameters;
            }

            // Otherwise, try to parse as key=value pairs and serialize into the dictionary as JSON.
            foreach (string arg in args)
            {
                var tokens = arg.Split("=", 2);
                var key = regexBeginEndQuotes.Replace(tokens[0].Trim(), string.Empty);
                var value = tokens.Length switch
                {
                    // If there was no value provided explicitly, treat it as an implicit "true" value
                    1 => true as object,

                    // Trim whitespace and also enclosing single-quotes or double-quotes before returning
                    2 => regexBeginEndQuotes.Replace(tokens[1].Trim(), string.Empty) as object,

                    // We called arg.Split("=", 2), so there should never be more than 2
                    _ => throw new InvalidOperationException()
                };
                using var writer = new StringWriter();
                Json.Serializer.Serialize(writer, value);
                inputParameters[key] = writer.ToString();
            }

            return inputParameters;
        }

        /// <summary>
        ///     A method to be run when the magic command is executed.
        /// </summary>
        public abstract ExecutionResult Run(string input, IChannel channel);

        /// <summary>
        ///     A method to be run when the magic command is executed, including a cancellation
        ///     token to use for requesting cancellation.
        /// </summary>
        /// <remarks>
        ///     The default implementation in <see cref="AbstractMagic"/> ignores the cancellation token.
        ///     Derived classes should override this method and monitor the cancellation token if they
        ///     wish to support cancellation.
        /// </remarks>
        public virtual ExecutionResult RunCancellable(string input, IChannel channel, CancellationToken cancellationToken) =>
            Run(input, channel);
    }
}
