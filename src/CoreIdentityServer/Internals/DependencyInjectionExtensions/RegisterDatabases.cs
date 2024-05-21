using System;
using System.Configuration;
using CoreIdentityServer.Internals.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    // registers the main database for the application
    public static class RegisterDatabases
    {
        public static IServiceCollection AddProjectDatabases(
            this IServiceCollection services,
            IConfiguration configuration
        ) {
            string mainDbHost = configuration["cis_main_database:host"];
            string mainDbName = configuration["cis_main_database:name"];
            int mainDbPort;

            bool isMainDbPortValid = int.TryParse(configuration["cis_main_database:port"], out mainDbPort);

            if (string.IsNullOrWhiteSpace(mainDbHost) ||
                string.IsNullOrWhiteSpace(mainDbName) ||
                !isMainDbPortValid
            ) {
                throw new NullReferenceException("Main database connection string is missing.");
            }

            string mainDbUserName = configuration["cis_main_database:username"];
            string mainDbPassword = configuration["cis_main_database:password"];

            if (string.IsNullOrWhiteSpace(mainDbUserName) || string.IsNullOrWhiteSpace(mainDbPassword))
                throw new NullReferenceException("Main database credentials are missing.");

            NpgsqlConnectionStringBuilder mainDbConnectionBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = mainDbHost,
                Port = mainDbPort,
                Database = mainDbName,
                Username = mainDbUserName,
                Password = mainDbPassword
            };

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    mainDbConnectionBuilder.ConnectionString,
                    o => o.MigrationsAssembly(typeof(Startup).Assembly.FullName)
                )
            );

            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }
    }
}
