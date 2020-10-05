﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#nullable enable
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// The iqsharp program.
    /// On top of the default commands for a Jupyter Kernel, this program also
    /// exposes a "server" command that starts this Kernel in HTTP mode,
    /// so the operations can be accessed and simulated using REST methods.
    /// </summary>
    public class Program
    {
        public static IConfiguration? Configuration;

        public class LoggingOptions
        {
            public string? LogPath { get; set; }
        }

        public static bool TelemetryOptOut
#if TELEMETRY
            => !string.IsNullOrEmpty(Configuration?[nameof(TelemetryOptOut)]);
#else
            => true;
#endif

        /// <summary>
        /// Creates dictionary of kernelspec file names to the embedded resource path
        /// of all embedded resources found in <c>Kernel.csproj</c>.
        /// Note: <c>WithKernelSpecResources</c> cannot handle keys that include directories
        /// so we treat all path names as period-separated file names.
        /// </summary>
        private static Dictionary<string, string> GetEmbeddedKernelResources() =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Single(asm => asm.GetName().Name == "Microsoft.Quantum.IQSharp.Kernel")
                .GetManifestResourceNames()
                // Take file name as substring after "Microsoft.Quantum.IQSharp.Kernel.res"
                .ToDictionary(resName => string.Join(".", resName.Split(".").Skip(5)));

        public static int Main(string[] args)
        {
            try
            {
                Configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddCommandLine(
                        args,
                        // We provide explicit aliases for those command line
                        // options that specify client information, matching
                        // the placeholder options that we define below.
                        new Dictionary<string, string>()
                        {
                            ["--user-agent"] = "UserAgent",
                            ["--hosting-env"] = "HostingEnvironment"
                        }
                    )
                    .Add(new NormalizedEnvironmentVariableConfigurationSource
                    {
                        Prefix = "IQSHARP_",
                        Aliases = new Dictionary<string, string>
                        {
                            ["TELEMETRY_OPT_OUT"] = nameof(TelemetryOptOut),
                            ["USER_AGENT"] = "UserAgent",
                            ["HOSTING_ENV"] = "HostingEnvironment",
                            ["LOG_PATH"] = "LogPath",
                            ["AUTO_LOAD_PACKAGES"] = "AutoLoadPackages",
                            ["AUTO_OPEN_NAMESPACES"] = "AutoOpenNamespaces",
                            ["SKIP_AUTO_LOAD_PROJECT"] = "SkipAutoLoadProject",
                        }
                    })
                    .Build();

                var app = new IQSharpKernelApp(
                    Kernel.Constants.IQSharpKernelProperties, new Startup(Configuration).ConfigureServices
                )
                .ConfigureLogging(
                    loggingBuilder => {
                        // As per https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#access-configuration-during-startup,
                        // we need to use an IConfiguration instance directly during
                        // startup, rather than using the nice binding methods
                        // like serviceCollection.Configure<TOptions>(configuration).
                        var options = Configuration.Get<LoggingOptions>();
                        if (options?.LogPath != null && options.LogPath.Length != 0)
                        {
                            loggingBuilder.AddFile(
                                options.LogPath,
                                minimumLevel: LogLevel.Debug
                            );
                        }
                    }
                )
                .WithKernelSpecResources<Kernel.IQSharpEngine>(GetEmbeddedKernelResources());
                app.Command(
                    "server",
                    cmd =>
                    {
                        cmd.HelpOption();
                        cmd.Description = $"Runs IQSharp as an HTTP server.";
                        cmd.OnExecute(() =>
                        {
                            GetHttpServer(args).Run();
                            return 0;
                        });
                    }
                );

                AddWorkspaceOption(
                    app
                    .AddInstallCommand()
                    .AddKernelCommand(
                        // These command options will be consumed by the Program.Configuration
                        // object above, rather than by the kernel application object itself.
                        // Thus, we only need placeholders to prevent the kernel application
                        // from raising an exception when unrecognized options are passed.
                        kernelCmd => {
                            kernelCmd.Option<string>(
                                "--user-agent <AGENT>",
                                "Specifies which user agent has initiated this kernel instance.",
                                CommandOptionType.SingleValue
                            );
                            kernelCmd.Option<string>(
                                "--hosting-env <ENV>",
                                "Specifies the hosting environment that this kernel is being run in.",
                                CommandOptionType.SingleValue
                            );
                        }
                    )
                );

                return app.Execute(args);
            }
            catch (CommandParsingException)
            {
                return 1;
            }
        }

        public static IWebHost GetHttpServer(string[]? args)
        {
           return WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://localhost:8888")
                .UseStartup<Startup>()
                .Build();
        }

        // Adds the Workspace settings to the "server" and "kernel" commands:
        public static KernelApplication AddWorkspaceOption(KernelApplication app)
        {
            var cacheOption = app.Option("--cacheFolder <FOLDER>",
                "Specifies the folder to use to create temporary cache files.", CommandOptionType.SingleValue);
            var workspaceOption = app.Option("-w|--workspace <FOLDER>",
                "Specifies the workspace root folder. " +
                "All .qs files inside this folder will be automatically compiled and the corresponding " +
                "operations available for simulation.", CommandOptionType.SingleValue);
            var skipAutoLoadProjectOption = app.Option("--skipAutoLoadProject",
                "Specifies whether to skip automatically loading the .csproj from the workspace's root folder.", CommandOptionType.SingleValue);

            foreach (var command in app.Commands.Where(c => c.Name == "kernel" || c.Name == "server"))
            {
                command.Options.Add(cacheOption);
                command.Options.Add(workspaceOption);
                command.Options.Add(skipAutoLoadProjectOption);
            }

            return app;
        }
    }
}
