#!/bin/bash
SCRIPT_DIR="$(pwd)"
cd websmtp && dotnet publish -o ../build
cd $SCRIPT_DIR
docker build -t yvansolutions/websmtp:latest .