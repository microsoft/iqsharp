// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Packaging.Core;
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
            Assert.AreEqual(0, response.Result.Length);
        }

        [TestMethod]
        public async Task AddPackage()
        {
            var controller = Init();
            var response = await controller.List();
            Assert.AreEqual(0, response.Messages.Length);

            var initCount = response.Result.Length;

            response = await controller.Add($"Microsoft.Quantum.Standard");
            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.AreEqual(initCount, response.Result.Length);

            response = await controller.Add($"microsoft.quantum.standard::0.11.2003.3107");
            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.AreEqual(initCount, response.Result.Length);

            response = await controller.Add($"jquery::3.5.0.1");
            Assert.AreEqual(Status.Success, response.Status);
            Assert.AreEqual(0, response.Messages.Length);
            Assert.AreEqual(initCount + 1, response.Result.Length);
        }
    }
}

#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
