// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    ///     A magic command that can be used to view or set target information for an Azure Quantum workspace.
    /// </summary>
    public class TargetMagic : AzureClientMagicBase
    {
        /// <summary>
        ///     Constructs a new magic command given an IAzureClient object.
        /// </summary>
        public TargetMagic(IAzureClient azureClient) :
            base(azureClient,
                "target",
                new Documentation
                {
                    Summary = "Views or sets the target for job submission to an Azure Quantum workspace.",
                    Description = @"
                        This magic command allows for specifying a target for job submission
                        to an Azure Quantum workspace, or viewing the list of all available targets.

                        The Azure Quantum workspace must previously have been initialized
                        using the %connect magic command, and the specified target must be
                        available in the workspace.   
                    ".Dedent(),
                    Examples = new[]
                    {
                        @"
                            Set the current target for job submission:
                            ```
                            In []: %target TARGET_NAME
                            Out[]: Active target is now TARGET_NAME
                            ```
                        ".Dedent(),
                        @"
                            View the current target and all available targets in the current Azure Quantum workspace:
                            ```
                            In []: %target
                            Out[]: <list of available targets>
                            ```
                        ".Dedent(),
                    }
                }) {}

        /// <summary>
        ///     Sets or views the target for job submission to the current Azure Quantum workspace.
        /// </summary>
        public override async Task<ExecutionResult> RunAsync(string input, IChannel channel)
        {
            Dictionary<string, string> keyValuePairs = this.ParseInput(input);
            if (keyValuePairs.Keys.Count > 0)
            {
                var targetName = keyValuePairs.Keys.First();
                return await AzureClient.SetActiveTargetAsync(channel, targetName);
            }

            return await AzureClient.PrintTargetListAsync(channel);
        }
    }
}