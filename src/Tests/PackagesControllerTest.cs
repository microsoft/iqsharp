// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Tests.IQSharp
{
    [TestClass]
    public class PackagesControllerTest
    {
        public PackagesController Init(string root = @"Workspace")
        {
            return Startup.Create<PackagesController>(root);
        }

        [TestMethod]
        public async Task ListPackages()
        {
            var controller = Init();
            var response = await controller.List();

            Assert.AreEqual(Status.success, response.status);
            Assert.AreEqual(0, response.messages.Length);
            Assert.IsTrue(response.result.Length == 1);
            Assert.IsTrue(response.result[0].StartsWith("Microsoft.Quantum.Standard"));
        }

        [TestMethod]
        public async Task AddPackage()
        {
            var controller = Init();
            var response = await controller.List();

            Assert.AreEqual(Status.success, response.status);
            Assert.AreEqual(0, response.messages.Length);
            Assert.IsTrue(response.result.Length == 1);
            Assert.IsTrue(response.result[0].StartsWith("Microsoft.Quantum.Standard"));

            response = await controller.Add("Microsoft.Quantum.Chemistry");
            Assert.AreEqual(0, response.messages.Length);
            Assert.IsTrue(response.result.Length == 2);
            Assert.IsTrue(response.result[0].StartsWith("Microsoft.Quantum.Standard"));
            Assert.IsTrue(response.result[1].StartsWith("Microsoft.Quantum.Chemistry"));

            response = await controller.Add("Microsoft.Quantum.Research::0.4.1901.3104");
            Assert.AreEqual(0, response.messages.Length);
            Assert.IsTrue(response.result.Length == 3);
            Assert.AreEqual("Microsoft.Quantum.Research::0.4.1901.3104", response.result[2]);
        }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
