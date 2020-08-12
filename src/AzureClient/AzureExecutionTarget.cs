// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureProvider { IonQ, Honeywell, QCI }

    internal class AzureExecutionTarget
    {
        public string TargetId { get; private set; } = string.Empty;

        public string PackageName => $"Microsoft.Quantum.Providers.{GetProvider(TargetId)}";

        public AssemblyConstants.RuntimeCapabilities RuntimeCapabilities => GetProvider(TargetId) switch
        {
            AzureProvider.IonQ      => AssemblyConstants.RuntimeCapabilities.QPRGen0,
            AzureProvider.Honeywell => AssemblyConstants.RuntimeCapabilities.QPRGen1,
            AzureProvider.QCI       => AssemblyConstants.RuntimeCapabilities.QPRGen1,
            _                       => AssemblyConstants.RuntimeCapabilities.Unknown
        };

        public static bool IsValid(string targetId) => GetProvider(targetId) != null;

        public static AzureExecutionTarget? Create(string targetId) =>
            IsValid(targetId)
            ? new AzureExecutionTarget() { TargetId = targetId }
            : null;

        /// <summary>
        ///     Gets the Azure Quantum provider corresponding to the given execution target.
        /// </summary>
        /// <param name="targetId">The Azure Quantum execution target ID.</param>
        /// <returns>The <see cref="AzureProvider"/> enum value representing the provider.</returns>
        /// <remarks>
        ///     Valid target IDs are structured as "provider.target".
        ///     For example, "ionq.simulator" or "honeywell.qpu".
        /// </remarks>
        private static AzureProvider? GetProvider(string targetId)
        {
            var parts = targetId.Split('.', 2);
            if (Enum.TryParse(parts[0], true, out AzureProvider provider))
            {
                return provider;
            }

            return null;
        }
    }
}
