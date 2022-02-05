using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterIdentityServer
    {
        public static IServiceCollection AddProjectIdentityServer(this IServiceCollection services, IConfiguration config)
        {
            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                config.GetConnectionString("AuxiliaryDatabaseConnection")
            );

            dbConnectionBuilder.Username = config["cisdb_auxiliary_username"];
            dbConnectionBuilder.Password = config["cisdb_auxiliary_password"];

            string auxiliaryDbConnectionString = dbConnectionBuilder.ConnectionString;
            string migrationsAssemblyName = typeof(Startup).Assembly.FullName;

            services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;

                options.UserInteraction.LoginUrl = "/access/authentication/signin";
                options.UserInteraction.LogoutUrl = "/access/authentication/signout";
                options.UserInteraction.LogoutIdParameter = "signOutId";
                options.UserInteraction.ConsentUrl = "/access/consent/index";
                options.UserInteraction.ErrorUrl = "/clientservices/correspondence/error";
            })
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionString,
                        o => o.MigrationsAssembly(migrationsAssemblyName)
                    );
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionString,
                        o => o.MigrationsAssembly(migrationsAssemblyName)
                    );
                })
                .AddAspNetIdentity<ApplicationUser>();

            return services;
        }
    }
}
