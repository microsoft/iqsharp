// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;

using Microsoft.Azure.Quantum;
using Microsoft.Quantum.QsCompiler;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureProvider { IonQ, Honeywell, QCI, Microsoft, Mock }

    internal class AzureExecutionTarget
    {
        protected AzureExecutionTarget(string? targetId)
        {
            this.TargetId = targetId ?? string.Empty;
        }

        public string? TargetId { get; }

        public virtual string PackageName => 
            GetProvider(TargetId) == AzureProvider.Microsoft
            ? "Microsoft.Quantum.Providers.Core"
            : $"Microsoft.Quantum.Providers.{GetProvider(TargetId)}";

        public RuntimeCapability RuntimeCapability => GetProvider(TargetId) switch
        {
            AzureProvider.IonQ      => RuntimeCapability.BasicQuantumFunctionality,
            AzureProvider.Honeywell => RuntimeCapability.BasicMeasurementFeedback,
            AzureProvider.QCI       => RuntimeCapability.BasicMeasurementFeedback,
            AzureProvider.Microsoft => RuntimeCapability.FullComputation,
            _                       => RuntimeCapability.FullComputation
        };

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
        public static AzureExecutionTarget? Create(TargetStatusInfo target) => target is MockTargetStatus
            ? MockAzureExecutionTarget.CreateMock(target)
            : Create(target?.TargetId);


        /// <summary>
        /// It creates the AzureExecutionTarget instance for the given targetId.
        /// </summary>
        public static AzureExecutionTarget? Create(string? targetId) => GetProvider(targetId) is null
            ? null
            : new AzureExecutionTarget(targetId);


        /// <summary>
        ///     Gets the Azure Quantum provider corresponding to the given execution target.
        /// </summary>
        /// <param name="targetId">The Azure Quantum execution target ID.</param>
        /// <returns>The <see cref="AzureProvider"/> enum value representing the provider.</returns>
        /// <remarks>
        ///     Valid target IDs are structured as "provider.target".
        ///     For example, "ionq.simulator" or "honeywell.qpu".
        /// </remarks>
        protected internal static AzureProvider? GetProvider(string? targetId)
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
