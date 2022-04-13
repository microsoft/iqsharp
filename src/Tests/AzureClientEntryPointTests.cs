// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
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
            var entryPoint = await entryPointGenerator.Generate("HelloQ", null);

            Assert.IsTrue(
                entryPointGenerator.EntryPointAssemblyInfo?.Operations.Count() >= 3,
                "Generated entry point assembly only had 0, 1, or 2 operations, but we expect at least three when C# code is properly regenerated."
            );

            Assert.IsNotNull(entryPointGenerator.EntryPointAssemblyInfo);
            Assert.That.Assembly(entryPointGenerator.EntryPointAssemblyInfo)
                // Check that snippets compiled from entry points have the
                // syntax trees that we need to generate classical control from.
                .HasResource(DotnetCoreDll.SyntaxTreeResourceName)
                // Make sure that the two particular operations we expect are both there.
                .HasOperation("ENTRYPOINT", "HelloQ")
                .HasOperation(Snippets.SNIPPETS_NAMESPACE, "HelloQ")
                // Since HelloQ calls Message, that function should also be regenerated.
                .HasOperation("Microsoft.Quantum.Intrinsic", "Message");

            // We also want to make sure that all other relevant assemblies
            // have the right resource attached.
            Assert.IsNotNull(entryPointGenerator.SnippetsAssemblyInfo);
            Assert.That.Assembly(entryPointGenerator.SnippetsAssemblyInfo).HasResource(DotnetCoreDll.SyntaxTreeResourceName);
            foreach (var refAsm in entryPointGenerator.References.Assemblies)
            {
                if (refAsm.Assembly.CustomAttributes.Any(attr => attr.AttributeType == typeof(CallableDeclarationAttribute)))
                {
                    Assert.That.Assembly(refAsm).HasResource(DotnetCoreDll.SyntaxTreeResourceName);
                }
            }
            foreach (var asm in entryPointGenerator.WorkspaceAssemblies)
            {
                Assert.That.Assembly(asm).HasResource(DotnetCoreDll.SyntaxTreeResourceName);
            }


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
            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(async () => await
                entryPointGenerator.Generate("TwoErrors", null));
        }

        [TestMethod]
        public async Task FromWorkspace()
        {
            var entryPointGenerator = Init("Workspace");
            var entryPoint = entryPointGenerator.Generate("Tests.qss.HelloAgain", null);
            Assert.IsNotNull(entryPoint);

            var job = await (await entryPoint).SubmitAsync(
                new MockQuantumMachine(),
                new AzureSubmissionContext()
                {
                    InputParameters = AbstractMagic.ParseInputParameters("count=2 name=\"test\"")
                }
            );
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task FromWorkspaceMissingArgument()
        {
            var entryPointGenerator = Init("Workspace");
            var entryPoint = await entryPointGenerator.Generate("Tests.qss.HelloAgain", null);
            Assert.IsNotNull(entryPoint);

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await entryPoint.SubmitAsync(
                    new MockQuantumMachine(),
                    new AzureSubmissionContext() { InputParameters = new Dictionary<string, string>() { ["count"] = "2" } }));
        }

        [TestMethod]
        public async Task FromWorkspaceIncorrectArgumentType()
        {
            var entryPointGenerator = Init("Workspace");
            var entryPoint = await entryPointGenerator.Generate("Tests.qss.HelloAgain", null);
            Assert.IsNotNull(entryPoint);

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await entryPoint.SubmitAsync(
                    new MockQuantumMachine(),
                    new AzureSubmissionContext() { InputParameters = new Dictionary<string, string>() { ["count"] = "NaN", ["name"] = "test" } }));
        }

        [TestMethod]
        public async Task FromProjectReferencesWorkspace()
        {
            var entryPointGenerator = Init("Workspace.ProjectReferences");
            var entryPoint = entryPointGenerator.Generate("Tests.ProjectReferences.MeasureSingleQubit", null);
            Assert.IsNotNull(entryPoint);

            var job = await (await entryPoint).SubmitAsync(
                new MockQuantumMachine(),
                new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task FromBrokenWorkspace()
        {
            var entryPointGenerator = Init("Workspace.Broken");
            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("Tests.qss.HelloAgain", null));
        }

        [TestMethod]
        public async Task FromSnippetDependsOnWorkspace()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.DependsOnWorkspace });
            var entryPoint = entryPointGenerator.Generate("DependsOnWorkspace", null);
            Assert.IsNotNull(entryPoint);

            var job = await (await entryPoint).SubmitAsync(
                    new MockQuantumMachine(),
                    new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task InvalidOperationName()
        {
            var entryPointGenerator = Init("Workspace");
            await Assert.ThrowsExceptionAsync<UnsupportedOperationException>(async () =>
                await entryPointGenerator.Generate("InvalidOperationName", null));
        }

        [TestMethod]
        public async Task InvalidEntryPointOperation()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidEntryPoint });
            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(async () =>
                await entryPointGenerator.Generate("InvalidEntryPoint", null));
        }

        [TestMethod]
        public async Task UnusedOperationInvalidForHardware()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.UnusedClassicallyControlledOperation });
            var entryPoint = await entryPointGenerator.Generate("ValidEntryPoint", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality);
            Assert.IsNotNull(entryPoint);

            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(async () =>
                await entryPointGenerator.Generate("ClassicalControl", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality));
        }

        [TestMethod]
        public async Task UnusedOperationInvalidForHardwareInWorkspace()
        {
            var entryPointGenerator = Init("Workspace.HardwareTarget");
            var entryPoint = await entryPointGenerator.Generate("ValidEntryPoint", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality);
            Assert.IsNotNull(entryPoint);

            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(async () =>
                await entryPointGenerator.Generate("ClassicalControl", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality));
        }
    }
}
