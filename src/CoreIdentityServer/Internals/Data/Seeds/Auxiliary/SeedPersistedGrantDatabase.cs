using System;
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
        // migrates the persisted grants database portion in the auxiliary database
        public static void InitializeDatabase(IConfiguration configuration)
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
                .AddOperationalStore(options =>
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
                    var context = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();

                    context.Database.Migrate();

                    Log.Debug("Auxiliary persistedGrant database migrated.");
                }
            }
        }
    }
}
