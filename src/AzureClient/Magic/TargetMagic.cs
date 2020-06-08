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
    ///     A magic command that can be used to view or set target information for an Azure Quantum workspace.
    /// </summary>
    public class TargetMagic : AzureClientMagicBase
    {
        private const string ParameterNameTargetName = "name";

        private IReferences? References { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetMagic"/> class.
        /// </summary>
        /// <param name="azureClient">
        /// The <see cref="IAzureClient"/> object to use for Azure functionality.
        /// </param>
        /// <param name="references">
        /// The <see cref="IReferences"/> object to use for loading target-specific packages.
        /// </param>
        public TargetMagic(IAzureClient azureClient, IReferences references)
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
                            In []: %azure.target TARGET_NAME
                            Out[]: Active target is now TARGET_NAME
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
                }) =>
            References = references;

        /// <summary>
        ///     Sets or views the target for job submission to the current Azure Quantum workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameTargetName);
            if (inputParameters.ContainsKey(ParameterNameTargetName))
            {
                string targetName = inputParameters.DecodeParameter<string>(ParameterNameTargetName);
                return await AzureClient.SetActiveTargetAsync(channel, References, targetName);
            }

            return await AzureClient.GetActiveTargetAsync(channel);
        }
    }
}