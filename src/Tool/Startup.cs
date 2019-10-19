// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace Microsoft.Quantum.IQSharp
{
    /// <summary>
    /// StartUp class used when starting as a WebHost (http server)
    /// </summary>
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Workspace.Settings>(Program.Configuration);
            services.Configure<NugetPackages.Settings>(Program.Configuration);
            services.Configure<ClientInformation>(Program.Configuration);

            services.AddSingleton<IConfiguration>(Program.Configuration);

            services.AddIQSharp();
            services.AddIQSharpKernel();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMvc();
        }
    }
}
