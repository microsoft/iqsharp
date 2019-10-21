// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if TELEMETRY

using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Applications.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.Simulation.Simulators;

using static Microsoft.Jupyter.Core.BaseEngine;

namespace Microsoft.Quantum.IQSharp
{

    public class Telemetry
    {
        public Telemetry(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.Logger = logger;
        }

        public ILogger Logger { get; }

        public void InitServices(IServiceProvider services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            InitLogManager(config);

            var console = services.GetService<Microsoft.Extensions.Logging.ILogger<Telemetry>>();
            Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(console, "Starting Telemetry.");
            Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(console, $"DeviceId: {GetDeviceId()}.");

            var snippets = services.GetService<ISnippets>();
            var workspace = services.GetService<IWorkspace>();
            var references = services.GetService<IReferences>();
            var executionEngine = services.GetService<IExecutionEngine>();

            snippets.SnippetCompiled += (_, info) => this.Logger.LogEvent(info.AsTelemetryEvent());
            workspace.Reloaded += (_, info) => this.Logger.LogEvent(info.AsTelemetryEvent());
            references.PackageLoaded += (_, info) => this.Logger.LogEvent(info.AsTelemetryEvent());

            if (executionEngine is BaseEngine engine)
            {
                engine.MagicExecuted += (_, info) => this.Logger.LogEvent(info.AsTelemetryEvent());
                engine.HelpExecuted += (_, info) => this.Logger.LogEvent(info.AsTelemetryEvent());
            }
        }

        private readonly static string TOKEN = "55aee962ee9445f3a86af864fc0fa766-48882422-3439-40de-8030-228042bd9089-7794";
        
        public static Telemetry _instance;
        private static IConfiguration _config;

        public static void Start(KernelApplication app, IConfiguration config)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (!string.IsNullOrWhiteSpace(config?.GetValue<string>("IQSHARP_TELEMETRY_OPT_OUT")))
            {
                Console.WriteLine("--> IQ# Telemetry opted-out. No telemetry data will be generated.");
                return;
            }

            _config = config;

            app.KernelStarted += OnKenerlStart;
            app.KernelStopped += OnKernelStop;
        }

        public static void OnKenerlStart(ServiceProvider services)
        {
            LogManager.Start(new LogConfiguration());

            _instance = new Telemetry(LogManager.GetLogger(TOKEN, out EVTStatus value));
            _instance.InitServices(services, _config);
            _instance.Logger.LogEvent("SessionStart".AsTelemetryEvent());
        }

        public static void OnKernelStop()
        {
            _instance.Logger.LogEvent("SessionEnd".AsTelemetryEvent());

            LogManager.Teardown();
        }

        public static void InitLogManager(IConfiguration config)
        {
            LogManager.SetSharedContext("AppInfo.Id", "iq#");
            LogManager.SetSharedContext("AppInfo.Version", Jupyter.Constants.IQSharpKernelProperties.KernelVersion);
            LogManager.SetSharedContext("CompilerVersion".WithTelemetryNamespace(), typeof(CompilationUnitManager).Assembly.GetName().Version.ToString());
            LogManager.SetSharedContext("SimulationVersion".WithTelemetryNamespace(), typeof(QuantumSimulator).Assembly.GetName().Version.ToString());
            LogManager.SetSharedContext("Root".WithTelemetryNamespace(), Path.GetFileName(Directory.GetCurrentDirectory()), PiiKind.GenericData);
            LogManager.SetSharedContext("DeviceId".WithTelemetryNamespace(), GetDeviceId(), PiiKind.GenericData);
            LogManager.SetSharedContext("UserAgent".WithTelemetryNamespace(), config?.GetValue<string>("IQSHARP_USER_AGENT"));
            LogManager.SetSharedContext("HostingEnvironment".WithTelemetryNamespace(), config?.GetValue<string>("IQSHARP_HOSTING_ENV"));
        }

        /// <summary>
        /// Return an Id for this device, namely, the first non-empty MAC address it can find across all network interfaces (if any).
        /// </summary>
        public static string GetDeviceId() =>
            NetworkInterface.GetAllNetworkInterfaces()?
                .Select(n => n?.GetPhysicalAddress()?.ToString())
                .Where(address => address != null && !string.IsNullOrWhiteSpace(address) && !address.StartsWith("000000"))
                .FirstOrDefault();
    }

    public static class TelemetryExtensions
    {
        public static string WithTelemetryNamespace(this string name) =>
            $"Quantum.IQSharp.{name}";

        public static EventProperties AsTelemetryEvent(this string name) =>
            new EventProperties() { Name = name.WithTelemetryNamespace() };

        public static EventProperties AsTelemetryEvent(this ReloadedEventArgs info)
        {
            var evt = new EventProperties() { Name = "WorkspaceReload".WithTelemetryNamespace() };

            evt.SetProperty("Workspace".WithTelemetryNamespace(), Path.GetFileName(info.Workspace), PiiKind.GenericData);
            evt.SetProperty("Status".WithTelemetryNamespace(), info.Status);
            evt.SetProperty("FileCount".WithTelemetryNamespace(), info.FileCount);
            evt.SetProperty("Errors".WithTelemetryNamespace(), string.Join(",", info.Errors?.OrderBy(e => e) ?? Enumerable.Empty<string>()));
            evt.SetProperty("Duration".WithTelemetryNamespace(), info.Duration.ToString());

            return evt;
        }

        public static EventProperties AsTelemetryEvent(this SnippetCompiledEventArgs info)
        {
            var evt = new EventProperties() { Name = "Compile".WithTelemetryNamespace() };

            evt.SetProperty("Status".WithTelemetryNamespace(), info.Status);
            evt.SetProperty("Errors".WithTelemetryNamespace(), string.Join(",", info.Errors?.OrderBy(e => e) ?? Enumerable.Empty<string>()));
            evt.SetProperty("Duration".WithTelemetryNamespace(), info.Duration.ToString());

            return evt;
        }

        public static EventProperties AsTelemetryEvent(this PackageLoadedEventArgs info)
        {
            var evt = new EventProperties() { Name = "PackageLoad".WithTelemetryNamespace() };

            evt.SetProperty("PackageId".WithTelemetryNamespace(), info.PackageId);
            evt.SetProperty("PackageVersion".WithTelemetryNamespace(), info.PackageVersion);
            evt.SetProperty("Duration".WithTelemetryNamespace(), info.Duration.ToString());

            return evt;
        }

        public static EventProperties AsTelemetryEvent(this ExecutedEventArgs info)
        {
            var evt = new EventProperties() { Name = "Action".WithTelemetryNamespace() };

            evt.SetProperty("Command".WithTelemetryNamespace(), info.Symbol?.Name);
            evt.SetProperty("Kind".WithTelemetryNamespace(), info.Symbol?.Kind.ToString());
            evt.SetProperty("Status".WithTelemetryNamespace(), info.Result.Status.ToString());
            evt.SetProperty("Duration".WithTelemetryNamespace(), info.Duration.ToString());

            return evt;
        }
    }

 
}

#endif
