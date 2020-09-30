// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !TELEMETRY
using Microsoft.Quantum.IQSharp;
using System;

namespace Tests.IQSharp
{
    public static class TelemetryTests
    {
        public static readonly Type TelemetryServiceType = typeof(NullTelemetryService);
    }
}
#endif

#if TELEMETRY

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Applications.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.IQSharp
{
    [TestClass]
    public class TelemetryTests
    {
        public static readonly Type TelemetryServiceType = typeof(MockTelemetryService);

        [TestMethod]
        public void MockTelemetryService()
        {
            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            GetAppLogger(services);
        }

        private static MockTelemetryService.MockAppLogger GetAppLogger(ServiceProvider services)
        {
            var telemetryService = services.GetService<ITelemetryService>();
            Assert.IsNotNull(telemetryService, "TelemetryService must not be null. It should be added in Startup.cs.");
            Assert.IsInstanceOfType(telemetryService, typeof(MockTelemetryService), "TelemetryService should be of type MockTelemetryService as set in Startup.cs");
            var mockTelemetryService = telemetryService as MockTelemetryService;
            Assert.IsInstanceOfType(mockTelemetryService.TelemetryLogger, typeof(MockTelemetryService.MockAppLogger), "TelemetryService.TelemetryLogger should be of type MockTelemetryService.MockAppLogger, set by MockTelemetryService");
            var mockAppLogger = mockTelemetryService.TelemetryLogger as MockTelemetryService.MockAppLogger;
            return mockAppLogger;
        }

        [TestMethod]
        public void WorkspaceReload()
        {
            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            var ws = services.GetService<IWorkspace>();

            logger.Events.Clear();
            Assert.AreEqual(0, logger.Events.Count);

            ws.Reload();
            Assert.AreEqual(1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.WorkspaceReload", logger.Events[0].Name);
            Assert.AreEqual(PiiKind.GenericData, logger.Events[0].PiiProperties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("Workspace", logger.Events[0].Properties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("ok", logger.Events[0].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);
            Assert.AreEqual(2L, logger.Events[0].Properties["Quantum.IQSharp.FileCount"]);
            Assert.AreEqual(0L, logger.Events[0].Properties["Quantum.IQSharp.ProjectCount"]);
        }

        [TestMethod]
        public void InvalidWorkspaceReload()
        {
            var workspace = "Workspace.Broken";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            var ws = services.GetService<IWorkspace>();

            logger.Events.Clear();
            Assert.AreEqual(0, logger.Events.Count);

            ws.Reload();
            Assert.AreEqual(1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.WorkspaceReload", logger.Events[0].Name);
            Assert.AreEqual(PiiKind.GenericData, logger.Events[0].PiiProperties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("Workspace.Broken", logger.Events[0].Properties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("error", logger.Events[0].Properties["Quantum.IQSharp.Status"]);
            Assert.IsTrue(logger.Events[0].Properties["Quantum.IQSharp.Errors"].ToString().StartsWith("QS"));
            Assert.AreEqual(2L, logger.Events[0].Properties["Quantum.IQSharp.FileCount"]);
            Assert.AreEqual(0L, logger.Events[0].Properties["Quantum.IQSharp.ProjectCount"]);
        }

        [TestMethod]
        public void CompileCode()
        {
            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            var snippets = services.GetService<ISnippets>();

            logger.Events.Clear();
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
            Assert.AreEqual(
                "Microsoft.Quantum.Canon,Microsoft.Quantum.Intrinsic",
                logger.Events[count].Properties["Quantum.IQSharp.Namespaces"]);

            count++;
            snippets.Compile(SNIPPETS.OpenNamespaces2);
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);
            Assert.AreEqual(
                "Microsoft.Quantum.Canon,Microsoft.Quantum.Diagnostics,Microsoft.Quantum.Intrinsic",
                logger.Events[count].Properties["Quantum.IQSharp.Namespaces"]);
        }

        [TestMethod]
        public void LoadPackage()
        {
            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            var mgr = services.GetService<IReferences>();

            logger.Events.Clear();
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
        public void LoadProjects()
        {
            var workspace = "Workspace.ProjectReferences";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            var ws = services.GetService<IWorkspace>();

            logger.Events.Clear();
            Assert.AreEqual(0, logger.Events.Count);

            ws.Reload();
            Assert.AreEqual(5, logger.Events.Count);

            Assert.AreEqual("Quantum.IQSharp.PackageLoad", logger.Events[0].Name);
            Assert.IsTrue(logger.Events[0].Properties["Quantum.IQSharp.PackageId"].ToString().StartsWith("Microsoft.Quantum.Xunit"));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(logger.Events[0].Properties["Quantum.IQSharp.PackageVersion"]?.ToString()));

            Assert.AreEqual("Quantum.IQSharp.ProjectLoad", logger.Events[1].Name);
            Assert.AreEqual(PiiKind.Uri, logger.Events[1].PiiProperties["Quantum.IQSharp.ProjectUri"]);
            Assert.IsTrue(logger.Events[1].Properties["Quantum.IQSharp.ProjectUri"].ToString().Contains("ProjectB.csproj"));
            Assert.AreEqual(1L, logger.Events[1].Properties["Quantum.IQSharp.SourceFileCount"]);
            Assert.AreEqual(0L, logger.Events[1].Properties["Quantum.IQSharp.ProjectReferenceCount"]);
            Assert.AreEqual(0L, logger.Events[1].Properties["Quantum.IQSharp.PackageReferenceCount"]);
            Assert.AreEqual(false, logger.Events[1].Properties["Quantum.IQSharp.UserAdded"]);

            Assert.AreEqual("Quantum.IQSharp.ProjectLoad", logger.Events[2].Name);
            Assert.AreEqual(PiiKind.Uri, logger.Events[2].PiiProperties["Quantum.IQSharp.ProjectUri"]);
            Assert.IsTrue(logger.Events[2].Properties["Quantum.IQSharp.ProjectUri"].ToString().Contains("ProjectA.csproj"));
            Assert.AreEqual(1L, logger.Events[2].Properties["Quantum.IQSharp.SourceFileCount"]);
            Assert.AreEqual(1L, logger.Events[2].Properties["Quantum.IQSharp.ProjectReferenceCount"]);
            Assert.AreEqual(0L, logger.Events[2].Properties["Quantum.IQSharp.PackageReferenceCount"]);
            Assert.AreEqual(false, logger.Events[2].Properties["Quantum.IQSharp.UserAdded"]);

            Assert.AreEqual("Quantum.IQSharp.ProjectLoad", logger.Events[3].Name);
            Assert.AreEqual(PiiKind.Uri, logger.Events[3].PiiProperties["Quantum.IQSharp.ProjectUri"]);
            Assert.IsTrue(logger.Events[3].Properties["Quantum.IQSharp.ProjectUri"].ToString().Contains("Workspace.ProjectReferences.csproj"));
            Assert.AreEqual(1L, logger.Events[3].Properties["Quantum.IQSharp.SourceFileCount"]);
            Assert.AreEqual(3L, logger.Events[3].Properties["Quantum.IQSharp.ProjectReferenceCount"]);
            Assert.AreEqual(1L, logger.Events[3].Properties["Quantum.IQSharp.PackageReferenceCount"]);
            Assert.AreEqual(false, logger.Events[3].Properties["Quantum.IQSharp.UserAdded"]);

            Assert.AreEqual("Quantum.IQSharp.WorkspaceReload", logger.Events[4].Name);
            Assert.AreEqual(PiiKind.GenericData, logger.Events[4].PiiProperties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("Workspace.ProjectReferences", logger.Events[4].Properties["Quantum.IQSharp.Workspace"]);
            Assert.AreEqual("ok", logger.Events[4].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[4].Properties["Quantum.IQSharp.Errors"]);
            Assert.AreEqual(3L, logger.Events[4].Properties["Quantum.IQSharp.FileCount"]);
            Assert.AreEqual(3L, logger.Events[4].Properties["Quantum.IQSharp.ProjectCount"]);
        }

        [TestMethod]
        public void JupyterActions()
        {
            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            var engine = services.GetService<IExecutionEngine>() as IQSharpEngine;
            var performanceMonitor = services.GetService<IPerformanceMonitor>();

            // Disable background logging so as to enable a determinstic test.
            performanceMonitor.EnableBackgroundReporting = false;

            var channel = new MockChannel();

            logger.Events.Clear();
            Assert.AreEqual(0, logger.Events.Count);

            var count = 0;
            engine.ExecuteMundane(SNIPPETS.HelloQ, channel).Wait();
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Compile", logger.Events[count].Name);
            Assert.AreEqual("ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            Assert.AreEqual("", logger.Events[0].Properties["Quantum.IQSharp.Errors"]);

            count++;
            engine.Execute("%simulate HelloQ", channel).Wait();
            // We expect both an Action and a SimulatorPerformance event from
            // running %simulate.
            Assert.AreEqual(count + 2, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.Action", logger.Events[count].Name);
            Assert.AreEqual("%simulate", logger.Events[count].Properties["Quantum.IQSharp.Command"]);
            Assert.AreEqual("Ok", logger.Events[count].Properties["Quantum.IQSharp.Status"]);
            count++;
            Assert.AreEqual("Quantum.IQSharp.SimulatorPerformance", logger.Events[count].Name);
            Assert.AreEqual(typeof(QuantumSimulator).FullName, logger.Events[count].Properties["Quantum.IQSharp.SimulatorName"]);
            Assert.AreEqual("0", logger.Events[count].Properties["Quantum.IQSharp.NQubits"]);
            // NB: Not testing Duration, since that is non-determinstic.

            // Make sure that forcing a performance report works.
            count++;
            performanceMonitor.Report();
            Assert.AreEqual(count + 1, logger.Events.Count);
            Assert.AreEqual("Quantum.IQSharp.KernelPerformance", logger.Events[count].Name);            

            count++;
            engine.Execute("%package Microsoft.Quantum.Standard", channel).Wait();
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
        public void ContextVariables()
        {
            var dict = new Dictionary<string, string> 
            {
                { "UserAgent", "TestUserAgent" },
                { "HostingEnvironment", "TestHostingEnvironment" }
            };

            Program.Configuration ??= new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();

            var workspace = "Workspace";
            var services = Startup.CreateServiceProvider(workspace);
            var logger = GetAppLogger(services);

            logger.Context.TryGetValue("AppInfo.Id", out var value);
            Assert.AreEqual("iq#", value);

            logger.Context.TryGetValue("AppInfo.Version", out value);
            Assert.AreEqual(Constants.IQSharpKernelProperties.KernelVersion, value);

            logger.Context.TryGetValue("Quantum.IQSharp.CompilerVersion", out value);
            Assert.AreEqual(typeof(CompilationUnitManager).Assembly.GetName().Version.ToString(), value);

            logger.Context.TryGetValue("Quantum.IQSharp.SimulationVersion", out value);
            Assert.AreEqual(typeof(QuantumSimulator).Assembly.GetName().Version.ToString(), value);

            logger.Context.TryGetValue("Quantum.IQSharp.Root", out value);
            Assert.AreEqual(Path.GetFileName(Directory.GetCurrentDirectory()), value);

            logger.Context.TryGetValue("Quantum.IQSharp.DeviceId", out value);
            Assert.AreEqual(TelemetryService.GetDeviceId(), value);

            logger.Context.TryGetValue("Quantum.IQSharp.UserAgent", out value);
            Assert.AreEqual(Program.Configuration["UserAgent"], value);

            logger.Context.TryGetValue("Quantum.IQSharp.HostingEnvironment", out value);
            Assert.AreEqual(Program.Configuration["HostingEnvironment"], value);
        }
    }

    public class MockTelemetryService : TelemetryService
    {
        public class MockAppLogger : Microsoft.Applications.Events.ILogger
        {
            public List<EventProperties> Events { get; } = new List<EventProperties>();
            public Dictionary<string, object> Context { get; } = new Dictionary<string, object>();

            public EVTStatus LogEvent(EventProperties properties)
            {
                Events.Add(properties);
                return EVTStatus.OK;
            }

            public Task<SendResult> LogEventAsync(EventProperties properties)
            {
                Events.Add(properties);
                return Task.FromResult(new SendResult(ResultStatus.Send));
            }

            public EVTStatus SetContext(string name, string value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, double value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, long value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, sbyte value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, short value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, int value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, byte value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, ushort value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, uint value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, bool value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, DateTime value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }

            public EVTStatus SetContext(string name, Guid value, PiiKind piiKind = PiiKind.None)
            {
                Context[name] = value;
                return EVTStatus.OK;
            }
        }

        public MockTelemetryService(ILogger<TelemetryService> logger, IEventService eventService)
            : base(logger, eventService)
        {
        }

        public override Microsoft.Applications.Events.ILogger CreateLogManager(IConfiguration config)
        {
            return new MockAppLogger();
        }
    }
}

#endif
