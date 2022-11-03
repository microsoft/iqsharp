// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.QsCompiler;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
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
                var snippets = services.GetRequiredService<ISnippets>();
                snippets.Items = codeSnippets.Select(codeSnippet => new Snippet() { Code = codeSnippet });
            }

            return services.GetRequiredService<IEntryPointGenerator>();
        }

        internal async Task CheckValidParameter(
            string snippetSource,
            string operationName,
            string argumentName,
            ArgumentValue value,
            string valueAsExpr
        )
        {
            var entryPointGenerator = Init("Workspace", new[] { snippetSource });
            var entryPoint = await entryPointGenerator.Generate(operationName, null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>() { new Argument(argumentName, value) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { [argumentName] = valueAsExpr }
                });
            Assert.IsNotNull(job);
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
                // The namespace must match the one found in the in CompilerService.cs in the Core project.
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
        public async Task QIRSubmission()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.HelloQ });
            var entryPoint = await entryPointGenerator.Generate("HelloQ", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidParameterTypes()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidParameterTypes });
            var entryPoint = await entryPointGenerator.Generate("UseValidParameterTypes", null, generateQir: true);

            Assert.IsNotNull(entryPoint);

            var validArguments = new Dictionary<Argument, string>()
            {
                [new Argument("myBool", new ArgumentValue.Bool(true))] =  "\"true\"",
                [new Argument("myDouble", new ArgumentValue.Double(1.2))] =  "\"1.2\"",
                [new Argument("myInt", new ArgumentValue.Int(2))] =  "\"2\"",
                [new Argument("myStr", new ArgumentValue.String("\"Hello\""))] = "\"Hello\"",
                [new Argument("myPauli", new ArgumentValue.Pauli(Pauli.PauliX))] = "\"PauliX\"",
                [new Argument("myResult", new ArgumentValue.Result(Result.One))] = "\"1\"",
                [new Argument("innerInt", new ArgumentValue.Int(7))] = "\"7\"",
                [new Argument("innerDouble", new ArgumentValue.Double(6.4))] = "\"6.4\""
            };

            var job = await entryPoint.SubmitAsync(
                new MockQirSubmitter(validArguments.Select(x => x.Key).ToList()),
                new AzureSubmissionContext()
                {
                    InputParameters = validArguments.ToDictionary(x => x.Key.Name, x => x.Value)
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidBoolParameter() =>
            await CheckValidParameter(
                SNIPPETS.ValidBoolParameter,
                "UseBoolType",
                "myBool",
                new ArgumentValue.Bool(true),
                "\"true\"");

        [TestMethod]
        public async Task ValidDoubleParameter() =>
            await CheckValidParameter(
                SNIPPETS.ValidDoubleParameter,
                "UseDoubleType",
                "myDouble",
                new ArgumentValue.Double(1.2),
                "\"1.2\"");

        [TestMethod]
        public async Task ValidIntParameter() =>
            await CheckValidParameter(
                SNIPPETS.ValidIntParameter,
                "UseIntType",
                "myInt",
                new ArgumentValue.Int(2),
                "\"2\"");

        [TestMethod]
        public async Task ValidStringParameter() =>
            await CheckValidParameter(
                SNIPPETS.ValidStringParameter,
                "UseStringType",
                "myStr",
                new ArgumentValue.String("\"Hello\""),
                "\"Hello\"");

        [TestMethod]
        public async Task ValidPauliParameter() =>
            await CheckValidParameter(
                SNIPPETS.ValidPauliParameter,
                "UsePauliType",
                "myPauli",
                new ArgumentValue.Pauli(Pauli.PauliX),
                "\"PauliX\"");

        [TestMethod]
        public async Task ValidResultParameter() =>
            await CheckValidParameter(
                SNIPPETS.ValidResultParameter,
                "UseResultType",
                "myResult",
                new ArgumentValue.Result(Result.One),
                "\"1\"");

        [TestMethod]
        public async Task ValidTupleParameters()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidTupleParameters });
            var entryPoint = await entryPointGenerator.Generate("UseTupleType", null, generateQir: true);

            var validArguments = new Dictionary<Argument, string>()
            {
                [new Argument("innerInt", new ArgumentValue.Int(7))] = "\"7\"",
                [new Argument("innerDouble", new ArgumentValue.Double(6.4))] = "\"6.4\"",
                [new Argument("innerString", new ArgumentValue.String("\"Hello\""))] = "\"Hello\"",
                [new Argument("innerResult", new ArgumentValue.Result(Result.One))] = "\"1\"",
            };

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQirSubmitter(validArguments.Select(x => x.Key).ToList()),
                new AzureSubmissionContext()
                {
                    InputParameters = validArguments.ToDictionary(x => x.Key.Name, x => x.Value)
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task InvalidUnitParameters()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidUnitParameters });
            
            var entryPoint = await entryPointGenerator.Generate("UseUnitType", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myUnit"] = "\"()\"" }
                }));

            entryPoint = await entryPointGenerator.Generate("UseUnitTypeFirst", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myUnit"] = "\"()\"",
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = await entryPointGenerator.Generate("UseUnitTypeMiddle", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myUnit"] = "\"()\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = await entryPointGenerator.Generate("UseUnitTypeLast", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\"",
                        ["myUnit"] = "\"()\""
                    }
                }));
        }

        [TestMethod]
        public async Task InvalidRangeParameters()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidRangeParameters });

            var entryPoint = await entryPointGenerator.Generate("UseRangeType", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myRange"] = "\"0..2..10\"" }
                }));

            entryPoint = await entryPointGenerator.Generate("UseRangeTypeFirst", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myRange"] = "\"0..2..10\"",
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = await entryPointGenerator.Generate("UseRangeTypeMiddle", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myRange"] = "\"0..2..10\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = await entryPointGenerator.Generate("UseRangeTypeLast", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQirSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\"",
                        ["myRange"] = "\"0..2..10\""
                    }
                }));
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
            var entryPoint = await entryPointGenerator.Generate("ValidEntryPoint", "ionq.simulator", TargetCapabilityModule.BasicQuantumFunctionality);
            Assert.IsNotNull(entryPoint);

            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(async () =>
                await entryPointGenerator.Generate("ClassicalControl", "ionq.simulator", TargetCapabilityModule.BasicQuantumFunctionality));
        }

        [TestMethod]
        public async Task UnusedOperationInvalidForHardwareInWorkspace()
        {
            var entryPointGenerator = Init("Workspace.HardwareTarget");
            var entryPoint = await entryPointGenerator.Generate("ValidEntryPoint", "ionq.simulator", TargetCapabilityModule.BasicQuantumFunctionality);
            Assert.IsNotNull(entryPoint);

            await Assert.ThrowsExceptionAsync<CompilationErrorsException>(async () =>
                await entryPointGenerator.Generate("ClassicalControl", "ionq.simulator", TargetCapabilityModule.BasicQuantumFunctionality));
        }
    }
}
