#!/bin/bash
SCRIPT_DIR="$(pwd)"
cd websmtp && dotnet publish -o ../build
cd $SCRIPT_DIR
cp -v ./appSettings.Production.json ./build/
docker build .