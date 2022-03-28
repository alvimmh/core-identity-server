# Setup

## user-secrets

Initialize user-secrets by running command `dotnet user-secrets init --project src/CoreIdentityServer/` from the root directory.

Add required user-secrets from protected sources for the following keys:

1. "MailtrapSmtpEmailService:SmtpHost"
2. "MailtrapSmtpEmailService:SmtpPort"
3. "MailtrapSmtpEmailService:SmtpUsername"
4. "MailtrapSmtpEmailService:SmtpPassword"
5. "cisdb_username"
6. "cisdb_password"
7. "cisdb_auxiliary_username"
8. "cisdb_auxiliary_password"
9. "cisdb_signing_credential_private_key_passphrase"

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
 