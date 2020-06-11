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
    ///     A magic command that can be used to connect to display the results of an Azure Quantum job.
    /// </summary>
    public class OutputMagic : AzureClientMagicBase
    {
        private const string ParameterNameJobId = "jobId";

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public OutputMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.output",
                new Documentation
                {
                    Summary = "Displays results for jobs in the current Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for displaying results of jobs in the current 
                        Azure Quantum workspace. If a valid job ID is provided as an argument, and the
                        job has completed, the output of that job will be displayed. If no job ID is
                        provided, the job ID from the most recent call to `%azure.submit` or
                        `%azure.execute` will be used.
                        
                        If the job has not yet completed, an error message will be displayed.

                        The Azure Quantum workspace must previously have been initialized
                        using the %azure.connect magic command.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Print results of a specific job:
                            ```
                            In []: %azure.output JOB_ID
                            Out[]: <job results of specified job>
                            ```
                        ".Dedent(),

                        @"
                            Print results of the most recently-submitted job:
                            ```
                            In []: %azure.output
                            Out[]: <job results of most recently-submitted job>
                            ```
                        ".Dedent(),
                    },
                }) {}

        /// <summary>
        ///     Displays the output of a given completed job ID, if provided,
        ///     or all jobs submitted in the current session.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameJobId);
            string jobId = inputParameters.DecodeParameter<string>(ParameterNameJobId);
            return await AzureClient.GetJobResultAsync(channel, jobId);
        }
    }
}