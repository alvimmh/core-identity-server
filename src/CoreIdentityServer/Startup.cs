// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CoreIdentityServer.Internals.DependencyInjectionExtensions;

namespace CoreIdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddProjectDatabases(Configuration);
 
            services.AddProjectIdentity();

            services.AddProjectAuthentication();

            services.AddProjectIdentityServer(Configuration);

            services.AddProjectAuthorization();

            services.AddProjectServices();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }

            app.UseExceptionHandler("/clientservices/correspondence/error");
            app.UseStatusCodePagesWithRedirects("~/clientservices/correspondence/error?errortype={0}");

            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Areas",
                    pattern: "{area=access}/{controller=authentication}/{action=signin}/{id?}"
                );
            });
        }
    }
}