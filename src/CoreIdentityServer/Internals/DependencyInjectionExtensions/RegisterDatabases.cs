using System;
using CoreIdentityServer.Internals.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterDatabases
    {
        public static IServiceCollection AddProjectDatabases(
            this IServiceCollection services,
            IWebHostEnvironment environment,
            IConfiguration configuration
        ) {
            string dbConnectionStringRoot = null;

            if (environment.IsDevelopment())
                dbConnectionStringRoot = configuration.GetConnectionString("DevelopmentMain");
            else if (environment.IsProduction())
                dbConnectionStringRoot = configuration.GetConnectionString("ProductionMain");

            if (string.IsNullOrWhiteSpace(dbConnectionStringRoot))
                throw new NullReferenceException("Main database connection string is missing.");

            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                dbConnectionStringRoot
            );

            string dbUserName = configuration["cisdb_username"];
            string dbPassword = configuration["cisdb_password"];

            if (string.IsNullOrWhiteSpace(dbUserName) || string.IsNullOrWhiteSpace(dbPassword))
                throw new NullReferenceException("Main database credentials are missing.");

            dbConnectionBuilder.Username = dbUserName;
            dbConnectionBuilder.Password = dbPassword;

            string databaseConnectionString = dbConnectionBuilder.ConnectionString;

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(databaseConnectionString, 
                    o => o.MigrationsAssembly(typeof(Startup).Assembly.FullName)));

            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }
    }
}
