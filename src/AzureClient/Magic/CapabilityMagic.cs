// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp.AzureClient;

/// <summary>
///     A magic command that can be used to view or set target information for an Azure Quantum workspace.
/// </summary>
public class CapabilityMagic : AzureClientMagicBase
{
    private const string ParameterNameTargetCapability = "capability";

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityMagic"/> class.
    /// </summary>
    /// <param name="azureClient">
    /// The <see cref="IAzureClient"/> object to use for Azure functionality.
    /// </param>
    /// <param name="logger">Logger instance for messages.</param>
    /// <param name="controller">
    ///     Metadata controller used to identify Python versus standalone
    ///     clients. If <c>null</c>, standalone notebooks are assumed.
    /// </param>
    public CapabilityMagic(IAzureClient azureClient, ILogger<TargetMagic> logger, IMetadataController? controller = null)
        : base(
            azureClient,
            "azure.target-capability",
            new Microsoft.Jupyter.Core.Documentation
            {
                Summary = "Sets or displays the active target capability level for compiling Q# functions and operations.",
                Description = $@"
                    This magic command allows for specifying or displaying the active target capability level for Q# job submission
                    in an Azure Quantum workspace.

                    #### Optional parameters

                    - The target ID to set as the active execution target for Q# job submission. If not specified,
                    the currently active execution target is displayed.
                ".Dedent(),
                Examples = new string[]
                {
                    @"
                        Sets the current target capability to full computation
                        (no restrictions), if allowed by the current execution
                        target.
                        ```
                        In []: %azure.target-capability FullComputation
                        ```
                    ".Dedent(),
                    @"
                        Displays the current target capability level.
                        ```
                        In []: %azure.target-capability
                        ```
                    ".Dedent(),
                    @"
                        Resets the current target capability level to either
                        the maximum supported by the current execution target,
                        or to full computation if no execution target is
                        currently set.
                        ```
                        In []: %azure.target-capability --clear
                        ```
                    ".Dedent()
                },
            },
            logger)
    { }

    /// <summary>
    ///     Sets or views the current target capability.
    /// </summary>
    public override Task<ExecutionResult> RunAsync(string input, IChannel channel, CancellationToken cancellationToken)
    {
        var inputParameters = ParseInputParameters(input, firstParameterInferredName: ParameterNameTargetCapability);
        if (inputParameters.ContainsKey(ParameterNameTargetCapability))
        {
            if (inputParameters.DecodeParameter<string>(ParameterNameTargetCapability) is {} capabilityName)
            {
                if (AzureClient.TrySetTargetCapability(channel, capabilityName.Trim() == "--clear" ? null : capabilityName, out var capability))
                {
                    return Task.FromResult(capability.ToExecutionResult());
                }
                else
                {
                    return Task.FromResult(ExecuteStatus.Error.ToExecutionResult());
                }
            }
            else
            {
                channel.Stderr("Expected string for target capability name.");
                return Task.FromResult(ExecuteStatus.Error.ToExecutionResult());
            }
        }

        return Task.FromResult(AzureClient.TargetCapability.ToExecutionResult());
    }
}
