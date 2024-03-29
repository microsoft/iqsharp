// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
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
            Assert.That.Workspace(ws).DoesNotHaveErrors();

            var op = ws.AssemblyInfo?.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);

            // On next reload:
            ws = Startup.Create<Workspace>("Workspace");
            await ws.Initialization;
            Assert.That.Workspace(ws).DoesNotHaveErrors();

            op = ws.AssemblyInfo?.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);
        }

        [TestMethod]
        public async Task ReloadWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            await ws.Initialization;
            var originalAssembly = ws.AssemblyInfo;
            var op = ws.AssemblyInfo?.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(ws.HasErrors);
            Assert.IsNotNull(op);

            // Calling Reload with no changes, should regenerate the dll:
            await ws.Reload();
            op = ws.AssemblyInfo?.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(ws.HasErrors);
            Assert.IsNotNull(op);
            Assert.AreNotSame(originalAssembly, ws.AssemblyInfo);

            var fileName = Path.Combine(Path.GetFullPath("Workspace"), "BasicOps.qs");
            File.SetLastWriteTimeUtc(fileName, DateTime.UtcNow);
            await ws.Reload();
            op = ws.AssemblyInfo?.Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);
            Assert.IsFalse(ws.HasErrors);
            Assert.AreNotSame(originalAssembly, ws.AssemblyInfo);
        }

        [TestMethod]
        public async Task BrokenWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Broken");
            await ws.Reload();
            Assert.IsTrue(ws.HasErrors);
        }

        [TestMethod]
        public async Task ProjectReferencesWorkspace()
        {
            // Loading this workspace should succeed and automatically pull in the referenced projects
            // Workspace.ProjectReferences.ProjectA and Workspace.ProjectReferences.ProjectB.
            var ws = Startup.Create<Workspace>("Workspace.ProjectReferences");
            await ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages.OrEmpty()));

            var operations = ws.Projects.SelectMany(p => (p.AssemblyInfo?.Operations).OrEmpty());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectB.RotateAndMeasure").Any());
        }

        [TestMethod]
        public async Task ProjectReferencesWorkspaceNoAutoLoad()
        {
            // Loading this workspace should fail because the .csproj does not specify <IQSharpLoadAutomatically>,
            // which prevents the code that depends on Workspace.ProjectReferences.ProjectB from compiling correctly.
            var ws = Startup.Create<Workspace>("Workspace.ProjectReferences.ProjectA");
            await ws.Reload();
            Assert.IsTrue(ws.HasErrors);

            // Loading this workspace should succeed, and its Q# operations should be available, but the .csproj
            // reference should not be loaded because the .csproj specifies <IQSharpLoadAutomatically> as false.
            ws = Startup.Create<Workspace>("Workspace.ProjectReferences.ProjectB");
            await ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages ?? Enumerable.Empty<string>()));
            Assert.IsTrue(ws.Projects.Count() == 1);
            Assert.IsTrue(string.IsNullOrEmpty(ws.Projects.First().ProjectFile));
            var operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations ?? Enumerable.Empty<OperationInfo>());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectB.RotateAndMeasure").Any());
        }

        [TestMethod]
        public async Task ManuallyAddProjects()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            await ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages ?? Enumerable.Empty<string>()));

            ws.AddProject("../Workspace.ProjectReferences.ProjectA/ProjectA.csproj");
            await ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages ?? Enumerable.Empty<string>()));
            
            var operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations ?? Enumerable.Empty<OperationInfo>());
            Assert.IsFalse(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());

            ws.AddProject("../Workspace.ProjectReferences/Workspace.ProjectReferences.csproj");
            await ws.Reload();
            Assert.IsFalse(ws.HasErrors, string.Join(Environment.NewLine, ws.ErrorMessages ?? Enumerable.Empty<string>()));

            operations = ws.Projects.SelectMany(p => p.AssemblyInfo?.Operations ?? Enumerable.Empty<OperationInfo>());
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
        public async Task ChemistryWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Chemistry");
            await ws.Reload();
            Assert.IsTrue(ws.HasErrors);

            ws.GlobalReferences.AddPackage($"mock.chemistry").Wait();
            await ws.Reload();
            Assert.IsFalse(ws.HasErrors);

            var op = ws
                .AssemblyInfo
                ?.Operations
                ?.FirstOrDefault(o => o.FullName == "Tests.IQSharp.Chemistry.Samples.UseJordanWignerEncodingData");
            Assert.IsNotNull(op);
        }
    }
}
