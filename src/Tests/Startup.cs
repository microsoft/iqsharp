// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.IQSharp.Jupyter;

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

            services.Configure<Workspace.Settings>(config);
            services.Configure<NugetPackages.Settings>(config);

            services.AddSingleton<IConfiguration>(config);

            services.AddLogging();
            services.AddMocks();
            services.AddIQSharp();
            services.AddIQSharpKernel();

            return services.BuildServiceProvider();
        }

        internal static T Create<T>(string workspaceFolder) =>
            ActivatorUtilities.CreateInstance<T>(CreateServiceProvider(workspaceFolder));

        public static void AddMocks(this IServiceCollection services)
        {
            services.AddSingleton<IShellServer>(new MockShell());
            services.AddSingleton<IOptions<KernelContext>>(new MockKernelOptions());
        }
    }
}
