// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// Extensions for IServiceCollection
    /// </summary>
    public static partial class Extensions
    {
        public static void AddIQSharp(this IServiceCollection services)
        {
            services.AddSingleton<IEventService, EventService>();
            services.AddSingleton<ICompilerService, CompilerService>();
            services.AddSingleton<IOperationResolver, OperationResolver>();
            services.AddSingleton<INugetPackages, NugetPackages>();
            services.AddSingleton<IReferences, References>();
            services.AddSingleton<IWorkspace, Workspace>();
            services.AddSingleton<ISnippets, Snippets>();
            services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
            services.AddSingleton<IMetadataController, MetadataController>();
        }
    }
}