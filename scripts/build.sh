#!/usr/bin/env bash
# Restores, builds, and tests the OS-agnostic Penny projects
# (Penny.Core, Penny.Security, Penny.Protocol, Penny.Network, and their
# test projects). Windows-only projects (Penny.Agent, Penny.Controller,
# Penny.Capture, Penny.Input) are added to this script once they exist.
set -euo pipefail

cd "$(dirname "$0")/.."

echo "== dotnet restore =="
dotnet restore Penny.sln

echo "== dotnet build =="
dotnet build Penny.sln --configuration Release --no-restore

echo "== dotnet test =="
dotnet test Penny.sln --configuration Release --no-build

echo "All good."
