using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.Extensions.Configuration;
using Serilog;
using Npgsql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CoreIdentityServer.Internals.Data.Seeds.Auxiliary
{
    public class SeedPersistedGrantDatabase
    {
        public static void InitializeDatabase(IWebHostEnvironment environment, IConfiguration config)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddLogging();

            string auxiliaryDbConnectionStringRoot = null;

            if (environment.IsDevelopment())
                auxiliaryDbConnectionStringRoot = config.GetConnectionString("DevelopmentAuxiliary");
            else if (environment.IsProduction())
                auxiliaryDbConnectionStringRoot = config.GetConnectionString("ProductionAuxiliary");

            if (string.IsNullOrWhiteSpace(auxiliaryDbConnectionStringRoot))
                throw new NullReferenceException("Auxiliary database connection string is missing.");

            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                auxiliaryDbConnectionStringRoot
            );

            string auxiliaryDbUserName = config["cisdb_auxiliary_username"];
            string auxiliaryDbPassword = config["cisdb_auxiliary_password"];

            if (string.IsNullOrWhiteSpace(auxiliaryDbUserName) || string.IsNullOrWhiteSpace(auxiliaryDbPassword))
                throw new NullReferenceException("Auxiliary database credentials are missing.");

            dbConnectionBuilder.Username = auxiliaryDbUserName;
            dbConnectionBuilder.Password = auxiliaryDbPassword;

            string databaseConnectionString = dbConnectionBuilder.ConnectionString;
            string migrationsAssemblyName = typeof(Startup).Assembly.FullName;

            services.AddIdentityServer()
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        databaseConnectionString,
                        o => o.MigrationsAssembly(migrationsAssemblyName)
                    );
                });

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                using (IServiceScope scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();

                    context.Database.Migrate();

                    Log.Debug("Auxiliary persistedGrant database migrated.");
                }
            }
        }
    }
}
