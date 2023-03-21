// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Core;

namespace Microsoft.Quantum.IQSharp.AzureClient
{
    internal class MockQuantumMachine : IQuantumMachine
    {
        public string ProviderId => "MockQuantumMachine";

        public string Target => "MockQuantumMachine.Target";

        private MockAzureWorkspace? Workspace { get; }

        public MockQuantumMachine(MockAzureWorkspace? workspace = null) => Workspace = workspace;

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            => ExecuteAsync(info, input, null as IQuantumMachineSubmissionContext);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext? submissionContext)
            => ExecuteAsync(info, input, submissionContext, null as IQuantumMachine.ConfigureJob);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext? submissionContext, IQuantumMachine.ConfigureJob? configureJobCallback)
            => ExecuteAsync(info, input, submissionContext, null, configureJobCallback);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineExecutionContext? executionContext)
            => ExecuteAsync(info, input, executionContext, null);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineExecutionContext? executionContext, IQuantumMachine.ConfigureJob? configureJobCallback)
            => ExecuteAsync(info, input, null, executionContext);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext? submissionContext, IQuantumMachineExecutionContext? executionContext)
            => ExecuteAsync(info, input, submissionContext, executionContext, null);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext? submissionContext, IQuantumMachineExecutionContext? executionContext, IQuantumMachine.ConfigureJob? configureJobCallback)
            => throw new NotImplementedException();

        public Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            => SubmitAsync(info, input, null);

        public Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext? submissionContext)
            => SubmitAsync(info, input, submissionContext, null);

        public Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext? submissionContext, IQuantumMachine.ConfigureJob? configureJobCallback)
        {
            var job = new MockCloudJob();
            MockAzureWorkspace.MockJobIds = new string[] { job.Id };
            return Task.FromResult(job as IQuantumMachineJob);
        }

        public (bool IsValid, string Message) Validate<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            => throw new NotImplementedException();
    }
}