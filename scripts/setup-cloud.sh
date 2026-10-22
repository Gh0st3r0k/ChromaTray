#!/usr/bin/env bash
set -euo pipefail

# Устанавливает проверенный SDK в workspace и восстанавливает зафиксированные зависимости.
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_ROOT="${CHROMATRAY_DOTNET_ROOT:-/workspace/.dotnet}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_NOLOGO=1
export PATH="$DOTNET_ROOT:$PATH"

sdk_version=8.0.426
# Официальный SHA-512 архива SDK: проверяется до распаковки.
sdk_sha512=8141d01bdc53f604aed3bb661b604707a64209f8a947cd81bf95b4f518951593d77864069191f54ea5e84362a409da6b0f69074bf893b283d7e0cf93e2bf20a8
if [[ ! -x "$DOTNET_ROOT/dotnet" ]] || ! "$DOTNET_ROOT/dotnet" --list-sdks | grep -q "^${sdk_version} "; then
    download_dir="$(mktemp -d)"
    trap 'rm -rf -- "$download_dir"' EXIT
    curl --fail --location --silent --show-error --retry 2 \
        "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdk_version/dotnet-sdk-$sdk_version-linux-x64.tar.gz" \
        --output "$download_dir/sdk.tar.gz"
    (cd "$download_dir" && printf '%s  sdk.tar.gz\n' "$sdk_sha512" | sha512sum --check --status)
    mkdir -p "$DOTNET_ROOT"
    tar -xzf "$download_dir/sdk.tar.gz" -C "$DOTNET_ROOT"
fi
cd "$project_root"
dotnet restore ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj --locked-mode
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
