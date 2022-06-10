// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
            services.AddSingleton<IConfiguration>(config);
            services.Configure<Workspace.Settings>(config);
            services.Configure<NugetPackages.Settings>(config);

            services.AddLogging(builder =>
            {
                builder.AddProvider(
                    new UnitTestLoggerProvider(
                        new UnitTestLoggerConfiguration
                        {
                            LogLevel = LogLevel.Information
                        }
                    )
                );
            });
            services.AddTelemetry();
            services.AddIQSharp();
            services.AddIQSharpKernel();
            services.AddAzureClient();
            services.AddMocks();

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ITelemetryService>();
            serviceProvider.GetRequiredService<IWorkspace>().Initialization.Wait();
            serviceProvider.AddBuiltInMagicSymbols();
            return serviceProvider;
        }

        internal static T Create<T>(string workspaceFolder, Action<IServiceProvider>? configure = null)
        {
            var serviceProvider = CreateServiceProvider(workspaceFolder);
            configure?.Invoke(serviceProvider);
            return ActivatorUtilities.CreateInstance<T>(serviceProvider);
        }

        internal async static Task<T> Create<T>(string workspaceFolder, Func<IServiceProvider, Task> configure)
        {
            var serviceProvider = CreateServiceProvider(workspaceFolder);
            await configure.Invoke(serviceProvider);
            return ActivatorUtilities.CreateInstance<T>(serviceProvider);
        }

        public static void AddMocks(this IServiceCollection services)
        {
            var shell = new MockShell();
            services.AddSingleton<IShellServer>(shell);
            services.AddSingleton<IShellRouter>(new MockShellRouter(shell));
            services.AddSingleton<ICommsRouter>(new MockCommsRouter(shell));
            services.AddSingleton<IOptions<KernelContext>>(new MockKernelOptions());
            services.AddSingleton<INugetPackages>(new MockNugetPackages());
            services.AddSingleton<IAzureFactory>(new MocksAzureFactory());
        }

        public static void AddTelemetry(this IServiceCollection services)
        {
            services.AddSingleton(typeof(ITelemetryService), TelemetryTests.TelemetryServiceType);
        }

        public static async Task<TOutput> Then<TInput, TOutput>(this Task<TInput> task, Func<TInput, Task<TOutput>> continuation)
        {
            var input = await task;
            return await continuation(input);
        }
    }

}
