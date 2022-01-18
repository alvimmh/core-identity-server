using CoreIdentityServer.Internals.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterDatabases
    {
        public static IServiceCollection AddProjectDatabases(this IServiceCollection services, IConfiguration config)
        {
            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
            dbConnectionBuilder["Username"] = config["cisdb_username"];
            dbConnectionBuilder["Password"] = config["cisdb_password"];

            string databaseConnectionString = dbConnectionBuilder.ConnectionString;

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(databaseConnectionString, 
                    o => o.MigrationsAssembly(typeof(Startup).Assembly.FullName)));

            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }
    }
}
