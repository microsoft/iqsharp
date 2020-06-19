// Copyright (c) Microsoft Corporation. All rights reserved.
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
                    Summary = "Views or sets the target for job submission to an Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for specifying a target for job submission
                        to an Azure Quantum workspace, or viewing the list of all available targets.

                        The Azure Quantum workspace must previously have been initialized
                        using the %azure.connect magic command, and the specified target must be
                        available in the workspace.   
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Set the current target for job submission:
                            ```
                            In []: %azure.target TARGET_ID
                            Out[]: Active target is now TARGET_ID
                            ```
                        ".Dedent(),
                        @"
                            View the current target and all available targets in the current Azure Quantum workspace:
                            ```
                            In []: %azure.target
                            Out[]: <current target and list of available targets>
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