using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterIdentity
    {
        // registers and configures the ASP.NET Identity system
        public static IServiceCollection AddProjectIdentity(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddProjectTokenProviders();


            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.MaxFailedAccessAttempts = 3;
            });

            return services;
        }
    }
}
