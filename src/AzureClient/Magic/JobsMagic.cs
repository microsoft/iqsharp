// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to list jobs in an Azure Quantum workspace.
    /// </summary>
    public class JobsMagic : AzureClientMagicBase
    {
        private const string ParameterNameFilter = "__filter__";
        private const string ParameterNameCount = "count";

        private const int CountDefaultValue = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobsMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        /// <param name="logger">Logger instance for messages.</param>
        public JobsMagic(IAzureClient azureClient, ILogger<JobsMagic> logger)
            : base(
                azureClient,
                "azure.jobs",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Displays a list of jobs in the current Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for displaying the list of jobs in the current 
                        Azure Quantum workspace, optionally filtering the list to jobs which
                        have an ID, name, or target containing the provided filter parameter.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command]({KnownUris.ReferenceForMagicCommand("azure.connect")}).
                        
                        #### Optional parameters

                        - A string to filter the list of jobs. Jobs which have an ID, name, or target
                        containing the provided filter parameter will be displayed. If not specified,
                        no job is filtered.
                        - `{ParameterNameCount}=<integer>` (default={CountDefaultValue}): The max number of jobs to return.

                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Get the list of jobs:
                            ```
                            In []: %azure.jobs
                            Out[]: <detailed status of all recent jobs in the workspace>
                            ```
                        ".Dedent(),

                        @"
                            Get the list of jobs whose ID, name, or target contains ""My job"":
                            ```
                            In []: %azure.jobs ""My job""
                            Out[]: <detailed status of matching jobs in the workspace>
                            ```
                        ".Dedent(),

                        @"
                            Get the list of jobs whose ID, name, or target contains ""My job"", limit it to at most 100 jobs:
                            ```
                            In []: %azure.jobs ""My job"" count=100
                            Out[]: <detailed status of at most 100 matching jobs in the workspace>
                            ```
                        ".Dedent(),
                    },
                }, logger) {}

        /// <summary>
        ///     Lists all jobs in the active workspace, optionally filtered by a provided parameter.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameFilter);
            var filter = inputParameters.DecodeParameter<string>(ParameterNameFilter, defaultValue: string.Empty);
            var count = inputParameters.DecodeParameter<int>(ParameterNameCount, defaultValue: CountDefaultValue);
            return await AzureClient.GetJobListAsync(channel, filter, count, cancellationToken);
        }
    }
}