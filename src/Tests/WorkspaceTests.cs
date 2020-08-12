// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
            var originalAssembly = ws.AssemblyInfo;
            var op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(ws.HasErrors);
            Assert.IsNotNull(op);

            // Calling Reload with no changes, should regenerate the dll:
            ws.Reload();
            op = ws.AssemblyInfo?.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(ws.HasErrors);
            Assert.IsNotNull(op);
            Assert.AreNotSame(originalAssembly, ws.AssemblyInfo);

            var fileName = Path.Combine(Path.GetFullPath("Workspace"), "BasicOps.qs");
            File.SetLastWriteTimeUtc(fileName, DateTime.UtcNow);
            ws.Reload();
            op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(ws.HasErrors);
            Assert.AreNotSame(originalAssembly, ws.AssemblyInfo);
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
        public void ProjectReferencesWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.ProjectReferences");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages));

            var operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());
        }

        [TestMethod]
        public void ChemistryWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Chemistry");
            ws.Reload();
            Assert.IsTrue(ws.HasErrors);

            ws.GlobalReferences.AddPackage($"mock.chemistry").Wait();
            ws.Reload();
            Assert.IsFalse(ws.HasErrors);

            var op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.IQSharp.Chemistry.Samples.UseJordanWignerEncodingData");
            Assert.IsNotNull(op);
        }
    }
}
