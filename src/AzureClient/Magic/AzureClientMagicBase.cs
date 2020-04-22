using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     Base class used for Azure Client magic commands.
    /// </summary>
    public abstract class AzureClientMagicBase : MagicSymbol
    {
        /// <summary>
        ///     The object used by this magic command to interact with Azure.
        /// </summary>
        public IAzureClient AzureClient { get; }

        /// <summary>
        ///     Constructs the Azure Client magic command with the specified keyword
        ///     and documentation.
        /// </summary>
        public AzureClientMagicBase(IAzureClient azureClient, string keyword, Documentation docs)
        {
            this.AzureClient = azureClient;
            this.Name = $"%{keyword}";
            this.Documentation = docs;

            this.Kind = SymbolKind.Magic;
            this.Execute = SafeExecute(this.RunAsync);
        }

        /// <summary>
        ///     Performs Azure Client magic commands with safe exception handling
        ///     and translates the result from <c>AzureClient.AzureClientError</c>
        ///     to <c>Jupyter.Core.ExecutionResult</c>.
        /// </summary>
        public Func<string, IChannel, Task<ExecutionResult>> SafeExecute(Func<string, IChannel, Task<AzureClientError>> azureClientMagic) =>
            async (input, channel) =>
            {
                channel = channel.WithNewLines();

                try
                {
                    return await azureClientMagic(input, channel).ToExecutionResult();
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
        ///     Parses the input parameters for a given magic symbol and returns a
        ///     <c>Dictionary</c> with the names and values of the parameters.
        /// </summary>
        public Dictionary<string, string> ParseInput(string input)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            foreach (string arg in input.Split(null as char[], StringSplitOptions.RemoveEmptyEntries))
            {
                var tokens = arg.Split("=", 2);
                var key = tokens[0].Trim();
                var value = (tokens.Length == 1) ? string.Empty : tokens[1].Trim();
                keyValuePairs[key] = value;
            }
            return keyValuePairs;
        }

        /// <summary>
        ///     Executes the magic command functionality for the given input.
        /// </summary>
        public abstract Task<AzureClientError> RunAsync(string input, IChannel channel);
    }
}
