#!/bin/bash

script_file_path=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)

bash $script_file_path/check_devcontainer_setup.sh

bash $script_file_path/install_dotnet_tools.sh

bash $script_file_path/initialize_databases.sh
