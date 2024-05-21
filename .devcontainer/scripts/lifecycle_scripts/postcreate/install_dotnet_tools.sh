#!/bin/bash

# add development certificate, so the server runs
# note: it won't be trusted
#
# see here for more information
# https://stackoverflow.com/questions/55485511/how-to-run-dotnet-dev-certs-https-trust

echo $'\n\nInstalling HTTPS development certificate'
echo "<<##########################################>>"
dotnet dev-certs https
echo "<\##########################################/>"

echo $'\n\nUpdating dotnet workload'
echo "<<##########################################>>"
# update dotnet workload but this may be an issue with dotnet cli itself
# see: https://github.com/dotnet/sdk/issues/35128 for more information
sudo dotnet workload update
echo "<\##########################################/>"

echo $'\n\nInstalling dotnet-ef tool globally'
echo "<<##########################################>>"
dotnet tool install dotnet-ef -g
echo "<\##########################################/>"
