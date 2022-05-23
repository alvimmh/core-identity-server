# Setup

## configure

Configure settings for the application and clients inside the `src/CoreIdentityServer/Config.cs` file.

## user-secrets

Initialize user-secrets by running command `dotnet user-secrets init --project src/CoreIdentityServer/` from the repository root directory.

Add required user-secrets from protected sources for the following keys:

1. "product_owner_email"
2. "MailtrapSmtpEmailService:SmtpHost"
3. "MailtrapSmtpEmailService:SmtpPort"
4. "MailtrapSmtpEmailService:SmtpUsername"
5. "MailtrapSmtpEmailService:SmtpPassword"
6. "cisdb_username"
7. "cisdb_password"
8. "cisdb_auxiliary_username"
9. "cisdb_auxiliary_password"
10. "cis_token_signing_credential_private_key_passphrase"
11. "captcha_encryption_key"
12. "cis_main_db_connection_string"
13. "cis_auxiliary_db_connection_string"

Use command `dotnet user-secrets set "{key}" '{value}' --project src/CoreIdentityServer/` for setting the user secrets.

## signing key management

Download the RSA private key from LastPass, rename it to remove the .txt extension and place it in src/CoreIdentityServer/keys directory.

The contents of this directory are git ignored. The highest level of security and awareness must be maitained dealing with this key. Information on how to create this key is in the LastPass record.

## initialize databases

Run the following commands inside the root directory to initialize & seed the required databases:

1. `dotnet ef database update --context ApplicationDbContext --project src/CoreIdentityServer/`
2. `dotnet ef database update --context ConfigurationDbContext --project src/CoreIdentityServer/`
3. `dotnet ef database update --context PersistedGrantDbContext --project src/CoreIdentityServer/`
4. `dotnet user-secrets -p src/CoreIdentityServer/ set "product_owner_email" "set product owner email here"`
4. `dotnet run \/seed --project src/CoreIdentityServer/`

## run project

Start the project by running command `dotnet watch run --project src/CoreIdentityServer/`.
 