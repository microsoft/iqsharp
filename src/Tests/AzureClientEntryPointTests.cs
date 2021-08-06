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
                var snippets = services.GetRequiredService<ISnippets>();
                snippets.Items = codeSnippets.Select(codeSnippet => new Snippet() { code = codeSnippet });
            }

            return services.GetRequiredService<IEntryPointGenerator>();
        }

        [TestMethod]
        public async Task FromSnippet()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.HelloQ });
            var entryPoint = entryPointGenerator.Generate("HelloQ", null);

            Assert.IsTrue(
                entryPointGenerator.EntryPointAssemblyInfo.Operations.Count() >= 3,
                "Generated entry point assembly only had 0, 1, or 2 operations, but we expect at least three when C# code is properly regenerated."
            );

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
        public void FromBrokenSnippet()
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
                new AzureSubmissionContext()
                {
                    InputParameters = AbstractMagic.ParseInputParameters("count=2 name=\"test\"")
                }
            );
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public void FromWorkspaceMissingArgument()
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
        public void FromWorkspaceIncorrectArgumentType()
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
        public async Task FromProjectReferencesWorkspace()
        {
            var entryPointGenerator = Init("Workspace.ProjectReferences");
            var entryPoint = entryPointGenerator.Generate("Tests.ProjectReferences.MeasureSingleQubit", null);
            Assert.IsNotNull(entryPoint);

            var job = await entryPoint.SubmitAsync(
                new MockQuantumMachine(),
                new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public void FromBrokenWorkspace()
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
        public void InvalidOperationName()
        {
            var entryPointGenerator = Init("Workspace");
            Assert.ThrowsException<UnsupportedOperationException>(() =>
                entryPointGenerator.Generate("InvalidOperationName", null));
        }

        [TestMethod]
        public void InvalidEntryPointOperation()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidEntryPoint });
            Assert.ThrowsException<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("InvalidEntryPoint", null));
        }
    }
}
