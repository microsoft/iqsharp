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
    ///     A magic command that can be used to connect to an Azure workspace.
    /// </summary>
    public class StatusMagic : AzureClientMagicBase
    {
        private const string
            ParameterNameJobId = "jobId";

        /// <summary>
        ///     Constructs a new magic command given an IAzureClient object.
        /// </summary>
        public StatusMagic(IAzureClient azureClient) :
            base(azureClient,
                "status",
                new Documentation
                {
                    Summary = "Displays status for jobs in the current Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for displaying status of jobs in the current 
                        Azure Quantum workspace. If a valid job ID is provided as an argument, the
                        detailed status of that job will be displayed; otherwise, a list of all jobs
                        created in the current session will be displayed.
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Print status about a specific job:
                            ```
                            In []: %status JOB_ID
                            Out[]: JOB_ID: <job status>
                            ```
                        ".Dedent(),

                        @"
                            Print status about all jobs created in the current session:
                            ```
                            In []: %status
                            Out[]: <status for each job>
                            ```
                        ".Dedent()
                    }
                }) {}

        /// <summary>
        ///     Displays the status corresponding to a given job ID, if provided,
        ///     or all jobs in the active workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameJobId);
            if (inputParameters.ContainsKey(ParameterNameJobId))
            {
                string jobId = inputParameters.DecodeParameter<string>(ParameterNameJobId);
                return await AzureClient.PrintJobStatusAsync(channel, jobId);
            }

            return await AzureClient.PrintJobListAsync(channel);
        }
    }
}