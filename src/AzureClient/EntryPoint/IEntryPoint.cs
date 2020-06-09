// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Quantum.Runtime;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    /// <summary>
    /// Represents a Q# entry point that can be submitted
    /// for execution to Azure Quantum.
    /// </summary>
    public interface IEntryPoint
    {
        /// <summary>
        /// Submits the entry point for execution to Azure Quantum.
        /// </summary>
        /// <param name="machine">The <see cref="IQuantumMachine"/> object representing the job submission target.</param>
        /// <param name="submissionContext">The <see cref="AzureSubmissionContext"/> object representing the submission context for the job.</param>
        /// <returns>The details of the submitted job.</returns>
        public Task<IQuantumMachineJob> SubmitAsync(IQuantumMachine machine, AzureSubmissionContext submissionContext);
    }
}
