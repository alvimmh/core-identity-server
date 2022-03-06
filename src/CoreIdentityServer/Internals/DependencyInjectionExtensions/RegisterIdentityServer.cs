using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterIdentityServer
    {
        public static IServiceCollection AddProjectIdentityServer(this IServiceCollection services, IConfiguration config)
        {
            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                config.GetConnectionString("AuxiliaryDatabaseConnection")
            );

            dbConnectionBuilder.Username = config["cisdb_auxiliary_username"];
            dbConnectionBuilder.Password = config["cisdb_auxiliary_password"];

            string auxiliaryDbConnectionString = dbConnectionBuilder.ConnectionString;
            string migrationsAssemblyName = typeof(Startup).Assembly.FullName;

            RsaSecurityKey tokenSigningCredentialPrivateKey = GenerateRSAPrivateKeyFromEncryptedPemFile(
                "keys/cis_sc_rsa_2048.pem",
                config["cisdb_signing_credential_private_key_passphrase"]
            );

            services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;

                options.UserInteraction.LoginUrl = "/access/authentication/signin";
                options.UserInteraction.LogoutUrl = "/access/authentication/signout";
                options.UserInteraction.LogoutIdParameter = "signOutId";
                options.UserInteraction.ConsentUrl = "/access/consent/index";
                options.UserInteraction.ErrorUrl = "/clientservices/correspondence/error";

                options.KeyManagement.Enabled = false;
            })
                .AddSigningCredential(tokenSigningCredentialPrivateKey, SecurityAlgorithms.RsaSha256)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionString,
                        o => o.MigrationsAssembly(migrationsAssemblyName)
                    );
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionString,
                        o => o.MigrationsAssembly(migrationsAssemblyName)
                    );
                })
                .AddAspNetIdentity<ApplicationUser>();

            return services;
        }

        // use this method to import an encrypted private key from PEM file and generate a RsaSecurityKey object
        private static RsaSecurityKey GenerateRSAPrivateKeyFromEncryptedPemFile(string fileLocation, string passphrase)
        {
            RSA RSAKey = RSA.Create();

            IEnumerable<string> RSAKeyLinesWithoutLabels = File.ReadAllLines(fileLocation).Where(line => !line.StartsWith("-"));
            string RSAKeyStringWithoutLabels = string.Join(null, RSAKeyLinesWithoutLabels);
            byte[] RSAKeyBytes = Convert.FromBase64String(RSAKeyStringWithoutLabels);
            ReadOnlySpan<byte> RSAKeyBytesReadOnlySpan = new ReadOnlySpan<byte>(RSAKeyBytes);

            ReadOnlySpan<char> RSAKeyPassphrase = passphrase.AsSpan();

            RSAKey.ImportEncryptedPkcs8PrivateKey(RSAKeyPassphrase, RSAKeyBytesReadOnlySpan, out int readBytes);

            return new RsaSecurityKey(RSAKey);
        }

        // use this method to import a public key from PEM file and generate a RsaSecurityKey object
        private static RsaSecurityKey GenerateRSAPublicKeyFromPemFile(string fileLocation)
        {
            RSA RSAKey = RSA.Create();
            
            IEnumerable<string> RSAKeyLinesWithoutLabels = File.ReadAllLines(fileLocation).Where(line => !line.StartsWith("-"));
            string RSAKeyStringWithoutLabels = string.Join(null, RSAKeyLinesWithoutLabels);
            byte[] RSAKeyBytes = Convert.FromBase64String(RSAKeyStringWithoutLabels);
            ReadOnlySpan<byte> RSAKeyBytesReadOnlySpan = new ReadOnlySpan<byte>(RSAKeyBytes);

            RSAKey.ImportSubjectPublicKeyInfo(RSAKeyBytesReadOnlySpan, out int readBytes);

            return new RsaSecurityKey(RSAKey);
        }
    }
}
