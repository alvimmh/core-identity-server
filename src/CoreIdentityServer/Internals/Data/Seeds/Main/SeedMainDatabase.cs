// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Linq;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Configuration;
using Npgsql;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Authorization;

namespace CoreIdentityServer.Internals.Data.Seeds.Main
{
    public class SeedMainDatabase
    {
        public static void EnsureSeedData(IConfiguration config)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();

            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                config.GetConnectionString("MainDatabaseConnection")
            );

            dbConnectionBuilder.Username = config["cisdb_username"];
            dbConnectionBuilder.Password = config["cisdb_password"];

            string databaseConnectionString = dbConnectionBuilder.ConnectionString;
            string migrationsAssemblyName = typeof(Startup).Assembly.FullName;

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    databaseConnectionString,
                    o => o.MigrationsAssembly(migrationsAssemblyName)
                )
            );

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {               
                using (IServiceScope scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    ApplicationDbContext DbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

                    DbContext.Database.Migrate();

                    UserManager<ApplicationUser> UserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    
                    string productOwnerEmail = config["product_owner_email"];

                    ApplicationUser productOwner = UserManager.FindByEmailAsync(productOwnerEmail).Result;

                    if (productOwner == null)
                    {
                        productOwner = new ApplicationUser
                        {
                            Email = productOwnerEmail,
                            EmailConfirmed = true,
                            UserName = productOwnerEmail,
                            FirstName = "",
                            LastName = "",
                            TwoFactorEnabled = true,
                            AccountRegistered = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        IdentityResult createProductOwner = UserManager.CreateAsync(productOwner).Result;

                        if (!createProductOwner.Succeeded)
                        {
                            throw new Exception(createProductOwner.Errors.First().Description);
                        }

                        Log.Information($"Seeded {AuthorizedRoles.ProductOwner}.");

                        string productOwnerTOTPAccessRecoveryCode = UserManager.GenerateNewTwoFactorRecoveryCodesAsync(productOwner, 1).Result.FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(productOwnerTOTPAccessRecoveryCode))
                            throw new Exception($"Could not create TOTP Access recovery code for {AuthorizedRoles.ProductOwner.ToLower()}.");

                        SMTPService SMTPService = new SMTPService(config);
                        EmailService EmailService = new EmailService(config, DbContext, SMTPService);

                        EmailService.SendProductOwnerTOTPAccessRecoveryCodeEmail(
                            AutomatedEmails.NoReply,
                            productOwnerEmail,
                            productOwnerEmail,
                            productOwnerTOTPAccessRecoveryCode
                        );

                        Log.Information($"Sent email notification to {AuthorizedRoles.ProductOwner}.");
                    }
                    else
                    {
                        Log.Debug($"{AuthorizedRoles.ProductOwner} already exists.");
                    }

                    RoleManager<IdentityRole> RoleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    IdentityRole productOwnerRole = RoleManager.FindByNameAsync(AuthorizedRoles.ProductOwner).Result;

                    if (productOwnerRole == null)
                    {
                        productOwnerRole = new IdentityRole(AuthorizedRoles.ProductOwner);

                        IdentityResult createProductOwnerRole = RoleManager.CreateAsync(productOwnerRole).Result;

                        if (!createProductOwnerRole.Succeeded)
                        {
                            throw new Exception(createProductOwnerRole.Errors.First().Description);
                        }

                        Log.Information($"Seeded {AuthorizedRoles.ProductOwner} role.");
                    }
                    else
                    {
                        Log.Debug($"{AuthorizedRoles.ProductOwner} role already exists.");
                    }

                    bool isProductOwnerAlreadyAssignedToCorrespondingRole = UserManager.IsInRoleAsync(productOwner, AuthorizedRoles.ProductOwner).Result;

                    if (isProductOwnerAlreadyAssignedToCorrespondingRole)
                    {
                        Log.Debug($"{AuthorizedRoles.ProductOwner} already assigned to corresponding role.");
                    }
                    else
                    {
                        IdentityResult assignProductOwnerToCorrespondingRole = UserManager.AddToRoleAsync(productOwner, AuthorizedRoles.ProductOwner).Result;

                        if (!assignProductOwnerToCorrespondingRole.Succeeded)
                        {
                            throw new Exception($"Could not assign {AuthorizedRoles.ProductOwner} user to corresponding role.");
                        }

                        Log.Information($"Assigned {AuthorizedRoles.ProductOwner} to corresponding role.");
                    }

                    Log.Information("Seeded main database.");
                }
            }
        }
    }
}
