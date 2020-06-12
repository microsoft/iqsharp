// Copyright (c) Microsoft Corporation.
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
        public StatusMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.status",
                new Documentation
                {
                    Summary = "Displays status for jobs in the current Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for displaying status of jobs in the current 
                        Azure Quantum workspace. If a valid job ID is provided as an argument, the
                        detailed status of that job will be displayed. If no job ID is
                        provided, the job ID from the most recent call to `%azure.submit` or
                        `%azure.execute` will be used.

                        The Azure Quantum workspace must previously have been initialized
                        using the %azure.connect magic command.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Print status of a specific job:
                            ```
                            In []: %azure.status JOB_ID
                            Out[]: <job status of specified job>
                            ```
                        ".Dedent(),

                        @"
                            Print status of the most recently-submitted job:
                            ```
                            In []: %azure.status
                            Out[]: <job status of most recently-submitted job>
                            ```
                        ".Dedent(),
                    },
                }) {}

        /// <summary>
        ///     Displays the status corresponding to a given job ID, if provided,
        ///     or the most recently-submitted job in the current session.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameJobId);
            string jobId = inputParameters.DecodeParameter<string>(ParameterNameJobId);
            return await AzureClient.GetJobStatusAsync(channel, jobId);
        }
    }
}