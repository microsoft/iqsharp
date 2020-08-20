// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Quantum.IQSharp.Kernel;
using Microsoft.Quantum.IQSharp.AzureClient;
using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// StartUp class used when starting as a WebHost (http server)
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Workspace.Settings>(Configuration);
            services.Configure<NugetPackages.Settings>(Configuration);
            services.Configure<References.Settings>(Configuration);
            services.Configure<CompilerService.Settings>(Configuration);
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
