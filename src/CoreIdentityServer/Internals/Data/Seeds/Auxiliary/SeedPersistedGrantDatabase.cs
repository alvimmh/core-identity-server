using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.Extensions.Configuration;
using Serilog;
using Npgsql;

namespace CoreIdentityServer.Internals.Data.Seeds.Auxiliary
{
    public class SeedPersistedGrantDatabase
    {
        public static void InitializeDatabase(IConfiguration config)
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
