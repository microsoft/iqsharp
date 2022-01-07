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
    ///     A magic command that can be used to connect to an Azure workspace.
    /// </summary>
    public class StatusMagic : AzureClientMagicBase
    {
        private const string ParameterNameJobId = "id";

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        /// <param name="logger">Logger instance for messages.</param>
        public StatusMagic(IAzureClient azureClient, ILogger<StatusMagic> logger)
            : base(
                azureClient,
                "azure.status",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Displays status for a job in the current Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for displaying status for a job in the current 
                        Azure Quantum workspace.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command]({KnownUris.ReferenceForMagicCommand("azure.connect")}).
                        
                        #### Optional parameters

                        - The job ID for which to display status. If not specified, the job ID from
                        the most recent call to [`%azure.submit`]({KnownUris.ReferenceForMagicCommand("azure.submit")})
                        or [`%azure.execute`]({KnownUris.ReferenceForMagicCommand("azure.execute")}) will be used.
                        
                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                        - {AzureClientError.JobNotFound.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Get the status of a specific job:
                            ```
                            In []: %azure.status JOB_ID
                            Out[]: <detailed status of specified job>
                            ```
                        ".Dedent(),

                        @"
                            Get the status of the most recently submitted job:
                            ```
                            In []: %azure.status
                            Out[]: <detailed status of most recently submitted job>
                            ```
                        ".Dedent(),
                    },
                },
                logger) {}

        /// <summary>
        ///     Displays the status corresponding to a given job ID, if provided,
        ///     or the most recently-submitted job in the current session.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameJobId);
            string jobId = inputParameters.DecodeParameter<string>(ParameterNameJobId);
            return await AzureClient.GetJobStatusAsync(channel, jobId, cancellationToken);
        }
    }
}