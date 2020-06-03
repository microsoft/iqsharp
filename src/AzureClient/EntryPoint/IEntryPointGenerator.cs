// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// This service is capable of generating entry points for
    /// job submission to Azure Quantum.
    /// </summary>
    public interface IEntryPointGenerator
    {
        /// <summary>
        /// Compiles an assembly and returns the <see cref="EntryPoint"/> object
        /// representing an entry point that wraps the specified operation.
        /// </summary>
        /// <param name="operationName">The name of the operation to wrap in an entry point.</param>
        /// <param name="executionTarget">The intended execution target for the compiled entry point.</param>
        /// <returns>The generated entry point.</returns>
        public IEntryPoint Generate(string operationName, string executionTarget);
    }
}
