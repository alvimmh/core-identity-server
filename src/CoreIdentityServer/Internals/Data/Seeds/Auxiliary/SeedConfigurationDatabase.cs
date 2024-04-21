using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.Extensions.Configuration;
using Serilog;
using Npgsql;
using Microsoft.AspNetCore.Hosting;

namespace CoreIdentityServer.Internals.Data.Seeds.Auxiliary
{
    // seeds the configuration database portion in the auxiliary database
    public class SeedConfigurationDatabase
    {
        public static void EnsureSeedData(IConfiguration config)
        {            
            ServiceCollection services = new ServiceCollection();

            services.AddLogging();

            string auxiliaryDbConnectionStringRoot = config["cis_auxiliary_db_connection_string"];

            if (string.IsNullOrWhiteSpace(auxiliaryDbConnectionStringRoot))
                throw new NullReferenceException("Auxiliary database connection string is missing.");

            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                auxiliaryDbConnectionStringRoot
            );

            string auxiliaryDbUserName = config["cis_auxiliary_db_username"];
            string auxiliaryDbPassword = config["cis_auxiliary_db_password"];

            if (string.IsNullOrWhiteSpace(auxiliaryDbUserName) || string.IsNullOrWhiteSpace(auxiliaryDbPassword))
                throw new NullReferenceException("Auxiliary database credentials are missing.");

            dbConnectionBuilder.Username = auxiliaryDbUserName;
            dbConnectionBuilder.Password = auxiliaryDbPassword;

            string databaseConnectionString = dbConnectionBuilder.ConnectionString;
            string migrationsAssemblyName = typeof(Startup).Assembly.FullName;

            services.AddIdentityServer()
                .AddConfigurationStore(options =>
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
                    var context = scope.ServiceProvider.GetService<ConfigurationDbContext>();

                    context.Database.Migrate();

                    Log.Debug("Auxiliary configuration database migrated.");

                    if (!context.Clients.Any())
                    {
                        foreach (Client client in Config.Clients)
                            context.Clients.Add(client.ToEntity());

                        context.SaveChanges();

                        Log.Debug("Seeded auxiliary configuration database Clients.");
                    }

                    if (!context.IdentityResources.Any())
                    {
                        foreach (IdentityResource identityResource in Config.IdentityResources)
                            context.IdentityResources.Add(identityResource.ToEntity());

                        context.SaveChanges();

                        Log.Debug("Seeded auxiliary configuration database IdentityResources.");
                    }

                    if (!context.ApiScopes.Any())
                    {
                        foreach (ApiScope apiScope in Config.ApiScopes)
                            context.ApiScopes.Add(apiScope.ToEntity());
                        
                        context.SaveChanges();

                        Log.Debug("Seeded auxiliary configuration database ApiScopes.");
                    }
                }
            }
        }
    }
}
