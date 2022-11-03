// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Threading;
using Microsoft.Extensions.Logging;

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
        /// <param name="logger">Logger instance for messages.</param>
        public SubmitMagic(IAzureClient azureClient, ILogger<SubmitMagic> logger)
            : base(
                azureClient,
                "azure.submit",
                new Microsoft.Jupyter.Core.Documentation
                {
                    Summary = "Submits a job to an Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for submitting a Q# operation or function
                        to be run on the specified target in the current Azure Quantum workspace.
                        The command returns immediately after the job is submitted.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command]({KnownUris.ReferenceForMagicCommand("azure.connect")}),
                        and an execution target for the job must have been specified using the
                        [`%azure.target` magic command]({KnownUris.ReferenceForMagicCommand("azure.target")}).

                        #### Required parameters

                        - Q# operation or function name. This must be the first parameter, and must be a valid Q# operation
                        or function name that has been defined either in the notebook or in a Q# file in the same folder.
                        - Arguments for the Q# operation or function must also be specified as `key=value` pairs.

                        #### Optional parameters

                        - `{AzureSubmissionContext.ParameterNameJobName}=<string>`: Friendly name to identify this job. If not specified,
                        the Q# operation or function name will be used as the job name.
                        - `{AzureSubmissionContext.ParameterNameJobParams}=<JSON key:value pairs>`: Provider-specific job parameters
                        expressed in JSON as one or more `key`:`value` pairs to be passed to the execution target. Values must be strings.
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
                            specifying a custom job name, number of shots, and provider-specific job parameters:
                            ```
                            In []: %azure.submit MyOperation a=5 b=10 jobName=""My job"" shots=100 jobParams={""Key1"":""Val1"",""Key2"":""Val2""}
                            Out[]: Submitting MyOperation to target provider.qpu...
                                   Job successfully submitted for 100 shots.
                                      Job name: My job
                                      Job ID: <Azure Quantum job ID>
                                   <detailed properties of submitted job>
                            ```
                        ".Dedent(),
                    },
                },
                logger)
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
