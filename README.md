# Setup

## configure

Configure settings for the application and clients inside the `src/CoreIdentityServer/Config.cs` file. And set the ASPNETCORE_ENVIRONMENT (& applicationUrl for Development environment only) in the `src/CoreIdentityServer/Properties/launchSettings.json` file.

## secrets

Add required secrets from protected sources for the keys in appsettings.[environment].json based on the current environment.

* Note: The files `appsettings.Development.json` and `appsettings.Production.json` must never be included in the source control.

## signing key management

Download the RSA private key from confidential source for the appropirate environment, rename it to remove the .txt extension and place it in src/CoreIdentityServer/keys directory.

The contents of this directory are git ignored. The highest level of security and awareness must be maitained dealing with this key. Information on how to create this key is in the confidential source record.

## setup devcontainer

Setup the devcontainer with detailed instructions from the `.devcontainer/README.md` file.
Please read the file in order to correctly use the devcontainer.

## Dev Container CLI Usage

Once you have setup the vscode devcontainer environment, and you have installed the devcontainer cli,
run `devcontainer --help` to view all commands.

Create and run dev container: `devcontainer up`

Create and run dev container after deleting existing contianer: `devcontainer up --remove-existing-container`

Open the container in vscode: `devcontainer open`

Alternatively, you can alse use "Reopen in container" from vscode.

## initializing and seeding databases

These commands now run inside the root project directory of the primary container using the `postCreateCommand`
lifecycle script available in the `.devcontainer/scripts/lifecycle_scripts/postcreate` directory.

1. `dotnet ef database update --context ApplicationDbContext --project src/CoreIdentityServer/`
2. `dotnet ef database update --context ConfigurationDbContext --project src/CoreIdentityServer/`
3. `dotnet ef database update --context PersistedGrantDbContext --project src/CoreIdentityServer/`
4. `dotnet run seed --project src/CoreIdentityServer/`

# Run application

Start the project by running the command `dotnet watch run --project src/CoreIdentityServer/`
inside the root project directory of the primary container cis.

Visit https://localhost:5001 to see the running application.

You can also access the pgAdmin4 webserver from localhost:65050 and use the pgAdmin4 credentials to log in.
There should be prebuilt server configurations you can use to connect to the `cis_main_database` and
`cis_auxiliary_database` using credentials from the `appsettings.[env].json file.
