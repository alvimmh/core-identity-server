# Setup

## configure

Configure settings for the application and clients inside the `src/CoreIdentityServer/Config.cs` file. And set the ASPNETCORE_ENVIRONMENT (& applicationUrl for Development environment only) in the `src/CoreIdentityServer/Properties/launchSettings.json` file.

## secrets

Add required secrets from protected sources for the following keys into appsettings.Development.json or appsettings.Production.json based on the current environment:

1. "product_owner_email"
2. "https_port
3. "smtp_host"
4. "smtp_port"
5. "smtp_username"
6. "smtp_password"
7. "captcha_encryption_key"
8. "cis_main_db_connection_string"
9. "cis_main_db_username"
10. "cis_main_db_password"
11. "cis_auxiliary_db_connection_string"
12. "cis_auxiliary_db_username"
13. "cis_auxiliary_db_password"
14. "cis_token_signing_credential_private_key_passphrase" 
15. "duende_identity_server_license_key" for production
16. "static_configuration": {
        "teamadha_backend_client_secret"
        "teamadha_frontend_client_secret"
    }

* Note: The files `appsettings.Development.json` and `appsettings.Production.json` must never be included in the source control.

## signing key management

Download the RSA private key from LastPass for the appropirate environment, rename it to remove the .txt extension and place it in src/CoreIdentityServer/keys directory.

The contents of this directory are git ignored. The highest level of security and awareness must be maitained dealing with this key. Information on how to create this key is in the LastPass record.

## initialize databases

Run the following commands inside the root directory to initialize & seed the required databases:

1. `dotnet ef database update --context ApplicationDbContext --project src/CoreIdentityServer/`
2. `dotnet ef database update --context ConfigurationDbContext --project src/CoreIdentityServer/`
3. `dotnet ef database update --context PersistedGrantDbContext --project src/CoreIdentityServer/`
4. `dotnet run seed --project src/CoreIdentityServer/`

## run project

Start the project by running command `dotnet watch run --project src/CoreIdentityServer/`.
 