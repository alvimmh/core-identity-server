#!/bin/bash

# don't use this script unless you must

# stop all containers
docker container stop $(docker container ls -a -q)

# remove all containers
docker container rm $(docker container ls -a -q)

# remove all images
docker image rm $(docker image ls -q)

# remove all volumes
docker volume rm $(docker volume ls -q)

# remove all networks
docker network rm $(docker network ls -q)

# delete all builds
docker buildx prune -a
