// Copyright (c) Duende Software. All rights reserved.
//
// By accessing the Duende IdentityServer code here, you are agreeing to the following licensing terms:
// https://duendesoftware.com/license/identityserver.pdf
//
// or, alternatively you can view the license in the project's ~/Licenses/DuendeSoftwareLicense.pdf file
//
// If you do not agree to these terms, do not access the Duende IdentityServer code.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterIdentityServer
    {
        // registers and configures Duende Identity Server as the identity server for the application
        public static IServiceCollection AddProjectIdentityServer(
            this IServiceCollection services,
            IWebHostEnvironment environment,
            IConfiguration configuration
        ) {
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

            string tokenSigningCredetialPrivateKeyPassphrase = configuration["cis_token_signing_credential_private_key_passphrase"];

            if (string.IsNullOrWhiteSpace(tokenSigningCredetialPrivateKeyPassphrase))
                throw new NullReferenceException("Duende Identity Server token signing credential private key passphrase is missing.");

            RsaSecurityKey tokenSigningCredentialPrivateKey = GenerateRSAPrivateKeyFromEncryptedPemFile(
                "keys/cis_sc_rsa_2048.pem",
                tokenSigningCredetialPrivateKeyPassphrase
            );

            string duendeIdentityServerLicenseKey = configuration["duende_identity_server_license_key"];

            if (string.IsNullOrWhiteSpace(duendeIdentityServerLicenseKey) && environment.IsProduction())
                throw new NullReferenceException("Duende Identity Server license key is missing.");

            services.AddIdentityServer(options =>
            {
                options.LicenseKey = duendeIdentityServerLicenseKey;

                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;

                // configure UserInteraction options
                options.UserInteraction.LoginUrl = "/access/authentication/signin";
                options.UserInteraction.LoginReturnUrlParameter = "returnurl";
                options.UserInteraction.LogoutUrl = "/access/authentication/signout";
                options.UserInteraction.LogoutIdParameter = "signoutid";
                options.UserInteraction.ConsentUrl = "/access/consent/index";
                options.UserInteraction.ConsentReturnUrlParameter = "returnurl";
                options.UserInteraction.ErrorUrl = "/clientservices/correspondence/error";
                options.UserInteraction.ErrorIdParameter = "errortype";

                // configure endpoints
                options.Endpoints.EnableBackchannelAuthenticationEndpoint = false;
                options.Endpoints.EnableCheckSessionEndpoint = false;
                options.Endpoints.EnableDeviceAuthorizationEndpoint = false;
                options.Endpoints.EnableIntrospectionEndpoint = false;
                options.Endpoints.EnableJwtRequestUri = false;
                options.Endpoints.EnableTokenRevocationEndpoint = false;

                // configure discovery endpoint data
                options.Discovery.ShowApiScopes = false;
                options.Discovery.ShowClaims = false;
                options.Discovery.ShowEndpoints = false;
                options.Discovery.ShowExtensionGrantTypes = false;
                options.Discovery.ShowGrantTypes = false;
                options.Discovery.ShowIdentityScopes = false;
                options.Discovery.ShowKeySet = true;
                options.Discovery.ShowResponseModes = false;
                options.Discovery.ShowResponseTypes = false;
                options.Discovery.ShowTokenEndpointAuthenticationMethods = false;

                // configure PersistedGrants options
                options.PersistentGrants.DataProtectData = true;

                // configure tenant validation on authorization
                options.ValidateTenantOnAuthorization = false;

                // configure mTLS options
                options.MutualTls.Enabled = false;

                // configure key management
                options.KeyManagement.Enabled = false;

                // cookie options are configured in src/CoreIdentityServer/Internals/DependencyInjectionExtensions/RegisterAuthentication.cs file
            })
                .AddSigningCredential(tokenSigningCredentialPrivateKey, SecurityAlgorithms.RsaSha256)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionBuilder.ConnectionString,
                        o => o.MigrationsAssembly(migrationsAssemblyName)
                    );
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = opt => opt.UseNpgsql(
                        auxiliaryDbConnectionBuilder.ConnectionString,
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
