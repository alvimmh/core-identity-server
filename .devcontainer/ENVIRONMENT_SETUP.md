# Setting up development environment

OS: `Ubuntu Linux`

## Add Docker's official GPG key:

```
sudo apt-get update
sudo apt-get install ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
```

## Add the repository to Apt sources:

```
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
```

## Install Docker packages

To install a specific version of Docker Engine, list available versions:

`apt-cache madison docker-ce | awk '{ print $3 }'`

Currently, the version `5:26.1.2-1~ubuntu.22.04~jammy` is in use.

Then install Docker packages by

```
VERSION_STRING=5:26.1.2-1~ubuntu.22.04~jammy
sudo apt-get install docker-ce=$VERSION_STRING docker-ce-cli=$VERSION_STRING containerd.io docker-buildx-plugin docker-compose-plugin
```

## Add current user to docker group

Add your user to the docker group

`sudo usermod -aG docker $USER`

Sign out and back in again so your changes take effect.

## Verify installation

Run a hello-world container.

`sudo docker run hello-world`

# Installing the Dev Container CLI

You can quickly try out the CLI through the Dev Containers extension. Select the Dev Containers: Install devcontainer CLI command from the Command Palette (F1).

Then vscode will prompt to add it to the `PATH` variable. Simply copy and append it to the PATH inside
`~/.bashrc` file or any alternative file.

Restart the terminal window to take effect.

Verify the CLI installation by `devcontainer --version`. Currently, it is `0.59.1`.

# Notes

## Docker Compose version

Make sure to update Docker Compose path and Docker path inside vscode settings.

Currently, they are `docker compose` and `docker` respectively.

It is important to use the `docker compose` V2 cli from `docker-compose-plugin` instead of the `docker-compose` V1 cli.

## Docker context version

If you use the `devcontainer cli` and also have `docker desktop` application installed, you might
run into issues if the `docker context` changes or not in sync with the terminal and that of vscode.

It is also common to face "bind address already in use" errors due to this. To confirm, when you
run `devcontainer up` command, observe the terminal output to see which docker context is being used.
Alternatively, you can run `docker context ls` to see which context is active in the terminal.

Now, after creating the containers when you run `devcontainer open` command, observe the output or logs
in the `vscode` terminal starting the container. If you see vscode uses a different one, you can use your
terminal to change to the context that is being run in `vscode` and restart the docker processes.

See https://docs.docker.com/desktop/install/linux-install/#installing-docker-desktop-and-docker-engine
for more information.

## Docker contexts

Use command `docker context ls` to see which docker context is active.

Change to a context by using `docker context use <context_name>` command.

## References:

```
Setting up Dev Container: https://code.visualstudio.com/docs/devcontainers/containers

Docker Engine Installation: https://docs.docker.com/engine/install/debian/#install-using-the-repository

Dev Container CLI: https://code.visualstudio.com/docs/devcontainers/devcontainer-cli#_installation

Docker Desktop vs Docker Engine: https://docs.docker.com/desktop/install/linux-install/#installing-docker-desktop-and-docker-engine
```
