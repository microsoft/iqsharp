using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.AzureClient;
using Tests.IQSharp;

namespace Microsoft.Quantum.IQSharp.Mocks
{
    public static class MockExtensions
    {
        public static void AddMocks(this IServiceCollection services)
        {
            var shell = new MockShell();
            services.AddSingleton<IShellServer>(shell);
            services.AddSingleton<IShellRouter>(new MockShellRouter(shell));
            services.AddSingleton<IOptions<KernelContext>>(new MockKernelOptions());
            services.AddSingleton<INugetPackages>(new MockNugetPackages());
            services.AddSingleton<IAzureFactory>(new MocksAzureFactory());
        }
    }
}
