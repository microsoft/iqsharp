// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Quantum;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.Runtime.Submitters;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureProvider
    {
        IonQ,
        Quantinuum,
        // NB: This provider name is deprecated, but may exist in older
        //     workspaces and should still be supported.
        Honeywell,
        QCI,
        Microsoft,
        Mock
    }

    public record AzureExecutionTarget
    {
        internal protected AzureExecutionTarget(string? targetId)
        {
            this.TargetId = targetId ?? string.Empty;
        }

        public string? TargetId { get; }

        public virtual string PackageName => GetProvider(TargetId) switch
        {
            
            AzureProvider.IonQ       => "Microsoft.Quantum.Providers.IonQ",
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
        public TargetCapability MaximumCapability => GetProvider(TargetId) switch
        {
            AzureProvider.IonQ       => TargetCapabilityModule.BasicQuantumFunctionality,
            AzureProvider.Quantinuum => TargetCapabilityModule.BasicMeasurementFeedback,
            AzureProvider.Honeywell  => TargetCapabilityModule.BasicMeasurementFeedback,
            AzureProvider.QCI        => TargetCapabilityModule.BasicMeasurementFeedback,
            AzureProvider.Microsoft  => TargetCapabilityModule.FullComputation,
            _                        => TargetCapabilityModule.FullComputation,
        };

        public bool SupportsCapability(TargetCapability capability) =>
            TargetCapabilityModule.Subsumes(this.MaximumCapability, capability);

        public virtual bool TryGetQirSubmitter(Azure.Quantum.IWorkspace workspace, string storageConnectionString, [NotNullWhen(true)] out IQirSubmitter? submitter)
        {
            if (this.TargetId == null)
            {
                submitter = null;
                return false;
            }
            if (SubmitterFactory.QirSubmitter(this.TargetId, workspace, storageConnectionString, this.MaximumCapability.Name) is IQirSubmitter qirSubmitter)
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
        public static bool IsValid(TargetStatusInfo target) => GetProvider(target?.TargetId) != null;

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
}
