// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using Extensions = Microsoft.Quantum.IQSharp.Extensions;


#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Tests.IQSharp
{
    [TestClass]
    public class SnippetsControllerTest
    {
        public SnippetsController Init(string workspace = "Workspace")
        {
            return Startup.Create<SnippetsController>(workspace);
        }

        public static async Task<string[]> AssertCompile(SnippetsController controller, string source, params string[] ops)
        {
            var response = await controller.Compile(source);
            Console.WriteLine(JsonConvert.SerializeObject(response));
            Assert.AreEqual(Status.success, response.status);
            Assert.AreEqual(0, response.messages.Length);
            foreach (var op in ops.OrderBy(o => o).Zip(response.result.OrderBy(o => o), (expected, actual) => new { expected, actual })) { Assert.AreEqual(op.expected, op.actual); }

            return response.result;
        }

        public static async Task<object> AssertSimulate(SnippetsController controller, string snippetName, params string[] messages)
        {
            var response = await controller.Simulate(snippetName);
            Console.WriteLine(JsonConvert.SerializeObject(response));
            Assert.AreEqual(Status.success, response.status);
            foreach (var m in messages.Zip(response.messages, (expected, actual) => new { expected, actual })) { Assert.AreEqual(m.expected, m.actual); }

            return response.result;
        }

        [TestMethod]
        public async Task CompileOne()
        {
            var controller = Init();
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");
        }


        [TestMethod]
        public async Task CompileAndSimulate()
        {
            var controller = Init();

            // Try running without compiling it, fails:
            var response = await controller.Simulate("_snippet_.HelloQ");
            Assert.AreEqual(Status.error, response.status);
            Assert.AreEqual(1, response.messages.Length);
            Assert.AreEqual($"Invalid operation name: _snippet_.HelloQ", response.messages[0]);

            // Compile it:
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");

            // Try running again:
            await AssertSimulate(controller, "HelloQ", "Hello from quantum world!");
        }

        [TestMethod]
        public async Task CompileInvalidFunctor()
        {
            var controller = Init();

            var response = await controller.Compile(SNIPPETS.InvalidFunctor);
            Console.WriteLine(JsonConvert.SerializeObject(response));
            Assert.AreEqual(Status.error, response.status);
            Assert.AreEqual(5, response.messages.Length);
        }

        [TestMethod]
        public async Task CompileAndSimulateOnBrokenWorkspace()
        {
            var controller = Init("Workspace.Broken");

            // Compile it:
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");

            // Try running:
            await AssertSimulate(controller, "HelloQ", "Hello from quantum world!");
        }

        [TestMethod]
        public async Task UpdateSnippet()
        {
            var controller = Init();

            // Compile it:
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");

            // Run:
            await AssertSimulate(controller, "HelloQ", "Hello from quantum world!");

            // Compile it with a new code
            await AssertCompile(controller, SNIPPETS.HelloQ_2, "HelloQ");

            // Run again:
            await AssertSimulate(controller, "HelloQ", "msg0", "msg1");
        }

        [TestMethod]
        public async Task UpdateDependency()
        {
            var controller = Init();

            // Compile HelloQ
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");

            // Compile something that depends on it:
            await AssertCompile(controller, SNIPPETS.DependsOnHelloQ, "DependsOnHelloQ");

            // Compile new version of HelloQ
            await AssertCompile(controller, SNIPPETS.HelloQ_2, "HelloQ");

            // Run dependency, it should reflect changes on HelloQ:
            await AssertSimulate(controller, "DependsOnHelloQ", "msg0", "msg1");
        }

        [TestMethod]
        public async Task MultipleSnippet()
        {
            var controller = Init();

            // Compile it:
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");

            // Compile snippet with dependencies and multiple operations:
            await AssertCompile(controller, SNIPPETS.Op1_Op2, "Op1", "Op2");

            // running Op2:
            await AssertSimulate(controller, "Op2", "Hello from quantum world!");
        }

        [TestMethod]
        public async Task DependsOnWorkspace()
        {
            var controller = Init();

            // Compile it:
            await AssertCompile(controller, SNIPPETS.DependsOnWorkspace, "DependsOnWorkspace");

            // Run:
            var results = await AssertSimulate(controller, "DependsOnWorkspace", "Hello Foo again!") as QArray<Result>;
            Assert.IsNotNull(results);
            Assert.AreEqual(5, results.Length);
        }

        [TestMethod]
        public void IdentifyOperations()
        {
            var compiler = new CompilerService();

            var elements = compiler.IdentifyElements(SNIPPETS.Op1_Op2).Select(Extensions.ToFullName).OrderBy(o => o).ToArray();

            Assert.AreEqual(2, elements.Length);
            Assert.AreEqual("SNIPPET.Op2", elements[1]);
        }
    }
}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
