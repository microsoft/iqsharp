// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
        ///     Constructs a new magic command given an IAzureClient object.
        /// </summary>
        public JobsMagic(IAzureClient azureClient) :
            base(azureClient,
                "jobs",
                new Documentation
                {
                    Summary = "Displays a list of jobs in the current Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for displaying the list of jobs in the current 
                        Azure Quantum workspace.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Print status about a specific job:
                            ```
                            In []: %jobs
                            Out[]: <list of jobs in the workspace>
                            ```
                        ".Dedent()
                    }
                }) {}

        /// <summary>
        ///     Lists all jobs in the active workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            return await AzureClient.GetJobListAsync(channel);
        }
    }
}