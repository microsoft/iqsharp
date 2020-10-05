// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.IQSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class WorkspaceTests
    {
        [TestMethod]
        public async Task InitWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            await ws.Initialization;

            var dll = ws.Projects.Single().CacheDllPath;
            if (File.Exists(dll)) File.Delete(dll);

            // First time
            ws = Startup.Create<Workspace>("Workspace");
            await ws.Initialization;
            Assert.IsFalse(ws.HasErrors);

            var op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);

            // On next reload:
            ws = Startup.Create<Workspace>("Workspace");
            await ws.Initialization;
            Assert.IsFalse(ws.HasErrors);

            op = ws.AssemblyInfo.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);
        }

        [TestMethod]
        public async Task ReloadWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            await ws.Initialization;
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
            // Loading this workspace should succeed and automatically pull in the referenced projects
            // Workspace.ProjectReferences.ProjectA and Workspace.ProjectReferences.ProjectB.
            var ws = Startup.Create<Workspace>("Workspace.ProjectReferences");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages));

            var operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectB.RotateAndMeasure").Any());
        }

        [TestMethod]
        public void ProjectReferencesWorkspaceNoAutoLoad()
        {
            // Loading this workspace should fail because the .csproj does not specify <IQSharpLoadAutomatically>,
            // which prevents the code that depends on Workspace.ProjectReferences.ProjectB from compiling correctly.
            var ws = Startup.Create<Workspace>("Workspace.ProjectReferences.ProjectA");
            ws.Reload();
            Assert.IsTrue(ws.HasErrors);

            // Loading this workspace should succeed, and its Q# operations should be available, but the .csproj
            // reference should not be loaded because the .csproj specifies <IQSharpLoadAutomatically> as false.
            ws = Startup.Create<Workspace>("Workspace.ProjectReferences.ProjectB");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages));
            Assert.IsTrue(ws.Projects.Count() == 1);
            Assert.IsTrue(string.IsNullOrEmpty(ws.Projects.First().ProjectFile));
            var operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectB.RotateAndMeasure").Any());
        }

        [TestMethod]
        public void ManuallyAddProjects()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages));

            ws.AddProject("../Workspace.ProjectReferences.ProjectA/ProjectA.csproj");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages));
            
            var operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsFalse(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());

            ws.AddProject("../Workspace.ProjectReferences/Workspace.ProjectReferences.csproj");
            ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages));

            operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());

            // Try to add a project that doesn't exist
            Assert.ThrowsException<FileNotFoundException>(() =>
                ws.AddProject("../InvalidProject/InvalidProject.csproj")
            );

            // Try to add a project that should have already been loaded
            Assert.ThrowsException<InvalidOperationException>(() =>
                ws.AddProject("../Workspace.ProjectReferences.ProjectB/ProjectB.csproj")
            );
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
