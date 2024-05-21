# Setup the developing environment

Read and setup the environment following instructions in the `ENVIRONMENT_SETUP.md`
file.

# Setting up environment variables

Run the bash script generate_env_variables.sh with sudo.

`sudo bash .devcontainer/scripts/generate/generate_env_variables.sh`

This file must remain owned by the root user for read, write and execute permission
and the current user-group for read and write permission.

Running this file creates a `.env` file with the required information from the
`appsettings.Development.json` file.

Note, these two files should not be included in version control.

And the PostgreSQL environment variables may seem to be ignored but it's not the case:

https://github.com/docker-library/docs/blob/master/postgres/README.md#postgres_password

https://github.com/docker-library/docs/blob/master/postgres/README.md#postgres_db

# Setting up pgAdmin4 server connections

Run the bash script generate_pgadmin4_server_connections.sh with sudo.

`sudo bash .devcontainer/scripts/generate/generate_pgadmin4_server_connections.sh`

This file must remain owned by the root user for read, write and execute permission
and the current user-group for read and write permission.

Running this file creates a `servers.json` file with the required information from the
`appsettings.Development.json` file.

Note, these two files should not be included in version control.

Reference:

https://www.pgadmin.org/docs/pgadmin4/latest/container_deployment.html#mapped-files-and-directories

https://github.com/pgadmin-org/pgadmin4/blob/master/docs/en_US/import_export_servers.rst#json-format

# Setting up docker secrets

Run the bash script generate_secrets.sh with sudo.

`sudo bash .devcontainer/scripts/generate/generate_secrets.sh`

This file must remain owned by the root user for read, write and execute permission
and the current user-group for read and write permission.

Running this file creates docker secret files in the `.devcontainer/secrets` directory
with the required secrets from the `appsettings.Development.json` file.

Note, the contents of the directory `.devcontainer/secrets/` should not be included in version control.

Reference:

https://docs.docker.com/compose/use-secrets/

# Check if environment variables are interpolated

Run `docker compose config` inside the `.devcontainer` directory
and inspect the config to see if the environment variables are
interpolated correctly.

# Docker CLI Usage

List all containers: `docker container ls -a`

List all images: `docker image ls`

List all volumes: `docker volume ls`

List all networks: `docker network ls`

Start all containers: `docker container start $(docker container ls -a -q)`

Stop all containers: `docker container stop $(docker container ls -a -q)`

Remove all containers: `docker container rm $(docker container ls -a -q)`

Remove all images: `docker image rm $(docker image ls -q)`

Remove all volumes: `docker volume rm $(docker volume ls -q)`

Remove all networks: `docker network rm $(docker network ls -q)`

View Docker context: `docker context ls`

Change Docker context: `docker context use <context-name>`

Inspect a container: `docker inspect <container-id>`

Inspect Docker network: `docker inspect <network_id>`

# useful scripts

Run `bash .devcontainer/scripts/remove/delete_all_docker_units.sh` to delete all
docker units, such as, containers, images, volumes and networks.

Run `bash .devcontainer/scripts/remove/hard_delete_docker_units.sh` to delete all
docker units, such as, containers, images, volumes and networks, including all
docker builds.

Run `bash .devcontainer/scripts/remove/remove_pgadmin4_container.sh` to delete the
pgadmin4 container.

# Check if db instances are accessible

Using `pgAdmin4` web server, try connecting to the db instances using the credentials
used in `docker-compose.yml` file. There should be pre-configured servers inside the
pgadmin4 web server.

You can try to connect to them via the terminal
using command `psql -U <db-username> -h <db-host> -p <db-port> -d <db-name>`.

# Notes

Only use `docker compose` cli, not the `docker-compose` cli.

Make sure your terminal is using the same `docker context` as
`vscode`.

Ensure that the host machine firewall allows for the connections to
ports defined in `docker-compose.yml` file.

When the container is running, open a terminal in the container and run these commands
in the project root directory:

`whoami` - should be the default user, "vscode" in most cases
`ls -l` - all files and directories should be owned by the user "vscode" generally, not by "root"
`id` - should output "uid=1000(vscode) gid=1000(vscode) groups=1000(vscode),999(nvm)"

More instructions on `ENVIRONMENT_SETUP.md` file, see the `Note` section.
