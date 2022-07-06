﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp.AzureClient;

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
    /// <param name="logger">Logger instance for messages.</param>
    /// <param name="controller">
    ///     Metadata controller used to identify Python versus standalone
    ///     clients. If <c>null</c>, standalone notebooks are assumed.
    /// </param>
    public TargetMagic(IAzureClient azureClient, ILogger<TargetMagic> logger, IMetadataController? controller = null)
        : base(
            azureClient,
            "azure.target",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Sets or displays the active execution target for Q# job submission in an Azure Quantum workspace.",
                Description = $@"
                    This magic command allows for specifying or displaying the execution target for Q# job submission
                    in an Azure Quantum workspace.

                    The Azure Quantum workspace must have been previously initialized
                    using the [`%azure.connect` magic command]({KnownUris.ReferenceForMagicCommand("azure.connect")})
                    magic command. The specified execution target must be available in the workspace and support execution of Q# programs.

                    #### Optional parameters

                    - The target ID to set as the active execution target for Q# job submission. If not specified,
                    the currently active execution target is displayed.

                    #### Possible errors

                    - {AzureClientError.NotConnected.ToMarkdown()}
                    - {AzureClientError.InvalidTarget.ToMarkdown()}
                    - {AzureClientError.NoTarget.ToMarkdown()}

                    #### Target capabilities

                    When setting a target, the target capability is set to
                    the maximum capability level supported by the given
                    target, such that all capabilities allowed by the target
                    are allowed in subsequent Q# compilation functions and
                    operations.

                    You can restrict target capability levels
                    further by using
                    `{controller.CommandDisplayName("target-capability")}`.
                    This may be useful, for instance, when comparing
                    functionality between different targets.
                ".Dedent(),
                Examples = new[]
                {
                    
                    @"
                        Sets the current target for Q# job submission to `provider.qpu`:
                        ```
                        In []: %azure.target provider.qpu
                        Out[]: Loading package Microsoft.Quantum.Providers.Provider and dependencies...
                                Active target is now provider.qpu
                                <detailed properties of active execution target>
                        ```
                    ".Dedent(),
                    @"
                        Clears the current target information:
                        ```
                        In []: %azure.target --clear
                        ```
                    ".Dedent(),
                    @"
                        Displays the current target and all available targets in the current Azure Quantum workspace:
                        ```
                        In []: %azure.target
                        Out[]: Current execution target: provider.qpu
                                Available execution targets: provider.qpu, provider.simulator
                                <detailed properties of active execution target>
                        ```
                    ".Dedent(),
                },
            },
            logger)
    { }

    /// <summary>
    ///     Sets or views the target for job submission to the current Azure Quantum workspace.
    /// </summary>
    public override async Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
    {
        var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameTargetId);
        if (inputParameters.ContainsKey(ParameterNameTargetId))
        {
            var targetId = inputParameters.DecodeParameter<string>(ParameterNameTargetId);
            if (targetId.Trim() == "--clear")
            {
                AzureClient.ClearActiveTarget();
                return ExecuteStatus.Ok.ToExecutionResult();
            }
            return await AzureClient.SetActiveTargetAsync(channel, targetId, cancellationToken);
        }

        return await AzureClient.GetActiveTargetAsync(channel, cancellationToken);
    }
}
