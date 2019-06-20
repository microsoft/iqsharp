// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if TELEMETRY

using System;

using Microsoft.Applications.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class TelemetryTests
    {
        public (Telemetry, IServiceProvider) StartTelemetry(string workspace = "Workspace")
        {
            var services = Startup.CreateServiceProvider(workspace);
            var telemetry = new Telemetry(new MockTelemetryLogger());
            telemetry.InitServices(services, null);

            return (telemetry, services);
        }

        [TestMethod]
        public void WorkspaceReload()
        {
            var (telemetry, services) = StartTelemetry();

            var ws = services.GetService<IWorkspace>();
            var logger = telemetry.Logger as MockTelemetryLogger;
            Assert.AreEqual(0, logger.Events.Count);

            ws.Reload();
            Assert.AreEqual(1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.WorkspaceReload", logger.Events[0].Name);
            Assert.AreEqual(PiiKind.GenericData, logger.Events[0].PiiProperties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("Workspace", logger.Events[0].Properties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("ok", logger.Events[0].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);
        }

        [TestMethod]
        public void InvalidWorkspaceReload()
        {
            var (telemetry, services) = StartTelemetry("Workspace.Broken");

            var ws = services.GetService<IWorkspace>();
            var logger = telemetry.Logger as MockTelemetryLogger;
            Assert.AreEqual(0, logger.Events.Count);

            ws.Reload();
            Assert.AreEqual(1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.WorkspaceReload", logger.Events[0].Name);
            Assert.AreEqual(PiiKind.GenericData, logger.Events[0].PiiProperties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("Workspace.Broken", logger.Events[0].Properties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("error", logger.Events[0].Properties["Quantum.IQSharp.Status"]);
            Assert.IsTrue(logger.Events[0].Properties["Quantum.IQSharp.Errors"].ToString().StartsWith("QS"));
        }

        [TestMethod]
        public void CompileCode()
        {
            var (telemetry, services) = StartTelemetry();

            var snippets = services.GetService<ISnippets>();
            var logger = telemetry.Logger as MockTelemetryLogger;
            Assert.AreEqual(0, logger.Events.Count);

            var count = 0;
            snippets.Compile(SNIPPETS.HelloQ);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);

            count++;
            snippets.Compile(SNIPPETS.HelloQ);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);

            count++;
            snippets.Compile(SNIPPETS.DependsOnHelloQ);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);

            count++;
            Assert.ThrowsException<CompilationErrorsException>(() => snippets.Compile(SNIPPETS.TwoErrors));
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("error", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);

            count++;
            snippets.Compile(SNIPPETS.OneWarning);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);
        }

        [TestMethod]
        public void LoadPackage()
        {
            var (telemetry, services) = StartTelemetry();

            var mgr = services.GetService<IReferences>();
            var logger = telemetry.Logger as MockTelemetryLogger;
            Assert.AreEqual(0, logger.Events.Count);

            mgr.AddPackage("Microsoft.Quantum.Standard");
            Assert.AreEqual(1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.PackageLoad", logger.Events[0].Name);
            Assert.AreEqual("Microsoft.Quantum.Standard", logger.Events[0].Properties["Quantum.IQSharp.PackageId"]);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(logger.Events[0].Properties["Quantum.IQSharp.PackageVersion"]?.ToString()));

            mgr.AddPackage("Microsoft.Quantum.Standard");
            Assert.AreEqual(2, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.PackageLoad", logger.Events[0].Name);
            Assert.AreEqual("Microsoft.Quantum.Standard", logger.Events[0].Properties["Quantum.IQSharp.PackageId"]);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(logger.Events[0].Properties["Quantum.IQSharp.PackageVersion"]?.ToString()));
        }

        [TestMethod]
        public void JupyterActions()
        {
            var (telemetry, services) = StartTelemetry();

            var engine = services.GetService<IExecutionEngine>() as IQSharpEngine;
            var channel = new MockChannel();
            var logger = telemetry.Logger as MockTelemetryLogger;
            Assert.AreEqual(0, logger.Events.Count);

            var count = 0;
            engine.ExecuteMundane(SNIPPETS.HelloQ, channel);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);

            count++;
            engine.Execute("%simulate HelloQ", channel);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Action", logger.Events[count].Name);
            Assert.AreEqual("%simulate", logger.Events[count].Properties["Quantum.IQSharp.Command"]);
            Assert.AreEqual("Ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);

            count++;
            engine.Execute("%package Microsoft.Quantum.Standard", channel);
            Assert.AreEqual(count + 2, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.PackageLoad", logger.Events[count].Name);
            Assert.AreEqual("Microsoft.Quantum.Standard", logger.Events[count].Properties["Quantum.IQSharp.PackageId"]);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(logger.Events[count].Properties["Quantum.IQSharp.PackageVersion"]?.ToString()));
            count++;
            Assert.AreEqual("Quantum.IQSharp.Action", logger.Events[count].Name);
            Assert.AreEqual("%package", logger.Events[count].Properties["Quantum.IQSharp.Command"]);
            Assert.AreEqual("Ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
        }

        [TestMethod]
        public void GetDeviceId()
        {
            var address = Telemetry.GetDeviceId();
            Assert.IsTrue(!string.IsNullOrEmpty(address));
        }
    }
}

#endif