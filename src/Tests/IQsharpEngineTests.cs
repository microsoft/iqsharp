// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.Jupyter.Core;
using Microsoft.Jupyter.Core.Protocol;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.IQSharp.ExecutionPathTracer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;


#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Tests.IQSharp
{
    [TestClass]
    public class IQSharpEngineTests
    {
        public IQSharpEngine Init(string workspace = "Workspace")
        {
            var engine = Startup.Create<IQSharpEngine>(workspace);
            engine.Start();
            engine.Initialized.Wait();
            engine.Workspace.Initialization.Wait();
            return engine;
        }

        public static void PrintResult(ExecutionResult result, MockChannel channel)
        {
            Console.WriteLine("Result:");
            Console.WriteLine(JsonConvert.SerializeObject(result));

            Console.WriteLine("Errors:");
            foreach (var m in channel.errors) Console.WriteLine($"  {m}");

            Console.WriteLine("Messages:");
            foreach (var m in channel.msgs) Console.WriteLine($"  {m}");
        }

        public static async Task<string> AssertCompile(IQSharpEngine engine, string source, params string[] expectedOps)
        {
            var channel = new MockChannel();
            var response = await engine.ExecuteMundane(source, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            CollectionAssert.AreEquivalent(expectedOps, response.Output as string[]);

            return response.Output?.ToString();
        }

        public static async Task<string> AssertSimulate(IQSharpEngine engine, string snippetName, params string[] messages)
        {
            var configSource = new ConfigurationSource(skipLoading: true);
            var simMagic = new SimulateMagic(engine.SymbolsResolver, configSource);
            var channel = new MockChannel();
            var response = await simMagic.Execute(snippetName, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            CollectionAssert.AreEqual(messages.Select(ChannelWithNewLines.Format).ToArray(), channel.msgs.ToArray());

            return response.Output?.ToString();
        }

        public static async Task<string> AssertEstimate(IQSharpEngine engine, string snippetName, params string[] messages)
        {
            var channel = new MockChannel();
            var estimateMagic = new EstimateMagic(engine.SymbolsResolver);
            var response = await estimateMagic.Execute(snippetName, channel);
            var result = response.Output as DataTable;
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.IsNotNull(result);
            Assert.AreEqual(9, result.Rows.Count);
            var keys = result.Rows.Cast<DataRow>().Select(row => row.ItemArray[0]).ToList();
            CollectionAssert.Contains(keys, "T");
            CollectionAssert.Contains(keys, "CNOT");
            CollectionAssert.AreEqual(messages.Select(ChannelWithNewLines.Format).ToArray(), channel.msgs.ToArray());

            return response.Output?.ToString();
        }

        private async Task AssertTrace(string name, ExecutionPath expectedPath, int expectedDepth)
        {
            var engine = Init("Workspace.ExecutionPathTracer");
            var snippets = engine.Snippets as Snippets;
            var configSource = new ConfigurationSource(skipLoading: true);

            var wsMagic = new WorkspaceMagic(snippets.Workspace);
            var pkgMagic = new PackageMagic(snippets.GlobalReferences);
            var traceMagic = new TraceMagic(engine.SymbolsResolver, configSource);

            var channel = new MockChannel();

            // Add dependencies:
            var response = await pkgMagic.Execute("mock.standard", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            // Reload workspace:
            response = await wsMagic.Execute("reload", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            response = await traceMagic.Execute(name, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            var message = channel.iopubMessages.ElementAtOrDefault(0);
            Assert.IsNotNull(message);
            Assert.AreEqual("render_execution_path", message.Header.MessageType);

            var content = message.Content as ExecutionPathVisualizerContent;
            Assert.IsNotNull(content);

            Assert.AreEqual(expectedDepth, content.RenderDepth);

            var path = content.ExecutionPath.ToObject<ExecutionPath>();
            Assert.IsNotNull(path);
            Assert.AreEqual(expectedPath.ToJson(), path.ToJson());
        }

        [TestMethod]
        public async Task CompileOne()
        {
            var engine = Init();
            await AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");
        }

        [TestMethod]
        public async Task CompileAndSimulate()
        {
            var engine = Init();
            var configSource = new ConfigurationSource(skipLoading: true);
            var simMagic = new SimulateMagic(engine.SymbolsResolver, configSource);
            var channel = new MockChannel();

            // Try running without compiling it, fails:
            var response = await simMagic.Execute("_snippet_.HelloQ", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.AreEqual(1, channel.errors.Count);
            Assert.AreEqual(ChannelWithNewLines.Format($"Invalid operation name: _snippet_.HelloQ"), channel.errors[0]);

            // Compile it:
            await AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Try running again:
            await AssertSimulate(engine, "HelloQ", "Hello from quantum world!");
        }

        [TestMethod]
        public async Task SimulateWithArguments()
        {
            var engine = Init();

            // Compile it:
            await AssertCompile(engine, SNIPPETS.Reverse, "Reverse");

            // Try running again:
            var results = await AssertSimulate(engine, "Reverse { \"array\": [2, 3, 4], \"name\": \"foo\" }", "Hello foo");
            Assert.AreEqual("[4,3,2]", results);
        }

        [TestMethod]
        public async Task OpenNamespaces()
        {
            var engine = Init();

            // Compile:
            await AssertCompile(engine, SNIPPETS.OpenNamespaces1);
            await AssertCompile(engine, SNIPPETS.OpenNamespaces2);
            await AssertCompile(engine, SNIPPETS.DependsOnNamespace, "DependsOnNamespace");

            // Run:
            await AssertSimulate(engine, "DependsOnNamespace", "Hello from DependsOnNamespace", "Hello from quantum world!");
        }

        [TestMethod]
        public async Task OpenAliasedNamespaces()
        {
            var engine = Init();

            // Compile:
            await AssertCompile(engine, SNIPPETS.OpenAliasedNamespace);
            await AssertCompile(engine, SNIPPETS.DependsOnAliasedNamespace, "DependsOnAliasedNamespace");

            // Run:
            await AssertSimulate(engine, "DependsOnAliasedNamespace", "Hello from DependsOnAliasedNamespace");
        }

        [TestMethod]
        public async Task CompileApplyWithin()
        {
            var engine = Init();

            // Compile:
            await AssertCompile(engine, SNIPPETS.ApplyWithinBlock, "ApplyWithinBlock");

            // Run:
            await AssertSimulate(engine, "ApplyWithinBlock", "Within", "Apply", "Within");
        }

        [TestMethod]
        public async Task Estimate()
        {
            var engine = Init();
            var channel = new MockChannel();

            // Compile it:
            await AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Try running again:
            await AssertEstimate(engine, "HelloQ");
        }

        [TestMethod]
        public async Task Toffoli()
        {
            var engine = Init();
            var channel = new MockChannel();

            // Compile it:
            await AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Run with toffoli simulator:
            var toffoliMagic = new ToffoliMagic(engine.SymbolsResolver);
            var response = await toffoliMagic.Execute("HelloQ", channel);
            var result = response.Output as Dictionary<string, double>;
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(1, channel.msgs.Count);
            Assert.AreEqual(ChannelWithNewLines.Format("Hello from quantum world!"), channel.msgs[0]);
        }

        [TestMethod]
        public async Task DependsOnWorkspace()
        {
            var engine = Init();

            // Compile it:
            await AssertCompile(engine, SNIPPETS.DependsOnWorkspace, "DependsOnWorkspace");

            // Run:
            var results = await AssertSimulate(engine, "DependsOnWorkspace", "Hello Foo again!");
            Assert.AreEqual("[Zero,One,Zero,Zero,Zero]", results);
        }

        [TestMethod]
        public async Task UpdateSnippet()
        {
            var engine = Init();

            // Compile it:
            await AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Run:
            await AssertSimulate(engine, "HelloQ", "Hello from quantum world!");

            // Compile it with a new code
            await AssertCompile(engine, SNIPPETS.HelloQ_2, "HelloQ");

            // Run again:
            await AssertSimulate(engine, "HelloQ", "msg0", "msg1");
        }

        [TestMethod]
        public async Task DumpToFile()
        {
            var engine = Init();

            // Compile DumpMachine snippet.
            await AssertCompile(engine, SNIPPETS.DumpToFile, "DumpToFile");

            // Run, which should produce files in working directory.
            await AssertSimulate(engine, "DumpToFile", "Dumped to file!");

            // Ensure the expected files got created.
            var machineFile = "DumpMachine.txt";
            var registerFile = "DumpRegister.txt";
            Assert.IsTrue(System.IO.File.Exists(machineFile));
            Assert.IsTrue(System.IO.File.Exists(registerFile));

            // Clean up produced files, if any.
            if (System.IO.File.Exists(machineFile))
            {
                System.IO.File.Delete(machineFile);
            }
            if (System.IO.File.Exists(registerFile))
            {
                System.IO.File.Delete(registerFile);
            }
        }

        [TestMethod]
        public async Task UpdateDependency()
        {
            var engine = Init();

            // Compile HelloQ
            await AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Compile something that depends on it:
            await AssertCompile(engine, SNIPPETS.DependsOnHelloQ, "DependsOnHelloQ");

            // Compile new version of HelloQ
            await AssertCompile(engine, SNIPPETS.HelloQ_2, "HelloQ");

            // Run dependency, it should reflect changes on HelloQ:
            await AssertSimulate(engine, "DependsOnHelloQ", "msg0", "msg1");
        }

        [TestMethod]
        public async Task ReportWarnings()
        {
            var engine = Init();

            {
                var channel = new MockChannel();
                var response = await engine.ExecuteMundane(SNIPPETS.ThreeWarnings, channel);
                PrintResult(response, channel);
                Assert.AreEqual(ExecuteStatus.Ok, response.Status);
                Assert.AreEqual(3, channel.msgs.Count);
                Assert.AreEqual(0, channel.errors.Count);
                Assert.AreEqual("ThreeWarnings", new ListToTextResultEncoder().Encode(response.Output).Value.Data);
            }

            {
                var channel = new MockChannel();
                var response = await engine.ExecuteMundane(SNIPPETS.OneWarning, channel);
                PrintResult(response, channel);
                Assert.AreEqual(ExecuteStatus.Ok, response.Status);
                Assert.AreEqual(1, channel.msgs.Count);
                Assert.AreEqual(0, channel.errors.Count);
                Assert.AreEqual("OneWarning", new ListToTextResultEncoder().Encode(response.Output).Value.Data);
            }
        }

        [TestMethod]
        public async Task ReportErrors()
        {
            var engine = Init();

            var channel = new MockChannel();
            var response = await engine.ExecuteMundane(SNIPPETS.TwoErrors, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.AreEqual(2, channel.errors.Count);
        }

        [TestMethod]
        public async Task TestPackages()
        {
            var engine = Init();
            var snippets = engine.Snippets as Snippets;

            var pkgMagic = new PackageMagic(snippets.GlobalReferences);
            var references = ((References)pkgMagic.References);
            var packageCount = references.AutoLoadPackages.Count;
            var channel = new MockChannel();
            var response = await pkgMagic.Execute("", channel);
            var result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.AreEqual(packageCount, result.Length);
            Assert.AreEqual("Microsoft.Quantum.Standard::0.0.0", result[0]);
            Assert.AreEqual("Microsoft.Quantum.Standard.Visualization::0.0.0", result[1]);

            // Try compiling TrotterEstimateEnergy, it should fail due to the lack
            // of chemistry package.
            response = await engine.ExecuteMundane(SNIPPETS.UseJordanWignerEncodingData, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            response = await pkgMagic.Execute("mock.chemistry", channel);
            result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.IsNotNull(result);
            Assert.AreEqual(packageCount + 1, result.Length);

            // Now it should compile:
            await AssertCompile(engine, SNIPPETS.UseJordanWignerEncodingData, "UseJordanWignerEncodingData");
        }

        [TestMethod]
        public async Task TestInvalidPackages()
        {
            var engine = Init();
            var snippets = engine.Snippets as Snippets;
            var pkgMagic = new PackageMagic(snippets.GlobalReferences);
            var channel = new MockChannel();

            var response = await pkgMagic.Execute("microsoft.invalid.quantum", channel);
            var result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(1, channel.errors.Count);
            Assert.IsTrue(channel.errors[0].StartsWith("Unable to find package 'microsoft.invalid.quantum'"));
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestProjectMagic()
        {
            var engine = Init();
            var snippets = engine.Snippets as Snippets;
            var projectMagic = new ProjectMagic(snippets.Workspace);
            var channel = new MockChannel();

            var response = await projectMagic.Execute("../Workspace.ProjectReferences/Workspace.ProjectReferences.csproj", channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            var loadedProjectFiles = response.Output as string[];
            Assert.AreEqual(3, loadedProjectFiles.Length);
        }

        [TestMethod]
        public async Task TestWho()
        {
            var snippets = Startup.Create<Snippets>("Workspace");
            await snippets.Workspace.Initialization;
            snippets.Compile(SNIPPETS.HelloQ);

            var whoMagic = new WhoMagic(snippets);
            var channel = new MockChannel();

            // Check the workspace, it should be in error state:
            var response = await whoMagic.Execute("", channel);
            var result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual("HelloQ", result[0]);
            Assert.AreEqual("Tests.qss.NoOp", result[4]);
        }

        [TestMethod]
        public async Task TestWorkspace()
        {
            var engine = Init("Workspace.Chemistry");
            var snippets = engine.Snippets as Snippets;

            var wsMagic = new WorkspaceMagic(snippets.Workspace);
            var pkgMagic = new PackageMagic(snippets.GlobalReferences);

            var channel = new MockChannel();
            var result = new string[0];

            // Check the workspace, it should be in error state:
            var response = await wsMagic.Execute("reload", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            response = await wsMagic.Execute("", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            // Try compiling a snippet that depends on a workspace that depends on the chemistry package:
            response = await engine.ExecuteMundane(SNIPPETS.DependsOnChemistryWorkspace, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);

            // Add dependencies:
            response = await pkgMagic.Execute("mock.chemistry", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            response = await pkgMagic.Execute("mock.research", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            // Reload workspace:
            response = await wsMagic.Execute("reload", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            response = await wsMagic.Execute("", channel);
            result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(2, result.Length);

            // Compilation must work:
            await AssertCompile(engine, SNIPPETS.DependsOnChemistryWorkspace, "DependsOnChemistryWorkspace");

            // Check an invalid command
            response = await wsMagic.Execute("foo", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            // Check that everything still works:
            response = await wsMagic.Execute("", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
        }

        [TestMethod]
        public async Task TestResolver()
        {
            var snippets = Startup.Create<Snippets>("Workspace");
            await snippets.Workspace.Initialization;
            snippets.Compile(SNIPPETS.HelloQ);

            var resolver = new SymbolResolver(snippets);

            // Intrinsics:
            var symbol = resolver.Resolve("X");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("Microsoft.Quantum.Intrinsic.X", symbol.Name);

            // FQN Intrinsics:
            symbol = resolver.Resolve("Microsoft.Quantum.Intrinsic.X");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("Microsoft.Quantum.Intrinsic.X", symbol.Name);

            // From namespace:
            symbol = resolver.Resolve("Tests.qss.CCNOTDriver");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("Tests.qss.CCNOTDriver", symbol.Name);

            symbol = resolver.Resolve("CCNOTDriver");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("Tests.qss.CCNOTDriver", symbol.Name);

            // Snippets:
            symbol = resolver.Resolve("HelloQ");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("HelloQ", symbol.Name);

            // resolver is case sensitive:
            symbol = resolver.Resolve("helloq");
            Assert.IsNull(symbol);

            // Invalid name
            symbol = resolver.Resolve("foo");
            Assert.IsNull(symbol);
        }

        [TestMethod]
        public void TestResolveMagic()
        {
            var resolver = Startup.Create<MagicSymbolResolver>("Workspace.Broken");

            var symbol = resolver.Resolve("%workspace");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("%workspace", symbol.Name);

            symbol = resolver.Resolve("%package") as MagicSymbol;
            Assert.IsNotNull(symbol);
            Assert.AreEqual("%package", symbol.Name);

            Assert.IsNotNull(resolver.Resolve("%who"));
            Assert.IsNotNull(resolver.Resolve("%estimate"));
            Assert.IsNotNull(resolver.Resolve("%simulate"));

            symbol = resolver.Resolve("%foo");
            Assert.IsNull(symbol);

            // AzureClient-provided commands
            Assert.IsNotNull(resolver.Resolve("%azure.connect"));
            Assert.IsNotNull(resolver.Resolve("%azure.target"));
            Assert.IsNotNull(resolver.Resolve("%azure.submit"));
            Assert.IsNotNull(resolver.Resolve("%azure.execute"));
            Assert.IsNotNull(resolver.Resolve("%azure.status"));
            Assert.IsNotNull(resolver.Resolve("%azure.output"));
            Assert.IsNotNull(resolver.Resolve("%azure.jobs"));
        }

        [TestMethod]
        public async Task TestDebugMagic()
        {
            var engine = Init();
            await AssertCompile(engine, SNIPPETS.SimpleDebugOperation, "SimpleDebugOperation");

            var configSource = new ConfigurationSource(skipLoading: true);
            var debugMagic = new DebugMagic(engine.SymbolsResolver, configSource, engine.ShellRouter, engine.ShellServer, null);

            // Start a debug session
            var channel = new MockChannel();
            var cts = new CancellationTokenSource();
            var debugTask = debugMagic.RunAsync("SimpleDebugOperation", channel, cts.Token);

            // Retrieve the debug session ID
            var message = channel.iopubMessages[0];
            Assert.IsNotNull(message);
            Assert.AreEqual("iqsharp_debug_sessionstart", message.Header.MessageType);

            var content = message.Content as DebugSessionContent;
            Assert.IsNotNull(content);
            var debugSessionId = content.DebugSession;

            // Send several iqsharp_debug_advance messages
            var debugAdvanceMessage = new Message
            {
                Header = new MessageHeader
                {
                    MessageType = "iqsharp_debug_advance"
                },
                Content = new UnknownContent
                {
                    Data = new Dictionary<string, object>
                    {
                        ["debug_session"] = debugSessionId
                    }
                }
            };

            foreach (int _ in Enumerable.Range(0, 1000))
            {
                Thread.Sleep(millisecondsTimeout: 10);
                if (debugTask.IsCompleted)
                    break;

                await debugMagic.HandleAdvanceMessage(debugAdvanceMessage);
            }

            // Verify that the command completes successfully
            Assert.IsTrue(debugTask.IsCompleted);
            Assert.AreEqual(System.Threading.Tasks.TaskStatus.RanToCompletion, debugTask.Status);

            // Ensure that expected messages were sent
            Assert.AreEqual("iqsharp_debug_sessionstart", channel.iopubMessages[0].Header.MessageType);
            Assert.AreEqual("iqsharp_debug_opstart", channel.iopubMessages[1].Header.MessageType);
            Assert.AreEqual("iqsharp_debug_sessionend", channel.iopubMessages.Last().Header.MessageType);
            Assert.IsTrue(channel.msgs[0].Contains("Starting debug session"));
            Assert.IsTrue(channel.msgs[1].Contains("Finished debug session"));

            // Verify debug status content
            var debugStatusContent = channel.iopubMessages[1].Content as DebugStatusContent;
            Assert.IsNotNull(debugStatusContent.State);
            Assert.AreEqual(debugSessionId, debugStatusContent.DebugSession);
        }

        [TestMethod]
        public async Task TestDebugMagicCancel()
        {
            var engine = Init();
            await AssertCompile(engine, SNIPPETS.SimpleDebugOperation, "SimpleDebugOperation");

            var configSource = new ConfigurationSource(skipLoading: true);
            var debugMagic = new DebugMagic(engine.SymbolsResolver, configSource, engine.ShellRouter, engine.ShellServer, null);

            // Start a debug session
            var channel = new MockChannel();
            var cts = new CancellationTokenSource();
            var debugTask = debugMagic.RunAsync("SimpleDebugOperation", channel, cts.Token);

            // Cancel the session
            cts.Cancel();

            // Ensure that the task throws an exception
            Assert.ThrowsException<AggregateException>(() => debugTask.Wait());

            // Ensure that expected messages were sent
            Assert.AreEqual("iqsharp_debug_sessionstart", channel.iopubMessages[0].Header.MessageType);
            Assert.AreEqual("iqsharp_debug_sessionend", channel.iopubMessages[1].Header.MessageType);
            Assert.IsTrue(channel.msgs[0].Contains("Starting debug session"));
            Assert.IsTrue(channel.msgs[1].Contains("Finished debug session"));
        }

        [TestMethod]
        public async Task TestTraceMagic()
        {
            await AssertTrace("FooCirc", new ExecutionPath(
                new QubitDeclaration[] { new QubitDeclaration(0) },
                new Operation[]
                {
                    new Operation ()
                    {
                        Gate = "FooCirc",
                        Targets = new List<QubitRegister> () { new QubitRegister (0) },
                        Children = ImmutableList<Operation>.Empty.AddRange (
                            new [] {
                                new Operation () {
                                    Gate = "Foo",
                                        DisplayArgs = "(2.1, (\"bar\"))",
                                        Targets = new List<Register> () { new QubitRegister (0) },
                                },
                            }
                        )
                    }
                }
            ), 1);

            // Depth 2
            await AssertTrace("FooCirc --depth=2", new ExecutionPath(
                new QubitDeclaration[] { new QubitDeclaration(0) },
                new Operation[]
                {
                    new Operation ()
                    {
                        Gate = "FooCirc",
                        Targets = new List<QubitRegister> () { new QubitRegister (0) },
                        Children = ImmutableList<Operation>.Empty.AddRange (
                            new [] {
                                new Operation () {
                                    Gate = "Foo",
                                        DisplayArgs = "(2.1, (\"bar\"))",
                                        Targets = new List<Register> () { new QubitRegister (0) },
                                },
                            }
                        )
                    }
                }
            ), 2);
        }
    }
}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
