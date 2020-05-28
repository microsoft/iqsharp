// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal enum AzureProvider { IonQ, Honeywell, QCI }

    internal class AzureExecutionTarget
    {
        public string TargetName { get; private set; }
        public AzureProvider? Provider { get => GetProvider(TargetName); }
        public string PackageName { get => $"Microsoft.Quantum.Providers.{Provider}"; }

        public static bool IsValid(string targetName) => GetProvider(targetName) != null;

        public AzureExecutionTarget(string targetName)
        {
            if (!IsValid(targetName))
            {
                throw new InvalidOperationException($"{targetName} is not a valid target name.");
            }

            TargetName = targetName;
        }

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
