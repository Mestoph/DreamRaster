#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DIAGNOSTIC="$ROOT/scripts/common/BuildDiagnostic.ps1"

if command -v pwsh >/dev/null 2>&1; then
  exec pwsh \
    -NoLogo \
    -NoProfile \
    -File "$DIAGNOSTIC" \
    -Configuration Release \
    -Project "src/DreamRaster/DreamRaster.csproj" \
    -NoOpenReport
fi

echo "[INFO] PowerShell 7 (pwsh) is not installed."
echo "The full HTML diagnostic requires pwsh."
echo "Falling back to the standard Linux cross-build."
exec "$ROOT/scripts/linux/build-release.sh"
