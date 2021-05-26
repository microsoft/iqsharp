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
    ///     A magic command that can be used to connect to display the results of an Azure Quantum job.
    /// </summary>
    public class OutputMagic : AzureClientMagicBase
    {
        private const string ParameterNameJobId = "id";

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public OutputMagic(IAzureClient azureClient, ILogger<OutputMagic> logger)
            : base(
                azureClient,
                "azure.output",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Displays results for a job in the current Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for displaying results for a job in the current 
                        Azure Quantum workspace.
                        The job execution must already be completed in order to display
                        results.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.connect).
                        
                        #### Optional parameters

                        - The job ID for which to display results. If not specified, the job ID from
                        the most recent call to [`%azure.submit`](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.submit)
                        or [`%azure.execute`](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.execute) will be used.
                        
                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                        - {AzureClientError.JobNotFound.ToMarkdown()}
                        - {AzureClientError.JobNotCompleted.ToMarkdown()}
                        - {AzureClientError.JobOutputDownloadFailed.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Get results of a specific job:
                            ```
                            In []: %azure.output JOB_ID
                            Out[]: <detailed results of specified job>
                            ```
                        ".Dedent(),

                        @"
                            Get results of the most recently submitted job:
                            ```
                            In []: %azure.output
                            Out[]: <detailed results of most recently submitted job>
                            ```
                        ".Dedent(),
                    },
                }, logger) {}

        /// <summary>
        ///     Displays the output of a given completed job ID, if provided,
        ///     or all jobs submitted in the current session.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameJobId);
            string jobId = inputParameters.DecodeParameter<string>(ParameterNameJobId);
            return await AzureClient.GetJobResultAsync(channel, jobId, cancellationToken);
        }
    }
}