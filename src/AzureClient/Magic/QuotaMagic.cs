// Copyright (c) Microsoft Corporation.
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
    public class QuotaMagic : AzureClientMagicBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public QuotaMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.quotas",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Displays a list of quotas for the current Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for displaying quota information for the current 
                        Azure Quantum workspace.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.connect).
                                                
                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Get the list of quotas:
                            ```
                            In []: %azure.quotas
                            Out[]: <quota information for the workspace>
                            ```
                        ".Dedent()
                    },
                }) {}

        /// <summary>
        ///     Lists all jobs in the active workspace, optionally filtered by a provided parameter.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            return await AzureClient.GetQuotaListAsync(channel);
        }
    }
}
