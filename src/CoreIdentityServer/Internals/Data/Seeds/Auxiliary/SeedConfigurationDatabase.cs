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

namespace CoreIdentityServer.Internals.Data.Seeds.Auxiliary
{
    // seeds the configuration database portion in the auxiliary database
    public class SeedConfigurationDatabase
    {
        public static void EnsureSeedData(IConfiguration configuration)
        {            
            ServiceCollection services = new ServiceCollection();

            services.AddLogging();

            string auxiliaryDbHost = configuration["cis_auxiliary_database:host"];
            string auxiliaryDbName = configuration["cis_auxiliary_database:name"];
            int auxiliaryDbPort;

            bool isAuxiliaryDbPortValid = int.TryParse(configuration["cis_auxiliary_database:port"], out auxiliaryDbPort);

            if (string.IsNullOrWhiteSpace(auxiliaryDbHost) ||
                !isAuxiliaryDbPortValid ||
                string.IsNullOrWhiteSpace(auxiliaryDbName)
            ) {
                throw new NullReferenceException("Auxiliary database connection string is missing.");
            }

            string auxiliaryDbUserName = configuration["cis_auxiliary_database:username"];
            string auxiliaryDbPassword = configuration["cis_auxiliary_database:password"];

            if (string.IsNullOrWhiteSpace(auxiliaryDbUserName) || string.IsNullOrWhiteSpace(auxiliaryDbPassword))
                throw new NullReferenceException("Auxiliary database credentials are missing.");

            NpgsqlConnectionStringBuilder auxiliaryDbConnectionBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = auxiliaryDbHost,
                Port = auxiliaryDbPort,
                Database = auxiliaryDbName,
                Username = auxiliaryDbUserName,
                Password = auxiliaryDbPassword
            };

            string migrationsAssemblyName = typeof(Startup).Assembly.FullName;

            services.AddIdentityServer()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionBuilder.ConnectionString,
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
