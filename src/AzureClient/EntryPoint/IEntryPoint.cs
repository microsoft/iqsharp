// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Runtime.Submitters;

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
        /// <param name="cancellationToken">Cancellation token used to interrupt this submission.</param>
        /// <returns>The details of the submitted job.</returns>
        public Task<IQuantumMachineJob> SubmitAsync(IQuantumMachine machine, AzureSubmissionContext submissionContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Submits the entry point for execution to Azure Quantum.
        /// </summary>
        /// <param name="submitter">The <see cref="IQirSubmitter"/> object representing the job submission target.</param>
        /// <param name="submissionContext">The <see cref="AzureSubmissionContext"/> object representing the submission context for the job.</param>
        /// /// <param name="cancellationToken">Cancellation token used to interrupt this submission.</param>
        /// <returns>The details of the submitted job.</returns>
        public Task<IQuantumMachineJob> SubmitAsync(IQirSubmitter submitter, AzureSubmissionContext submissionContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// The stream from which QIR bitcode for the entry point can be read.
        /// </summary>
        public Stream QirStream { get; }
    }
}
