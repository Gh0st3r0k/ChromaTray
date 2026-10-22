#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_ROOT="${CHROMATRAY_DOTNET_ROOT:-/workspace/.dotnet}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export PATH="$DOTNET_ROOT:$PATH"
cd "$project_root"
# Linux only compiles the Windows forms; the Windows CI job runs their tests.
dotnet build ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore
# Run the platform-independent algorithms using the same sources as the application.
dotnet test ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj -c Release --no-restore \
    --logger trx --results-directory artifacts/test-results/core
