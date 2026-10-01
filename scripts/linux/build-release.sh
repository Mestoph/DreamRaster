#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$ROOT/src/DreamRaster/DreamRaster.csproj"

echo "DreamRaster - Linux cross-build -> Windows x64 (Release)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR] .NET SDK 9 is required."
  exit 1
fi

dotnet restore "$PROJECT" \
  -r win-x64 \
  -p:EnableWindowsTargeting=true

dotnet build "$PROJECT" \
  -c Release \
  -r win-x64 \
  --no-restore \
  --nologo \
  -p:EnableWindowsTargeting=true
