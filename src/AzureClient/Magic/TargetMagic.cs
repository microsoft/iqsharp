// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to view or set target information for an Azure Quantum workspace.
    /// </summary>
    public class TargetMagic : AzureClientMagicBase
    {
        private const string ParameterNameTargetId = "id";

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        public TargetMagic(IAzureClient azureClient)
            : base(
                azureClient,
                "azure.target",
                new Documentation
                {
                    Summary = "Sets or displays the active execution target for Q# job submission in an Azure Quantum workspace.",
                    Description = $@"
                        This magic command allows for specifying or displaying the execution target for Q# job submission
                        in an Azure Quantum workspace.

                        The Azure Quantum workspace must have been previously initialized
                        using the [`%azure.connect` magic command](https://docs.microsoft.com/qsharp/api/iqsharp-magic/azure.connect)
                        magic command. The specified execution target must be available in the workspace and support execution of Q# programs.

                        #### Optional parameters

                        - The target ID to set as the active execution target for Q# job submission. If not specified,
                        the currently active execution target is displayed.

                        #### Possible errors

                        - {AzureClientError.NotConnected.ToMarkdown()}
                        - {AzureClientError.InvalidTarget.ToMarkdown()}
                        - {AzureClientError.NoTarget.ToMarkdown()}
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Set the current target for Q# job submission to `provider.qpu`:
                            ```
                            In []: %azure.target provider.qpu
                            Out[]: Loading package Microsoft.Quantum.Providers.Provider and dependencies...
                                   Active target is now provider.qpu
                                   <detailed properties of active execution target>
                            ```
                        ".Dedent(),
                        @"
                            Display the current target and all available targets in the current Azure Quantum workspace:
                            ```
                            In []: %azure.target
                            Out[]: Current execution target: provider.qpu
                                   Available execution targets: provider.qpu, provider.simulator
                                   <detailed properties of active execution target>
                            ```
                        ".Dedent(),
                    },
                })
        { }

        /// <summary>
        ///     Sets or views the target for job submission to the current Azure Quantum workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameTargetId);
            if (inputParameters.ContainsKey(ParameterNameTargetId))
            {
                string targetId = inputParameters.DecodeParameter<string>(ParameterNameTargetId);
                return await AzureClient.SetActiveTargetAsync(channel, targetId);
            }

            return await AzureClient.GetActiveTargetAsync(channel);
        }
    }
}