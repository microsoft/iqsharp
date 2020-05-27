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
    public class ExecuteMagic : AzureClientMagicBase
    {
        /// <summary>
        ///      Gets the symbol resolver used by this magic command to find
        ///      operations or functions to be simulated.
        /// </summary>
        public IOperationResolver OperationResolver { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteMagic"/> class.
        /// </summary>
        /// <param name="operationResolver">
        /// The <see cref="IOperationResolver"/> object used to find and resolve operations.
        /// </param>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public ExecuteMagic(IOperationResolver operationResolver, IAzureClient azureClient)
            : base(
                azureClient,
                "azure.execute",
                new Documentation
                {
                    Summary = "Executes a job in an Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for executing a job in an Azure Quantum workspace
                        corresponding to the Q# operation provided as an argument, and it waits
                        for the job to complete before returning.

                        The Azure Quantum workspace must previously have been initialized
                        using the %azure.connect magic command.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Execute an operation in the current Azure Quantum workspace:
                            ```
                            In []: %azure.execute OPERATION_NAME
                            Out[]: Executing job on target TARGET_NAME...
                                   <job results displayed here after execution completes>
                            ```
                        ".Dedent(),
                    },
                }) =>
            this.OperationResolver = operationResolver;

        /// <summary>
        ///     Executes a new job to an Azure Quantum workspace given a Q# operation
        ///     name that is present in the current Q# Jupyter workspace, and
        ///     waits for the job to complete before returning.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            Dictionary<string, string> keyValuePairs = ParseInputParameters(input);
            var operationName = keyValuePairs.Keys.FirstOrDefault();
            return await AzureClient.ExecuteJobAsync(channel, OperationResolver, operationName);
        }
    }
}