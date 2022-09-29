// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Quantum;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.Runtime.Submitters;

namespace Microsoft.Quantum.IQSharp.AzureClient;

internal enum AzureProvider
{
    Microsoft,
    IonQ,
    Quantinuum,
    // NB: This provider name is deprecated, but may exist in older
    //     workspaces and should still be supported.
    Honeywell,
    QCI,
    Rigetti,
    Mock
}

/// <summary>
///     An execution target for Q# jobs on Azure Quantum.
/// </summary>
public record AzureExecutionTarget
{
    /// <summary>
    ///     Constructs an execution target from its target ID.
    /// </summary>
    internal protected AzureExecutionTarget(string? targetId)
    {
        this.TargetId = targetId ?? string.Empty;
    }

    /// <summary>
    ///      A short string used to identify the target.
    /// </summary>
    public string? TargetId { get; }

    /// <summary>
    ///     The name of the NuGet package required to compile against this
    ///     target.
    /// </summary>
    public virtual string PackageName => GetProvider(TargetId) switch
    {
        // TODO (cesarzc): Revert this.
        //AzureProvider.IonQ       => "Microsoft.Quantum.Providers.IonQ",
        AzureProvider.IonQ       => "Microsoft.Quantum.Providers.QCI",
        AzureProvider.Quantinuum => "Microsoft.Quantum.Providers.Honeywell",
        AzureProvider.Honeywell  => "Microsoft.Quantum.Providers.Honeywell",
        AzureProvider.QCI        => "Microsoft.Quantum.Providers.QCI",
        AzureProvider.Microsoft  => "Microsoft.Quantum.Providers.Core",
        _                        => $"Microsoft.Quantum.Providers.{GetProvider(TargetId)}"
    };

    /// <summary>
    ///     Returns the maximum level of capability supported by this target.
    ///     Any other target capability must be subsumed by this maximum
    ///     in order to be supported by this target.
    /// </summary>
    public TargetCapability DefaultCapability => GetProvider(TargetId) switch
    {
        AzureProvider.IonQ       => TargetCapabilityModule.BasicQuantumFunctionality,
        AzureProvider.Quantinuum => TargetCapabilityModule.BasicMeasurementFeedback,
        AzureProvider.Honeywell  => TargetCapabilityModule.BasicMeasurementFeedback,
        AzureProvider.QCI        => TargetCapabilityModule.AdaptiveExecution,
        AzureProvider.Rigetti    => TargetCapabilityModule.BasicExecution,
        AzureProvider.Microsoft  => TargetCapabilityModule.FullComputation,
        _                        => TargetCapabilityModule.FullComputation,
    };

    /// <summary>
    ///     Returns <c>true</c> if this target supports a given capability
    ///     level.
    /// </summary>
    public bool SupportsCapability(TargetCapability capability) =>
        // NB: Duplicates logic at https://github.com/microsoft/qsharp-compiler/blob/7714168fda4379fb7e6a6c616f680ec039c482f4/src/QuantumSdk/DefaultItems/DefaultItems.targets#L78,
        //     but at the level of providers rather than at the level of resolved processors.
        (GetProvider(TargetId) switch
        {
            AzureProvider.Quantinuum or AzureProvider.Honeywell => new[]
            {
                TargetCapabilityModule.AdaptiveExecution,
                TargetCapabilityModule.BasicMeasurementFeedback,
                TargetCapabilityModule.BasicQuantumFunctionality,
                TargetCapabilityModule.BasicExecution
            },
            AzureProvider.IonQ => new[]
            {
                TargetCapabilityModule.BasicQuantumFunctionality
            },
            AzureProvider.QCI => new[]
            {
                TargetCapabilityModule.AdaptiveExecution,
                TargetCapabilityModule.BasicExecution
            },
            AzureProvider.Rigetti => new[]
            {
                TargetCapabilityModule.BasicExecution
            },
            AzureProvider.Microsoft => new[]
            {
                TargetCapabilityModule.FullComputation
            },
            _ => new[]
            {
                TargetCapabilityModule.FullComputation,
                TargetCapabilityModule.AdaptiveExecution,
                TargetCapabilityModule.BasicMeasurementFeedback,
                TargetCapabilityModule.BasicQuantumFunctionality,
                TargetCapabilityModule.BasicExecution
            }
        })
        .Any(c => TargetCapabilityModule.Subsumes(c, capability));

    /// <summary>
    ///      Attempts to get a <see cref="IQirSubmitter"/> instance appropriate
    ///      for use with this target.
    /// </summary>
    public virtual bool TryGetQirSubmitter(
        Azure.Quantum.IWorkspace workspace,
        string storageConnectionString,
        TargetCapability? targetCapability,
        [NotNullWhen(true)] out IQirSubmitter? submitter
    )
    {
        if (this.TargetId == null || this.TargetId.EndsWith(".mock"))
        {
            submitter = null;
            return false;
        }

        // TODO (cesarzc): Remove hard-coded creation of submitter.
        if (SubmitterFactory.QirSubmitter("qci.simulator", workspace, storageConnectionString, "AdaptiveExecution") is IQirSubmitter hardcodedSubmitter)
        {
            submitter = hardcodedSubmitter;
            return true;
        }
        if (SubmitterFactory.QirSubmitter(this.TargetId, workspace, storageConnectionString, (targetCapability ?? this.DefaultCapability).Name) is IQirSubmitter qirSubmitter)
        {
            submitter = qirSubmitter;
            return true;
        }
        else
        {
            submitter = null;
            return false;
        }
    }

    /// <summary>
    /// Returns true, if the provider for the given target is a known provider 
    /// capable of running Q# applications.
    /// </summary>
    public static bool IsValid(TargetStatusInfo target) => IsValid(target?.TargetId);

    /// <summary>
    /// Returns true, if the provider for the given target is a known provider 
    /// capable of running Q# applications.
    /// </summary>
    public static bool IsValid(string? targetId) => GetProvider(targetId) != null;

    /// <summary>
    /// It creates the <see cref="AzureExecutionTarget" /> instance for the given TargetStatusInfo. If the TargetStatusInfo
    /// is from a Mock, then it returns a Mock AzureExecutionTarget, otherwise it creates the instance 
    /// based on the corresponding target id.
    /// </summary>
    internal static AzureExecutionTarget? Create(TargetStatusInfo target) => target is MockTargetStatus
        ? MockAzureExecutionTarget.CreateMock(target)
        : Create(target?.TargetId);


    /// <summary>
    /// It creates the AzureExecutionTarget instance for the given targetId.
    /// </summary>
    /// <returns>
    ///     An instance of <see cref="AzureExecutionTarget"/> if
    ///     <param name="targetId" /> describes a target for a valid
    ///     provider, and <c>null</c> otherwise.
    /// </returns>
    internal static AzureExecutionTarget? Create(string? targetId) =>
        GetProvider(targetId) is null
        ? null
        : new AzureExecutionTarget(targetId);


    /// <summary>
    ///     Gets the Azure Quantum provider corresponding to the given execution target.
    /// </summary>
    /// <param name="targetId">The Azure Quantum execution target ID.</param>
    /// <returns>
    ///     The <see cref="AzureProvider"/> enum value representing the
    ///     provider, or <c>null</c> if <paramref name="targetId"/> does
    ///     not describe a valid provider.
    /// </returns>
    /// <remarks>
    ///     Valid target IDs are structured as "provider.target".
    ///     For example, "ionq.simulator" or "quantinuum.qpu".
    /// </remarks>
    internal static AzureProvider? GetProvider(string? targetId)
    {
        if (targetId == null)
        {
            return null;
        }

        var parts = targetId.Split('.', 2);
        if (Enum.TryParse(parts[0], true, out AzureProvider provider))
        {
            return provider;
        }

        return null;
    }

}
