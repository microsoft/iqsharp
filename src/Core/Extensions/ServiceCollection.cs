// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Extensions for IServiceCollection
    /// </summary>
    public static partial class Extensions
    {
        public static T AddIQSharp<T>(this T services)
        where T: IServiceCollection
        {
            services.AddSingleton<IEventService, EventService>();
            services.AddSingleton<ICompilerService, CompilerService>();
            services.AddSingleton<IOperationResolver, OperationResolver>();
            services.AddSingleton<INugetPackages, NugetPackages>();
            services.AddSingleton<IReferences, References>();
            services.AddSingleton<IWorkspace, Workspace>();
            services.AddSingleton<ISnippets, Snippets>();
            services.AddSingleton<PerformanceMonitor>();
            services.AddSingleton<IMetadataController, MetadataController>();

            return services;
        }
    }
}
