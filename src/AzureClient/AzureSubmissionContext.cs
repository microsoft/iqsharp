// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal sealed class AzureSubmissionContext : IQuantumMachineSubmissionContext
    {
        public string FriendlyName { get; set; } = string.Empty;

        public int Shots { get; set; } = 500;
    }
}
