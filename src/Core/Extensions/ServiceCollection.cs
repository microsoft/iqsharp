// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
            services.AddSingleton<IMetadataController, MetadataController>();

            return services;
        }

        public static Task<T> GetRequiredServiceInBackground<T>(this IServiceProvider services, ILogger? logger = null)
        {
            var eventService = services.GetRequiredService<IEventService>();
            eventService.OnServiceInitialized<T>().On += (service) =>
            {
                logger?.LogInformation(
                    "Service {Service} initialized {Time} after startup.",
                    typeof(T),
                    DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
                );
            };
            return Task.Run(() => services.GetRequiredService<T>());
        }
    }
}
