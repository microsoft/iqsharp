// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp.AzureClient;

/// <summary>
///     A magic command that can be used to view or set target information for an Azure Quantum workspace.
/// </summary>
public class CapabilityMAgic : AzureClientMagicBase
{
    private const string ParameterNameTargetCapability = "capability";

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityMAgic"/> class.
    /// </summary>
    /// <param name="azureClient">
    /// The <see cref="IAzureClient"/> object to use for Azure functionality.
    /// </param>
    /// <param name="logger">Logger instance for messages.</param>
    /// <param name="controller">
    ///     Metadata controller used to identify Python versus standalone
    ///     clients. If <c>null</c>, standalone notebooks are assumed.
    /// </param>
    public CapabilityMAgic(IAzureClient azureClient, ILogger<TargetMagic> logger, IMetadataController? controller = null)
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
                    // TODO
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
            var capabilityName = inputParameters.DecodeParameter<string>(ParameterNameTargetCapability);
            // TODO: Handle "--clear" here.
            if (AzureClient.TrySetTargetCapability(channel, capabilityName, out var capability))
            {
                return Task.FromResult(capability.ToExecutionResult());
            }
            else
            {
                return Task.FromResult(ExecuteStatus.Error.ToExecutionResult());
            }
        }

        return Task.FromResult(AzureClient.TargetCapability.ToExecutionResult());
    }
}
