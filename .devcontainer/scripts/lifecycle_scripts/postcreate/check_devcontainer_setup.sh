#!/bin/bash

echo $'\n\nCheck file permissions'
echo "<<##########################################>>"
echo "Current user: $(whoami), $(id)"
echo $'\nls -l\n'
ls -l
echo "<\##########################################/>"


echo $'\n\nUpdate apt packages index'
echo "<<##########################################>>"
sudo apt update
echo "<\##########################################/>"


echo $'\n\nInstall iputils-ping package to ping containers'
echo "<<##########################################>>"
sudo apt install iputils-ping
echo "<\##########################################/>"

echo $'\n\nSource nvm'
echo "<<##########################################>>"
# source nvm as it is required
# See: https://github.com/devcontainers/features/tree/main/src/node
source ${NVM_DIR}/nvm.sh

echo "nvm version: $(nvm -v)"
echo "<\##########################################/>"

# check executables

echo $'\n\nChecking bash executable'
echo "<<##########################################>>"
which bash
echo "<\##########################################/>"


echo $'\n\nChecking ping executable'
echo "<<##########################################>>"
which ping
echo "<\##########################################/>"


echo $'\n\nChecking dotnet executable'
echo "<<##########################################>>"
which dotnet
echo "<\##########################################/>"


echo $'\n\nSelecting node version and checking node executable'
echo "<<##########################################>>"
echo "Select node version 20.13.1"
# use node version 20.13.1
nvm use 20.13.1

echo "Check node executable"
which node
echo "<\##########################################/>"

# check if other containers are reachable

echo $'\n\nPing cis_main_db container'
echo "<<##########################################>>"
ping cis_main_db -c 1
echo "<\##########################################/>"


echo $'\n\nPing cis_auxiliary_db container'
echo "<<##########################################>>"
ping cis_auxiliary_db -c 1
echo "<\##########################################/>"
