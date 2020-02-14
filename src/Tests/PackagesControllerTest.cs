// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

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

            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.IsTrue(response.Result.Length == 1);
            Assert.IsTrue(response.Result[0].StartsWith("Microsoft.Quantum.Standard"));
        }

        [TestMethod]
        public async Task AddPackage()
        {
            var controller = Init();
            var response = await controller.List();

            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.IsTrue(response.Result.Length == 1);
            Assert.IsTrue(response.Result[0].StartsWith("Microsoft.Quantum.Standard"));

            response = await controller.Add("Microsoft.Quantum.Chemistry");
            Assert.AreEqual(0, response.Messages.Length);
            Assert.IsTrue(response.Result.Length == 2);
            Assert.IsTrue(response.Result[0].StartsWith("Microsoft.Quantum.Standard"));
            Assert.IsTrue(response.Result[1].StartsWith("Microsoft.Quantum.Chemistry"));

            response = await controller.Add("Microsoft.Quantum.Research::0.10.1911.2805-alpha");
            Assert.AreEqual(0, response.Messages.Length);
            Assert.IsTrue(response.Result.Length == 3);
            Assert.AreEqual("Microsoft.Quantum.Research::0.10.1911.2805-alpha", response.Result[2]);
        }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
