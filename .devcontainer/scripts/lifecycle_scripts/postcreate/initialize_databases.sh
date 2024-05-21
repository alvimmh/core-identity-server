#!/bin/bash

echo $'\n\nInitializing cis_main_database'
echo "<<##########################################>>"
dotnet ef database update --context ApplicationDbContext --project src/CoreIdentityServer/
echo "<\##########################################/>"

echo $'\n\nInitializing cis_auxiliary_database'
echo "<<##########################################>>"
dotnet ef database update --context ConfigurationDbContext --project src/CoreIdentityServer/

echo $'\nDone with ConfigurationDbContext, continuing to PersistedGrantDbContext\n'

dotnet ef database update --context PersistedGrantDbContext --project src/CoreIdentityServer/
echo "<\##########################################/>"

echo $'\n\nSeeding databases'
echo "<<##########################################>>"
dotnet run seed --project src/CoreIdentityServer/
echo "<\##########################################/>"
