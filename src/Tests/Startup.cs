// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.AzureClient;
using Microsoft.Quantum.IQSharp.Kernel;

namespace Tests.IQSharp
{
    static class Startup
    {
        internal static ServiceProvider CreateServiceProvider(string workspaceFolder)
        {
            var dict = new Dictionary<string, string> { { "Workspace", Path.GetFullPath(workspaceFolder) } };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(config);
            services.Configure<Workspace.Settings>(config);
            services.Configure<NugetPackages.Settings>(config);

            services.AddLogging();
            services.AddTelemetry();
            services.AddIQSharp();
            services.AddIQSharpKernel();
            services.AddAzureClient();
            services.AddMocks();

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ITelemetryService>();
            serviceProvider.GetService<IWorkspace>().WaitForInitialization();
            return serviceProvider;
        }

        internal static T Create<T>(string workspaceFolder) =>
            ActivatorUtilities.CreateInstance<T>(CreateServiceProvider(workspaceFolder));

        public static void AddMocks(this IServiceCollection services)
        {
            var shell = new MockShell();
            services.AddSingleton<IShellServer>(shell);
            services.AddSingleton<IShellRouter>(new MockShellRouter(shell));
            services.AddSingleton<IOptions<KernelContext>>(new MockKernelOptions());
            services.AddSingleton<INugetPackages>(new MockNugetPackages());
        }

        public static void AddTelemetry(this IServiceCollection services)
        {
            services.AddSingleton(typeof(ITelemetryService), TelemetryTests.TelemetryServiceType);
        }
    }
}
