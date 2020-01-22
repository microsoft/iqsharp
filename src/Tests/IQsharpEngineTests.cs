// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.Quantum.Simulation.Core;
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
            return Startup.Create<IQSharpEngine>(workspace);
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

        public static string AssertCompile(IQSharpEngine engine, string source, params string[] expectedOps)
        {
            var channel = new MockChannel();
            var response = engine.ExecuteMundane(source, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            CollectionAssert.AreEquivalent(expectedOps, response.Output as string[]);

            return response.Output?.ToString();
        }

        public static string AssertSimulate(IQSharpEngine engine, string snippetName, params string[] messages)
        {
            var configSource = new ConfigurationSource(skipLoading: true);
            var simMagic = new SimulateMagic(engine.SymbolsResolver, configSource);
            var channel = new MockChannel();
            var response = simMagic.Execute(snippetName, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            CollectionAssert.AreEqual(messages.Select(ChannelWithNewLines.Format).ToArray(), channel.msgs.ToArray());

            return response.Output?.ToString();
        }

        public static string AssertEstimate(IQSharpEngine engine, string snippetName, params string[] messages)
        {
            var channel = new MockChannel();
            var estimateMagic = new EstimateMagic(engine.SymbolsResolver);
            var response = estimateMagic.Execute(snippetName, channel);
            var result = response.Output as Dictionary<string, double>;
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.IsNotNull(result);
            Assert.AreEqual(8, result.Count);
            CollectionAssert.Contains(result.Keys, "T");
            CollectionAssert.Contains(result.Keys, "CNOT");
            CollectionAssert.AreEqual(messages.Select(ChannelWithNewLines.Format).ToArray(), channel.msgs.ToArray());

            return response.Output?.ToString();
        }

        [TestMethod]
        public void CompileOne()
        {
            var engine = Init();
            AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");
        }


        [TestMethod]
        public void CompileAndSimulate()
        {
            var engine = Init();
            var configSource = new ConfigurationSource(skipLoading: true);
            var simMagic = new SimulateMagic(engine.SymbolsResolver, configSource);
            var channel = new MockChannel();

            // Try running without compiling it, fails:
            var response = simMagic.Execute("_snippet_.HelloQ", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.AreEqual(1, channel.errors.Count);
            Assert.AreEqual(ChannelWithNewLines.Format($"Invalid operation name: _snippet_.HelloQ"), channel.errors[0]);

            // Compile it:
            AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Try running again:
            AssertSimulate(engine, "HelloQ", "Hello from quantum world!");
        }

        [TestMethod]
        public void SimulateWithArguments()
        {
            var engine = Init();

            // Compile it:
            AssertCompile(engine, SNIPPETS.Reverse, "Reverse");

            // Try running again:
            var results = AssertSimulate(engine, "Reverse { \"array\": [2, 3, 4], \"name\": \"foo\" }", "Hello foo");
            Assert.AreEqual("[4,3,2]", results);
        }


        [TestMethod]
        public void Estimate()
        {
            var engine = Init();
            var channel = new MockChannel();

            // Compile it:
            AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Try running again:
            AssertEstimate(engine, "HelloQ");
        }

        [TestMethod]
        public void Toffoli()
        {
            var engine = Init();
            var channel = new MockChannel();

            // Compile it:
            AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Run with toffoli simulator:
            var toffoliMagic = new ToffoliMagic(engine.SymbolsResolver);
            var response = toffoliMagic.Execute("HelloQ", channel);
            var result = response.Output as Dictionary<string, double>;
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(1, channel.msgs.Count);
            Assert.AreEqual(ChannelWithNewLines.Format("Hello from quantum world!"), channel.msgs[0]);
        }

        [TestMethod]
        public void DependsOnWorkspace()
        {
            var engine = Init();

            // Compile it:
            AssertCompile(engine, SNIPPETS.DependsOnWorkspace, "DependsOnWorkspace");

            // Run:
            var results = AssertSimulate(engine, "DependsOnWorkspace", "Hello Foo again!");
            Assert.AreEqual("[Zero,One,Zero,Zero,Zero]", results);
        }

        [TestMethod]
        public void UpdateSnippet()
        {
            var engine = Init();

            // Compile it:
            AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Run:
            AssertSimulate(engine, "HelloQ", "Hello from quantum world!");

            // Compile it with a new code
            AssertCompile(engine, SNIPPETS.HelloQ_2, "HelloQ");

            // Run again:
            AssertSimulate(engine, "HelloQ", "msg0", "msg1");
        }

        [TestMethod]
        public void UpdateDependency()
        {
            var engine = Init();

            // Compile HelloQ
            AssertCompile(engine, SNIPPETS.HelloQ, "HelloQ");

            // Compile something that depends on it:
            AssertCompile(engine, SNIPPETS.DependsOnHelloQ, "DependsOnHelloQ");

            // Compile new version of HelloQ
            AssertCompile(engine, SNIPPETS.HelloQ_2, "HelloQ");

            // Run dependency, it should reflect changes on HelloQ:
            AssertSimulate(engine, "DependsOnHelloQ", "msg0", "msg1");
        }

        [TestMethod]
        public void ReportWarnings()
        {
            var engine = Init();

            {
                var channel = new MockChannel();
                var response = engine.ExecuteMundane(SNIPPETS.ThreeWarnings, channel);
                PrintResult(response, channel);
                Assert.AreEqual(ExecuteStatus.Ok, response.Status);
                Assert.AreEqual(3, channel.msgs.Count);
                Assert.AreEqual(0, channel.errors.Count);
                Assert.AreEqual("ThreeWarnings", new ListToTextResultEncoder().Encode(response.Output).Value.Data);
            }

            {
                var channel = new MockChannel();
                var response = engine.ExecuteMundane(SNIPPETS.OneWarning, channel);
                PrintResult(response, channel);
                Assert.AreEqual(ExecuteStatus.Ok, response.Status);
                Assert.AreEqual(1, channel.msgs.Count);
                Assert.AreEqual(0, channel.errors.Count);
                Assert.AreEqual("OneWarning", new ListToTextResultEncoder().Encode(response.Output).Value.Data);
            }
        }

        [TestMethod]
        public void ReportErrors()
        {
            var engine = Init();

            var channel = new MockChannel();
            var response = engine.ExecuteMundane(SNIPPETS.TwoErrors, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.AreEqual(2, channel.errors.Count);
        }

        [TestMethod]
        public void TestPackages()
        {
            var engine = Init();
            var snippets = engine.Snippets as Snippets;
            var pkgMagic = new PackageMagic(snippets.GlobalReferences);
            var channel = new MockChannel();
            var response = pkgMagic.Execute("", channel);
            var result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Length);

            // Try compiling TrotterEstimateEnergy, it should fail due to the lack
            // of chemistry package.
            response = engine.ExecuteMundane(SNIPPETS.TrotterEstimateEnergy, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            response = pkgMagic.Execute("microsoft.quantum.chemistry", channel);
            result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Length);

            // Now it should compile:
            AssertCompile(engine, SNIPPETS.TrotterEstimateEnergy, "TrotterEstimateEnergy");

        }

        [TestMethod]
        public void TestInvalidPackages()
        {
            var engine = Init();
            var snippets = engine.Snippets as Snippets;
            var pkgMagic = new PackageMagic(snippets.GlobalReferences);
            var channel = new MockChannel();

            var response = pkgMagic.Execute("microsoft.quantum", channel);
            var result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(1, channel.errors.Count);
            Assert.IsTrue(channel.errors[0].StartsWith("Unable to find package 'microsoft.quantum'"));
            Assert.IsNull(result);
        }


        [TestMethod]
        public void TestWho()
        {
            var snippets = Startup.Create<Snippets>("Workspace");
            snippets.Compile(SNIPPETS.HelloQ);

            var whoMagic = new WhoMagic(snippets);
            var channel = new MockChannel();

            // Check the workspace, it should be in error state:
            var response = whoMagic.Execute("", channel);
            var result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(5, result.Length);
            Assert.AreEqual("HelloQ", result[0]);
            Assert.AreEqual("Tests.qss.NoOp", result[4]);
        }

        [TestMethod]
        public void TestWorkspace()
        {
            var engine = Init("Workspace.Chemistry");
            var snippets = engine.Snippets as Snippets;

            var wsMagic = new WorkspaceMagic(snippets.Workspace);
            var pkgMagic = new PackageMagic(snippets.GlobalReferences);

            var channel = new MockChannel();
            var result = new string[0];

            // Check the workspace, it should be in error state:
            var response = wsMagic.Execute("reload", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            response = wsMagic.Execute("", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            // Try compiling a snippet that depends on a workspace that depends on the chemistry package:
            response = engine.ExecuteMundane(SNIPPETS.DependsOnChemistryWorkspace, channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);
            Assert.AreEqual(0, channel.msgs.Count);

            // Add dependencies:
            response = pkgMagic.Execute("microsoft.quantum.chemistry", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            response = pkgMagic.Execute("microsoft.quantum.research", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            // Reload workspace:
            response = wsMagic.Execute("reload", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);

            response = wsMagic.Execute("", channel);
            result = response.Output as string[];
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
            Assert.AreEqual(3, result.Length);

            // Now compilation must work:
            AssertCompile(engine, SNIPPETS.DependsOnChemistryWorkspace, "DependsOnChemistryWorkspace");

            // Check an invalid command
            response = wsMagic.Execute("foo", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Error, response.Status);

            // Check that everything still works:
            response = wsMagic.Execute("", channel);
            PrintResult(response, channel);
            Assert.AreEqual(ExecuteStatus.Ok, response.Status);
        }


        [TestMethod]
        public void TestResolver()
        {
            var snippets = Startup.Create<Snippets>("Workspace");
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

            /// From Canon:
            symbol = resolver.Resolve("ApplyToEach");
            Assert.IsNotNull(symbol);
            Assert.AreEqual("Microsoft.Quantum.Canon.ApplyToEach", symbol.Name);

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
        }
    }
}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
