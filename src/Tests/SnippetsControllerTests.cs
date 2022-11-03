// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Web.Models;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
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
            var response = await controller.Compile(new CompileSnippetModel
            {
                Code = source
            });
            Console.WriteLine(JsonConvert.SerializeObject(response));
            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            foreach (var op in ops.Zip(response.Result, (expected, actual) => new { expected, actual })) { Assert.AreEqual(op.expected, op.actual); }

            return response.Result;
        }

        public static async Task<object> AssertSimulate(SnippetsController controller, string snippetName, params string[] messages)
        {
            var response = await controller.Simulate(snippetName);
            Console.WriteLine(JsonConvert.SerializeObject(response));
            Assert.AreEqual(Status.Success, response.Status);
            foreach (var m in messages.Zip(response.Messages, (expected, actual) => new { expected, actual })) { Assert.AreEqual(m.expected, m.actual); }

            return response.Result;
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
            Assert.AreEqual(Status.Error, response.Status);
            Assert.AreEqual(1, response.Messages.Length);
            Assert.AreEqual($"Invalid operation name: _snippet_.HelloQ", response.Messages[0]);

            // Compile it:
            await AssertCompile(controller, SNIPPETS.HelloQ, "HelloQ");

            // Try running again:
            await AssertSimulate(controller, "HelloQ", "Hello from quantum world!");
        }

        [TestMethod]
        public async Task CompileInvalidFunctor()
        {
            var controller = Init();

            var response = await controller.Compile(new CompileSnippetModel
            {
                Code = SNIPPETS.InvalidFunctor
            });
            Console.WriteLine(JsonConvert.SerializeObject(response));
            Assert.AreEqual(Status.Error, response.Status);
            Assert.IsTrue(response.Messages.Length > 1);
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

            // Compile snippet with entry point attributes and multiple operations:
            await AssertCompile(controller, SNIPPETS.Op3_Op4_Op5_EntryPoints, "Op3", "Op4", "Op5");

            // Compile snippet with operations out of alphabetical order to ensure order is preserved:
            await AssertCompile(controller, SNIPPETS.Op6b_Op6a, "Op6b", "Op6a");

            // Compile snippets that end with comments:
            await AssertCompile(controller, SNIPPETS.CommentOnly);
            await AssertCompile(controller, SNIPPETS.Op7_EndsWithComment, "Op7");

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
            Assert.AreEqual(5, results!.Length);
        }

        [TestMethod]
        public void IdentifyOperations()
        {
            var serviceProvider = Startup.CreateServiceProvider("Workspace");
            var compiler = new CompilerService(null, null, eventService: null, serviceProvider: serviceProvider);

            var elements = compiler.IdentifyElements(SNIPPETS.Op1_Op2).Select(Extensions.ToFullName).ToArray();

            Assert.AreEqual(2, elements.Length);
            Assert.AreEqual("SNIPPET.Op2", elements[1]);
        }
    }
}
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
