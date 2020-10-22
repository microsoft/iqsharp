// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Tests.IQSharp
{
    [TestClass]
    public class WorkspaceControllerTest
    {
        public WorkspaceController Init(string root = @"Workspace")
        {
            return Startup.Create<WorkspaceController>(root);
        }

        [TestMethod]
        public async Task GetMany()
        {
            var controller = Init();
            var response = await controller.GetMany();

            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.IsTrue(response.Result.Length > 1);
        }

        [TestMethod]
        public async Task GetOne()
        {
            var controller = Init();
            var response = await controller.GetOne("Tests.qss.NoOp");

            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.AreEqual("Tests.qss.NoOp", response.Result.FullName);

            response = await controller.GetOne("Tests.qss.HelloAgain");

            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.AreEqual("Tests.qss.HelloAgain", response.Result.FullName);
        }

        [TestMethod]
        public async Task SimulateHelloQ()
        {
            var controller = Init();
            var response = await controller.Simulate("Tests.qss.HelloQ");

            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(1, response.Messages.Length);
            Assert.AreEqual("Hello from quantum world!", response.Messages[0]);
            Assert.AreEqual(QVoid.Instance, response.Result);
        }

        [TestMethod]
        public async Task SimulateHelloAgain()
        {
            var args = new Dictionary<string, string> { { "name", "'foo'" }, { "count", "5" } };
            var controller = Init();
            var messages = new List<string>();

            var response = await controller.Simulate("Tests.qss.HelloAgain", args, messages.Add);

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual($"Hello foo again!", messages[0]);
            Assert.AreEqual(5L, ((QArray<Result>)response).Length);
        }

        [TestMethod]
        public async Task EstimateCCNOTDriver()
        {
            var args = new Dictionary<string, string> { { "applyT", "false" }, { "extra", "whatever" } };    // Extra parameters should be ignored
            var controller = Init();
            var messages = new List<string>();

            var response = await controller.Estimate("Tests.qss.CCNOTDriver", args, messages.Add);

            Assert.AreEqual(0, messages.Count);
            Assert.AreEqual(9, response.Count);
            Assert.AreEqual(10.0, response["CNOT"]);
            Assert.AreEqual(7.0, response["T"]);
            Assert.AreEqual(3.0, response["Width"]);
        }

        [TestMethod]
        public async Task EstimateMissingParameter()
        {
            var controller = Init();

            var response = await controller.Estimate("Tests.qss.CCNOTDriver");

            Assert.AreEqual(Status.Error, response.Status);
            Assert.AreEqual(1, response.Messages.Length);
            Assert.AreEqual($"Received invalid parameters. Please fix and try again:\n applyT: missing.", response.Messages[0]);
        }

        [TestMethod]
        public async Task SimulateUnknown()
        {
            var controller = Init();

            var response = await controller.Simulate("Foo.Q");

            Assert.AreEqual(Status.Error, response.Status);
            Assert.AreEqual(1, response.Messages.Length);
            Assert.AreEqual($"Invalid operation name: Foo.Q", response.Messages[0]);
        }


        [TestMethod]
        public async Task SimulateWithNullWorkspace()
        {
            var controller = Init();
            controller.Workspace = null;

            var response = await controller.Simulate("Foo.Q");

            Assert.AreEqual(Status.Error, response.Status);
            Assert.AreEqual(1, response.Messages.Length);
            Assert.AreEqual($"Workspace is not ready. Try again.", response.Messages[0]);
        }


        [TestMethod]
        public async Task SimulateOnBrokenWorkspace()
        {
            var controller = Init(@"Workspace.Broken");

            var response = await controller.Simulate("Tests.qss.NoOp");

            Assert.AreEqual(Status.Error, response.Status);
            Assert.AreEqual(2, response.Messages.Length);
            Assert.IsNotNull(response.Messages.First(m => m.Contains("QS6301")));
            Assert.IsNotNull(response.Messages.First(m => m.Contains("QS5022")));
        }


        [TestMethod]
        public void JsonToDict()
        {
            var s = "bar";
            var i = 1;
            var t = ("A", ("A.a", "A.b"));
            var a = new int[] { 1, 2, 3, 4};
            var arg = new Dictionary<string, object>
            {
                { "s", s },
                { "i", i },
                { "t", t },
                { "a", a }
            };

            var json = JsonConvert.SerializeObject(arg);
            var result = JsonConverters.JsonToDict(json);

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(JsonConvert.SerializeObject(s), result["s"]);
            Assert.AreEqual(JsonConvert.SerializeObject(i), result["i"]);
            Assert.AreEqual(JsonConvert.SerializeObject(t), result["t"]);
            Assert.AreEqual(JsonConvert.SerializeObject(a), result["a"]);
        }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
