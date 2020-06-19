// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to list jobs in an Azure Quantum workspace.
    /// </summary>
    public class JobsMagic : AzureClientMagicBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobsMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public JobsMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.jobs",
                new Documentation
                {
                    Summary = "Displays a list of jobs in the current Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for displaying the list of jobs in the current 
                        Azure Quantum workspace.

                        The Azure Quantum workspace must previously have been initialized
                        using the %azure.connect magic command.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Print the list of jobs:
                            ```
                            In []: %azure.jobs
                            Out[]: <list of jobs in the workspace>
                            ```
                        ".Dedent(),
                    },
                }) {}

        /// <summary>
        ///     Lists all jobs in the active workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken) =>
            await AzureClient.GetJobListAsync(channel);
    }
}