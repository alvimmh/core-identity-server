// Copyright (c) Duende Software. All rights reserved.
//
// By accessing the Duende IdentityServer code here, you are agreeing to the following licensing terms:
// https://duendesoftware.com/license/identityserver.pdf
//
// or, alternatively you can view the license in the project's ~/Licenses/DuendeSoftwareLicense.pdf file
//
// If you do not agree to these terms, do not access the Duende IdentityServer code.

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
        public static IConfigurationSection StaticConfiguration { get; private set; }
        public static IWebHostEnvironment StaticEnvironment { get; private set; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
            StaticConfiguration = configuration.GetSection("static_configuration");
            StaticEnvironment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddProjectDatabases(Configuration);
 
            services.AddProjectIdentity();

            services.AddProjectAuthentication();

            services.AddProjectIdentityServer(Environment, Configuration);

            services.AddProjectAuthorizationPolicies();

            services.AddProjectServices();

            services.AddForwardedHeadersMiddleware(Environment);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/clientservices/correspondence/error");
                app.UseStatusCodePagesWithRedirects("~/clientservices/correspondence/error?errortype={0}");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

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