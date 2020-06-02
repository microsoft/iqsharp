// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureProvider { IonQ, Honeywell, QCI }

    internal class AzureExecutionTarget
    {
        public string TargetName { get; private set; }
        public string PackageName => $"Microsoft.Quantum.Providers.{GetProvider(TargetName)}";

        public static bool IsValid(string targetName) => GetProvider(targetName) != null;

        public static AzureExecutionTarget? Create(string targetName) =>
            IsValid(targetName)
            ? new AzureExecutionTarget() { TargetName = targetName }
            : null;

        private static AzureProvider? GetProvider(string targetName)
        {
            var parts = targetName.Split('.', 2);
            if (Enum.TryParse(parts[0], true, out AzureProvider provider))
            {
                return provider;
            }

            return null;
        }
    }
}
