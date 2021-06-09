// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to submit jobs to an Azure Quantum workspace.
    /// </summary>
    public class ExecuteMagic : AzureClientMagicBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        /// <param name="logger">Logger instance for messages.</param>
        public ExecuteMagic(IAzureClient azureClient, ILogger<ExecuteMagic> logger)
            : base(
                azureClient,
                "azure.execute",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Submits a job to an Azure Quantum workspace and waits for completion.",
                    Description = $@"
                        This magic command allows for submitting a Q# operation or function
                        to be run on the specified target in the current Azure Quantum workspace.
                        The command waits a specified amount of time for the job to complete before returning.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.connect),
                        and an execution target for the job must have been specified using the
                        [`%azure.target` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.target).

                        #### Required parameters

                        - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                        or function name that has been defined either in the notebook or in a Q# file in the same folder.
                        - Arguments for the Q# operation or function must also be specified as `key=value` pairs.
                        
                        #### Optional parameters

                        - `{AzureSubmissionContext.ParameterNameJobName}=<string>`: Friendly name to identify this job. If not specified,
                        the Q# operation or function name will be used as the job name.
                        - `{AzureSubmissionContext.ParameterNameShots}=<integer>` (default=500): Number of times to repeat execution of the
                        specified Q# operation or function.
                        - `{AzureSubmissionContext.ParameterNameTimeout}=<integer>` (default=30): Time to wait (in seconds) for job completion
                        before the magic command returns.
                        - `{AzureSubmissionContext.ParameterNamePollingInterval}=<integer>` (default=5): Interval (in seconds) to poll for
                        job status while waiting for job execution to complete.
                        
                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                        - {AzureClientError.NoTarget.ToMarkdown()}
                        - {AzureClientError.NoOperationName.ToMarkdown()}
                        - {AzureClientError.InvalidTarget.ToMarkdown()}
                        - {AzureClientError.UnrecognizedOperationName.ToMarkdown()}
                        - {AzureClientError.InvalidEntryPoint.ToMarkdown()}
                        - {AzureClientError.JobSubmissionFailed.ToMarkdown()}
                        - {AzureClientError.JobNotCompleted.ToMarkdown()}
                        - {AzureClientError.JobOutputDownloadFailed.ToMarkdown()}
                        - {AzureClientError.JobFailedOrCancelled.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Run a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`
                            on the active target in the current Azure Quantum workspace:
                            ```
                            In []: %azure.execute MyOperation a=5 b=10
                            Out[]: Submitting MyOperation to target provider.qpu...
                                   Job successfully submitted for 500 shots.
                                      Job name: MyOperation
                                      Job ID: <Azure Quantum job ID>
                                   Waiting up to 30 seconds for Azure Quantum job to complete...
                                   [1:23:45 PM] Current job status: Waiting
                                   [1:23:50 PM] Current job status: Executing
                                   [1:23:55 PM] Current job status: Succeeded
                                   <detailed results of completed job>
                            ```
                        ".Dedent(),
                        @"
                            Run a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`
                            on the active target in the current Azure Quantum workspace,
                            specifying a custom job name, number of shots, timeout, and polling interval:
                            ```
                            In []: %azure.submit MyOperation a=5 b=10 jobName=""My job"" shots=100 timeout=60 poll=10
                            Out[]: Submitting MyOperation to target provider.qpu...
                                   Job successfully submitted for 100 shots.
                                      Job name: My job
                                      Job ID: <Azure Quantum job ID>
                                   Waiting up to 60 seconds for Azure Quantum job to complete...
                                   [1:23:45 PM] Current job status: Waiting
                                   [1:23:55 PM] Current job status: Waiting
                                   [1:24:05 PM] Current job status: Executing
                                   [1:24:15 PM] Current job status: Succeeded
                                   <detailed results of completed job>
                            ```
                        ".Dedent(),
                    },
                }, logger)
        { }

        /// <summary>
        ///     Executes a new job in an Azure Quantum workspace given a Q# operation
        ///     name that is present in the current Q# Jupyter workspace, and
        ///     waits for the job to complete before returning.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            return await AzureClient.ExecuteJobAsync(channel, AzureSubmissionContext.Parse(input), cancellationToken);
        }
    }
}
