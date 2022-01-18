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
    public class SeedConfigurationDatabase
    {
        public static void EnsureSeedData(IConfiguration config)
        {            
            ServiceCollection services = new ServiceCollection();

            services.AddLogging();

            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                config.GetConnectionString("AuxiliaryDatabaseConnection")
            );

            dbConnectionBuilder.Username = config["cisdb_auxiliary_username"];
            dbConnectionBuilder.Password = config["cisdb_auxiliary_password"];

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
