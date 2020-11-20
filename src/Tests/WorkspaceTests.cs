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

            var dll = ws.GetProjectsAsync().Result.Single().CacheDllPath;
            if (File.Exists(dll)) File.Delete(dll);

            // First time
            ws = Startup.Create<Workspace>("Workspace");
            Assert.IsFalse(await ws.GetHasErrorsAsync());

            var op = ws.GetAssemblyInfo().Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);

            // On next reload:
            ws = Startup.Create<Workspace>("Workspace");
            Assert.IsFalse(await ws.GetHasErrorsAsync());

            op = ws.GetAssemblyInfo().Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsNotNull(op);
        }

        [TestMethod]
        public async Task ReloadWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            var originalAssembly = ws.GetAssemblyInfo();
            var op = ws.GetAssemblyInfo().Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(await ws.GetHasErrorsAsync());
            Assert.IsNotNull(op);

            // Calling Reload with no changes, should regenerate the dll:
            await ws.ReloadAsync();
            op = ws.GetAssemblyInfo().Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(await ws.GetHasErrorsAsync());
            Assert.IsNotNull(op);
            Assert.AreNotSame(originalAssembly, ws.GetAssemblyInfo());

            var fileName = Path.Combine(Path.GetFullPath("Workspace"), "BasicOps.qs");
            File.SetLastWriteTimeUtc(fileName, DateTime.UtcNow);
            await ws.ReloadAsync();
            op = ws.GetAssemblyInfo().Operations.FirstOrDefault(o => o.FullName == "Tests.qss.NoOp");
            Assert.IsFalse(await ws.GetHasErrorsAsync());
            Assert.AreNotSame(originalAssembly, ws.GetAssemblyInfo());
            Assert.IsNotNull(op);
        }

        [TestMethod]
        public async Task BrokenWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Broken");
            await ws.ReloadAsync();
            Assert.IsTrue(await ws.GetHasErrorsAsync());
        }

        [TestMethod]
        public async Task ProjectReferencesWorkspace()
        {
            // Loading this workspace should succeed and automatically pull in the referenced projects
            // Workspace.ProjectReferences.ProjectA and Workspace.ProjectReferences.ProjectB.
            var ws = Startup.Create<Workspace>("Workspace.ProjectReferences");
            await ws.ReloadAsync();
            Assert.IsFalse(await ws.GetHasErrorsAsync(), string.Join(Environment.NewLine, await ws.GetErrorMessagesAsync()));

            var operations = (await ws.GetProjectsAsync()).SelectMany(p => p.AssemblyInfo?.Operations);
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
            await ws.ReloadAsync();
            Assert.IsTrue(await ws.GetHasErrorsAsync());

            // Loading this workspace should succeed, and its Q# operations should be available, but the .csproj
            // reference should not be loaded because the .csproj specifies <IQSharpLoadAutomatically> as false.
            ws = Startup.Create<Workspace>("Workspace.ProjectReferences.ProjectB");
            await ws.ReloadAsync();
            Assert.IsFalse(await ws.GetHasErrorsAsync(), string.Join(Environment.NewLine, await ws.GetErrorMessagesAsync()));

            var projects = await ws.GetProjectsAsync();
            Assert.IsTrue(projects.Count() == 1);
            Assert.IsTrue(string.IsNullOrEmpty(projects.First().ProjectFile));
            var operations = projects.SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectB.RotateAndMeasure").Any());
        }

        [TestMethod]
        public async Task ManuallyAddProjects()
        {
            var ws = Startup.Create<Workspace>("Workspace");
            await ws.ReloadAsync();
            Assert.IsFalse(await ws.GetHasErrorsAsync(), string.Join(Environment.NewLine, await ws.GetErrorMessagesAsync()));

            await ws.AddProjectAsync("../Workspace.ProjectReferences.ProjectA/ProjectA.csproj");
            await ws.ReloadAsync();
            Assert.IsFalse(await ws.GetHasErrorsAsync(), string.Join(Environment.NewLine, await ws.GetErrorMessagesAsync()));
            
            var operations = (await ws.GetProjectsAsync()).SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsFalse(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());

            await ws.AddProjectAsync("../Workspace.ProjectReferences/Workspace.ProjectReferences.csproj");
            await ws.ReloadAsync();
            Assert.IsFalse(await ws.GetHasErrorsAsync(), string.Join(Environment.NewLine, await ws.GetErrorMessagesAsync()));

            operations = (await ws.GetProjectsAsync()).SelectMany(p => p.AssemblyInfo?.Operations);
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.MeasureSingleQubit").Any());
            Assert.IsTrue(operations.Where(o => o.FullName == "Tests.ProjectReferences.ProjectA.RotateAndMeasure").Any());

            // Try to add a project that doesn't exist
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(() =>
                ws.AddProjectAsync("../InvalidProject/InvalidProject.csproj")
            );

            // Try to add a project that should have already been loaded
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                ws.AddProjectAsync("../Workspace.ProjectReferences.ProjectB/ProjectB.csproj")
            );
        }

        [TestMethod]
        public async Task ChemistryWorkspace()
        {
            var ws = Startup.Create<Workspace>("Workspace.Chemistry");
            await ws.ReloadAsync();
            Assert.IsTrue(await ws.GetHasErrorsAsync());

            ws.GlobalReferences.AddPackage($"mock.chemistry").Wait();
            await ws.ReloadAsync();
            Assert.IsFalse(await ws.GetHasErrorsAsync());

            var op = ws.GetAssemblyInfo().Operations.FirstOrDefault(o => o.FullName == "Tests.IQSharp.Chemistry.Samples.UseJordanWignerEncodingData");
            Assert.IsNotNull(op);
        }
    }
}
