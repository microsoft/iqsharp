// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Runtime;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class AzureClientEntryPointTests
    {
        private IEntryPointGenerator Init(string workspace, IEnumerable<string>? codeSnippets = null)
        {
            var services = Startup.CreateServiceProvider(workspace);

            if (codeSnippets != null)
            {
                var snippets = services.GetService<ISnippets>();
                snippets.Items = codeSnippets.Select(codeSnippet => new Snippet() { code = codeSnippet });
            }

            return services.GetService<IEntryPointGenerator>();
        }

        [TestMethod]
        public async Task FromSnippet()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.HelloQ });
            var entryPoint = entryPointGenerator.Generate("HelloQ", null);
            Assert.IsNotNull(entryPoint);

            var job = await entryPoint.SubmitAsync(
                new MockQuantumMachine(),
                new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task FromBrokenSnippet()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.TwoErrors });
            Assert.ThrowsException<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("TwoErrors", null));
        }

        [TestMethod]
        public async Task FromWorkspace()
        {
            var entryPointGenerator = Init("Workspace");
            var entryPoint = entryPointGenerator.Generate("Tests.qss.HelloAgain", null);
            Assert.IsNotNull(entryPoint);

            var job = await entryPoint.SubmitAsync(
                new MockQuantumMachine(),
                new AzureSubmissionContext() { InputParameters = new Dictionary<string, string>() { ["count"] = "2", ["name"] = "test" } });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task FromWorkspaceMissingArgument()
        {
            var entryPointGenerator = Init("Workspace");
            var entryPoint = entryPointGenerator.Generate("Tests.qss.HelloAgain", null);
            Assert.IsNotNull(entryPoint);

            Assert.ThrowsException<ArgumentException>(() =>
                entryPoint.SubmitAsync(
                    new MockQuantumMachine(),
                    new AzureSubmissionContext() { InputParameters = new Dictionary<string, string>() { ["count"] = "2" } }));
        }

        [TestMethod]
        public async Task FromWorkspaceIncorrectArgumentType()
        {
            var entryPointGenerator = Init("Workspace");
            var entryPoint = entryPointGenerator.Generate("Tests.qss.HelloAgain", null);
            Assert.IsNotNull(entryPoint);

            Assert.ThrowsException<ArgumentException>(() =>
                entryPoint.SubmitAsync(
                    new MockQuantumMachine(),
                    new AzureSubmissionContext() { InputParameters = new Dictionary<string, string>() { ["count"] = "NaN", ["name"] = "test" } }));
        }

        [TestMethod]
        public async Task FromBrokenWorkspace()
        {
            var entryPointGenerator = Init("Workspace.Broken");
            Assert.ThrowsException<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("Tests.qss.HelloAgain", null));
        }

        [TestMethod]
        public async Task FromSnippetDependsOnWorkspace()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.DependsOnWorkspace });
            var entryPoint = entryPointGenerator.Generate("DependsOnWorkspace", null);
            Assert.IsNotNull(entryPoint);

            var job = await entryPoint.SubmitAsync(
                    new MockQuantumMachine(),
                    new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task InvalidOperationName()
        {
            var entryPointGenerator = Init("Workspace");
            Assert.ThrowsException<UnsupportedOperationException>(() =>
                entryPointGenerator.Generate("InvalidOperationName", null));
        }

        [TestMethod]
        public async Task InvalidEntryPointOperation()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidEntryPoint });
            Assert.ThrowsException<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("InvalidEntryPoint", null));
        }
    }

    public class MockQuantumMachine : IQuantumMachine
    {
        public string ProviderId => throw new NotImplementedException();

        public string Target => throw new NotImplementedException();

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            => ExecuteAsync(info, input, null as IQuantumMachineSubmissionContext);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext submissionContext)
            => ExecuteAsync(info, input, submissionContext, null as IQuantumMachine.ConfigureJob);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext submissionContext, IQuantumMachine.ConfigureJob configureJobCallback)
            => ExecuteAsync(info, input, submissionContext, null, configureJobCallback);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineExecutionContext executionContext)
            => ExecuteAsync(info, input, executionContext, null);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineExecutionContext executionContext, IQuantumMachine.ConfigureJob configureJobCallback)
            => ExecuteAsync(info, input, null, executionContext);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext submissionContext, IQuantumMachineExecutionContext executionContext)
            => ExecuteAsync(info, input, submissionContext, executionContext, null);

        public Task<IQuantumMachineOutput<TOutput>> ExecuteAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext submissionContext, IQuantumMachineExecutionContext executionContext, IQuantumMachine.ConfigureJob configureJobCallback)
            => throw new NotImplementedException();

        public Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            => SubmitAsync(info, input, null);

        public Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext submissionContext)
            => SubmitAsync(info, input, submissionContext, null);

        public Task<IQuantumMachineJob> SubmitAsync<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input, IQuantumMachineSubmissionContext submissionContext, IQuantumMachine.ConfigureJob configureJobCallback)
            => Task.FromResult(new MockQuantumMachineJob() as IQuantumMachineJob);

        public (bool IsValid, string Message) Validate<TInput, TOutput>(EntryPointInfo<TInput, TOutput> info, TInput input)
            => throw new NotImplementedException();
    }

    public class MockQuantumMachineJob : IQuantumMachineJob
    {
        public bool Failed => throw new NotImplementedException();

        public string Id => throw new NotImplementedException();

        public bool InProgress => throw new NotImplementedException();

        public string Status => throw new NotImplementedException();

        public bool Succeeded => throw new NotImplementedException();

        public Uri Uri => throw new NotImplementedException();

        public Task CancelAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RefreshAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
