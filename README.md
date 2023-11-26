# Setup

## configure

Configure settings for the application and clients inside the `src/CoreIdentityServer/Config.cs` file. And set the ASPNETCORE_ENVIRONMENT (& applicationUrl for Development environment only) in the `src/CoreIdentityServer/Properties/launchSettings.json` file.

## secrets

Add required secrets from protected sources for the keys in appsettings.[environment].json based on the current environment.

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
 