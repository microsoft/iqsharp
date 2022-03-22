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

            Assert.IsTrue(
                entryPointGenerator.EntryPointAssemblyInfo.Operations.Count() >= 3,
                "Generated entry point assembly only had 0, 1, or 2 operations, but we expect at least three when C# code is properly regenerated."
            );

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
            var entryPoint = entryPointGenerator.Generate("HelloQ", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext());
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidParameterTypes()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidParameterTypes });
            var entryPoint = entryPointGenerator.Generate("UseValidParameterTypes", null, generateQir: true);

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
                new MockQIRSubmitter(validArguments.Select(x => x.Key).ToList()),
                new AzureSubmissionContext()
                {
                    InputParameters = validArguments.ToDictionary(x => x.Key.Name, x => x.Value)
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidBoolParameter()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidBoolParameter });
            var entryPoint = entryPointGenerator.Generate("UseBoolType", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>() { new Argument("myBool", new ArgumentValue.Bool(true)) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myBool"] = "\"true\"" }
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidDoubleParameter()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidDoubleParameter });
            var entryPoint = entryPointGenerator.Generate("UseDoubleType", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>() { new Argument("myDouble", new ArgumentValue.Double(1.2)) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myDouble"] = "\"1.2\"" }
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidIntParameter()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidIntParameter });
            var entryPoint = entryPointGenerator.Generate("UseIntType", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>() { new Argument("myInt", new ArgumentValue.Int(2)) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myInt"] = "\"2\"" }
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidStringParameter()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidStringParameter });
            var entryPoint = entryPointGenerator.Generate("UseStringType", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>() { new Argument("myStr", new ArgumentValue.String("\"Hello\"")) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myStr"] = "\"Hello\"" }
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidPauliParameter()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidPauliParameter });
            var entryPoint = entryPointGenerator.Generate("UsePauliType", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>() { new Argument("myPauli", new ArgumentValue.Pauli(Pauli.PauliX)) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myPauli"] = "\"PauliX\"" }
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidResultParameter()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidResultParameter });
            var entryPoint = entryPointGenerator.Generate("UseResultType", null, generateQir: true);

            Assert.IsNotNull(entryPoint);
            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>() { new Argument("myResult", new ArgumentValue.Result(Result.One)) }),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myResult"] = "\"1\"" }
                });
            Assert.IsNotNull(job);
        }

        [TestMethod]
        public async Task ValidTupleParameters()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.ValidTupleParameters });
            var entryPoint = entryPointGenerator.Generate("UseTupleType", null, generateQir: true);

            var validArguments = new Dictionary<Argument, string>()
            {
                [new Argument("innerInt", new ArgumentValue.Int(7))] = "\"7\"",
                [new Argument("innerDouble", new ArgumentValue.Double(6.4))] = "\"6.4\"",
                [new Argument("innerString", new ArgumentValue.String("\"Hello\""))] = "\"Hello\"",
                [new Argument("innerResult", new ArgumentValue.Result(Result.One))] = "\"1\"",
            };

            var job = await entryPoint.SubmitAsync(
                new MockQIRSubmitter(validArguments.Select(x => x.Key).ToList()),
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
            
            var entryPoint = entryPointGenerator.Generate("UseUnitType", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myUnit"] = "\"()\"" }
                }));

            entryPoint = entryPointGenerator.Generate("UseUnitTypeFirst", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myUnit"] = "\"()\"",
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = entryPointGenerator.Generate("UseUnitTypeMiddle", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myUnit"] = "\"()\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = entryPointGenerator.Generate("UseUnitTypeLast", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
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
        public async Task InvalidArrayParameters()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidArrayParameters });

            var entryPoint = entryPointGenerator.Generate("UseArrayType", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myArray"] = "\"[2, 4, 8]\"" }
                }));

            entryPoint = entryPointGenerator.Generate("UseArrayTypeFirst", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myArray"] = "\"[2, 4, 8]\"",
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = entryPointGenerator.Generate("UseArrayTypeMiddle", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myArray"] = "\"[2, 4, 8]\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = entryPointGenerator.Generate("UseArrayTypeLast", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\"",
                        ["myArray"] = "\"[2, 4, 8]\""
                    }
                }));
        }

        [TestMethod]
        public async Task InvalidRangeParameters()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.InvalidRangeParameters });

            var entryPoint = entryPointGenerator.Generate("UseRangeType", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string> { ["myRange"] = "\"0..2..10\"" }
                }));

            entryPoint = entryPointGenerator.Generate("UseRangeTypeFirst", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myRange"] = "\"0..2..10\"",
                        ["myInt"] = "\"2\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = entryPointGenerator.Generate("UseRangeTypeMiddle", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
                new AzureSubmissionContext()
                {
                    InputParameters = new Dictionary<string, string>
                    {
                        ["myInt"] = "\"2\"",
                        ["myRange"] = "\"0..2..10\"",
                        ["myBool"] = "\"true\""
                    }
                }));

            entryPoint = entryPointGenerator.Generate("UseRangeTypeLast", null);
            Assert.IsNotNull(entryPoint);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => entryPoint.SubmitAsync(
                new MockQIRSubmitter(new List<Argument>()),
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

        [TestMethod]
        public void UnusedOperationInvalidForHardware()
        {
            var entryPointGenerator = Init("Workspace", new string[] { SNIPPETS.UnusedClassicallyControlledOperation });
            var entryPoint = entryPointGenerator.Generate("ValidEntryPoint", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality);
            Assert.IsNotNull(entryPoint);

            Assert.ThrowsException<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("ClassicalControl", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality));
        }

        [TestMethod]
        public void UnusedOperationInvalidForHardwareInWorkspace()
        {
            var entryPointGenerator = Init("Workspace.HardwareTarget");
            var entryPoint = entryPointGenerator.Generate("ValidEntryPoint", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality);
            Assert.IsNotNull(entryPoint);

            Assert.ThrowsException<CompilationErrorsException>(() =>
                entryPointGenerator.Generate("ClassicalControl", "ionq.simulator", RuntimeCapability.BasicQuantumFunctionality));
        }
    }
}
