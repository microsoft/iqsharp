// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.IQSharp.AzureClient;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Build.Locator;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// StartUp class used when starting as a WebHost (http server)
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration Configuration;

        // We need to make sure that MSBuild is located exactly once; either
        // failing to call `.RegisterDefaults` or calling it twice will cause
        // an exception.
        private static readonly Lazy<VisualStudioInstance?> visualStudioInstance = new(() =>
            MSBuildLocator.RegisterDefaults()
        );
        public static VisualStudioInstance? VisualStudioInstance => visualStudioInstance.Value;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            // NB: MSBuildLocator must be used as early as possible, so we
            //     run it as services are being configured.
            if (VisualStudioInstance is {} vsi)
            {
                services.AddSingleton<CompilerService.MSBuildMetadata>(new CompilerService.MSBuildMetadata(
                    Version: vsi.Version,
                    RootPath: vsi.VisualStudioRootPath,
                    Name: vsi.Name,
                    Path: vsi.MSBuildPath
                ));
            }

            services.Configure<Workspace.Settings>(Configuration);
            services.Configure<NugetPackages.Settings>(Configuration);
            services.Configure<References.Settings>(Configuration);
            services.Configure<CompilerService.Settings>(Configuration);
            services.Configure<IQSharpEngine.Settings>(Configuration);
            services.Configure<ClientInformation>(Configuration);

            services.AddSingleton(typeof(ITelemetryService), GetTelemetryServiceType());
            services.AddIQSharp();
            services.AddIQSharpKernel();
            services.AddAzureClient();

            var assembly = typeof(PackagesController).Assembly;
            services.AddControllers()
                .AddApplicationPart(assembly);
        }

        private Type GetTelemetryServiceType()
        {
            return
#if TELEMETRY
                Program.TelemetryOptOut ? typeof(NullTelemetryService) : typeof(TelemetryService);
#else
                typeof(NullTelemetryService);
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
