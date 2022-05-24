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

            services.AddProjectDatabases(Environment, Configuration);
 
            services.AddProjectIdentity();

            services.AddProjectAuthentication();

            services.AddProjectIdentityServer(Environment, Configuration);

            services.AddProjectAuthorization();

            services.AddProjectServices(Configuration);

            services.AddForwardedHeadersMiddleware(Environment);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();

            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/clientservices/correspondence/error");
                app.UseStatusCodePagesWithRedirects("~/clientservices/correspondence/error?errortype={0}");
            }

            app.UseForwardedHeaders();

            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            // UseIdentityServer includes a call to UseAuthentication, so it’s not necessary to have both.
            // Ref: https://docs.duendesoftware.com/identityserver/v6/fundamentals/hosting/
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