// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to submit jobs to an Azure Quantum workspace.
    /// </summary>
    public class SubmitMagic : AzureClientMagicBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public SubmitMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.submit",
                new Documentation
                {
                    Summary = "Submits a job to an Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for submitting a Q# operation or function
                        for execution on the specified target in the current Azure Quantum workspace.
                        The command returns immediately after the job is submitted.

                        The Azure Quantum workspace must have been previously initialized
                        using the `%azure.connect` magic command, and an execution target for the job
                        must have been specified using the `%azure.target` magic command.

                        #### Required parameters

                        - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                        or function name that has been defined either in the notebook or in a Q# file in the same folder.
                        - Arguments for the Q# operation or function must also be specified as `key=value` pairs.

                        #### Optional parameters

                        - `{AzureSubmissionContext.ParameterNameJobName}=<string>`: Friendly name to identify this job. If not specified,
                        the Q# operation or function name will be used as the job name.
                        - `{AzureSubmissionContext.ParameterNameShots}=<integer>` (default=500): Number of times to repeat execution of the
                        specified Q# operation or function.
                        
                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                        - {AzureClientError.NoTarget.ToMarkdown()}
                        - {AzureClientError.NoOperationName.ToMarkdown()}
                        - {AzureClientError.InvalidTarget.ToMarkdown()}
                        - {AzureClientError.UnrecognizedOperationName.ToMarkdown()}
                        - {AzureClientError.InvalidEntryPoint.ToMarkdown()}
                        - {AzureClientError.JobSubmissionFailed.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Submit a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`
                            for execution on the active target in the current Azure Quantum workspace:
                            ```
                            In []: %azure.submit MyOperation a=5 b=10
                            Out[]: Submitting MyOperation to target provider.qpu...
                                   Job successfully submitted for 500 shots.
                                      Job name: MyOperation
                                      Job ID: <Azure Quantum job ID>
                                   <detailed properties of submitted job>
                            ```
                        ".Dedent(),
                        @"
                            Submit a Q# operation defined as `operation MyOperation(a : Int, b : Int) : Result`
                            for execution on the active target in the current Azure Quantum workspace,
                            specifying a custom job name, number of shots, timeout, and polling interval:
                            ```
                            In []: %azure.submit MyOperation a=5 b=10 jobName=""My job"" shots=100
                            Out[]: Submitting MyOperation to target provider.qpu...
                                   Job successfully submitted for 100 shots.
                                      Job name: My job
                                      Job ID: <Azure Quantum job ID>
                                   <detailed properties of submitted job>
                            ```
                        ".Dedent(),
                    },
                })
        { }

        /// <summary>
        ///     Submits a new job to an Azure Quantum workspace given a Q# operation
        ///     name that is present in the current Q# Jupyter workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            return await AzureClient.SubmitJobAsync(channel, AzureSubmissionContext.Parse(input), cancellationToken);
        }
    }
}