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
    ///     A magic command that can be used to connect to display the results of an Azure Quantum job.
    /// </summary>
    public class OutputMagic : AzureClientMagicBase
    {
        private const string
            ParameterNameJobId = "jobId";

        /// <summary>
        ///     Constructs a new magic command given an IAzureClient object.
        /// </summary>
        public OutputMagic(IAzureClient azureClient) :
            base(azureClient,
                "output",
                new Documentation
                {
                    Summary = "Displays results for jobs in the current Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for displaying the results of jobs in the current 
                        Azure Quantum workspace. If a valid job ID is provided as an argument, and the
                        job has completed, the output of that job will be displayed. If no job ID is
                        provided, the job ID from the most recent call to `%aq submit` will be used.
                        
                        If the job has not yet completed, an error message will be displayed.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Print results of a specific job:
                            ```
                            In []: %output JOB_ID
                            Out[]: <job results of specified job>
                            ```
                        ".Dedent(),

                        @"
                            Print results of the most recently-submitted job:
                            ```
                            In []: %output
                            Out[]: <job results of most recently-submitted job>
                            ```
                        ".Dedent()
                    }
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