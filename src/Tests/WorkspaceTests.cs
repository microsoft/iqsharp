// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.Quantum.IQSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class WorkspaceTests
    {
        [TestMethod]
        public void InitWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace");

            var dll = Path.Combine(ws.CacheFolder, "__ws__.dll");
            if (File.Exists(dll)) File.Delete(dll);

            // First time
            ws = Startup.Create<Workspace>("Workspace");
            Assert.IsFalse(ws.HasErrors);

            var op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);

            // On next reload:
            ws = Startup.Create<Workspace>("Workspace");
            Assert.IsFalse(ws.HasErrors);

            op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);
        }

        [TestMethod]
        public void ReloadWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors);

            var op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);
        }

        [TestMethod]
        public void BrokenWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Broken");
            ws.Reload();
            Assert.IsTrue(ws.HasErrors);
        }


        [TestMethod]
        public void ChemistryWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Chemistry");
            ws.Reload();
            Assert.IsTrue(ws.HasErrors);

            ws.GlobalReferences.AddPackage("Microsoft.Quantum.Research").Wait();
            ws.Reload();
            Assert.IsFalse(ws.HasErrors);

            var op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Microsoft.Quantum.Chemistry.Samples.OptimizedTrotterEstimateEnergy");
            Assert.IsNotNull(op);
        }
    }
}
