// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to submit jobs to an Azure Quantum workspace.
    /// </summary>
    public class SubmitMagic : MagicSymbol
    {
        /// <summary>
        ///     Constructs a new magic command given a resolver used to find
        ///     operations and functions and an IAzureClient object.
        /// </summary>
        public SubmitMagic(IOperationResolver operationResolver, IAzureClient azureClient)
        {
            this.OperationResolver = operationResolver;
            this.AzureClient = azureClient;

            this.Name = "%submit";
            this.Kind = SymbolKind.Magic;
            this.Execute = async (input, channel) => await RunAsync(input, channel);
            this.Documentation = new Documentation
            {
                Summary = "Submits a job to an Azure Quantum workspace.",
                Description = @"
                    This magic command allows for submitting a job to an Azure Quantum workspace
                    corresponding to the Q# operation provided as an argument.

                    The Azure Quantum workspace must previously have been initialized
                    using the %connect magic command.
                ".Dedent(),
                Examples = new[]
                {
                    @"
                        Submit an operation as a new job to the current Azure Quantum workspace:
                        ```
                        In []: %submit OPERATION_NAME
                        Out[]: Submitted job JOB_ID
                        ```
                    ".Dedent(),
                }
            };
        }

        /// <summary>
        ///      The symbol resolver used by this magic command to find
        ///      operations or functions to be simulated.
        /// </summary>
        public IOperationResolver OperationResolver { get; }

        /// <summary>
        ///     The object used by this magic command to interact with Azure.
        /// </summary>
        public IAzureClient AzureClient { get; }

        /// <summary>
        ///     Submits a new job to an Azure Quantum workspace given a Q# operation
        ///     name that is present in the current Q# Jupyter workspace.
        /// </summary>
        public async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            channel = channel.WithNewLines();

            Dictionary<string, string> keyValuePairs = this.ParseInput(input);
            var operationName = keyValuePairs.Keys.FirstOrDefault();
            return await AzureClient.SubmitJobAsync(channel, OperationResolver, operationName).ToExecutionResult();
        }
    }
}