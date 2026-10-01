#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$ROOT/src/DreamRaster/DreamRaster.csproj"

echo
echo "========================================================================"
echo "  DreamRaster - Linux cross-build -> Windows x64 (Debug)"
echo "========================================================================"
echo "Root    : $ROOT"
echo "Project : $PROJECT"
echo

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR / ERREUR] .NET SDK was not found in PATH."
  echo "Install .NET SDK 9, then retry."
  exit 1
fi

if [[ ! -f "$PROJECT" ]]; then
  echo "[ERROR / ERREUR] Project not found: $PROJECT"
  exit 1
fi

rm -rf "$ROOT/src/DreamRaster/bin" "$ROOT/src/DreamRaster/obj"

dotnet restore "$PROJECT" \
  -r win-x64 \
  -p:EnableWindowsTargeting=true

dotnet build "$PROJECT" \
  -c Debug \
  -r win-x64 \
  --no-restore \
  --nologo \
  -p:EnableWindowsTargeting=true

echo
echo "Build completed."
echo "Windows output:"
echo "  $ROOT/src/DreamRaster/bin/Debug/net9.0-windows/win-x64/"
echo
echo "NOTE: This is a Windows WinForms target. It cannot run natively on Linux."
