#!/usr/bin/env bash
# Restores, builds, and tests the OS-agnostic Penny projects
# (Penny.Core, Penny.Security, Penny.Protocol, Penny.Network, Penny.Agent,
# Penny.Controller, and test projects). Penny.Capture and Penny.Input are
# added once they exist.
set -euo pipefail

cd "$(dirname "$0")/.."

echo "== dotnet restore =="
dotnet restore Penny.sln

echo "== dotnet build =="
dotnet build Penny.sln --configuration Release --no-restore

echo "== dotnet test =="
dotnet test Penny.sln --configuration Release --no-build

echo "All good."
