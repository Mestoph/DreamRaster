#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$ROOT/src/DreamRaster/DreamRaster.csproj"
OUT="$ROOT/src/DreamRaster/bin/Publish/Portable"

echo
echo "========================================================================"
echo "  DreamRaster - Linux publish -> Windows x64 single EXE"
echo "========================================================================"
echo "Root    : $ROOT"
echo "Project : $PROJECT"
echo

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR / ERREUR] .NET SDK 9 is required."
  exit 1
fi

rm -rf "$OUT"

dotnet restore "$PROJECT" \
  -r win-x64 \
  -p:EnableWindowsTargeting=true

dotnet publish "$PROJECT" \
  -c Release \
  -r win-x64 \
  --no-restore \
  -p:PublishProfile=Portable \
  -p:EnableWindowsTargeting=true \
  --nologo

rm -f \
  "$OUT/Microsoft.Web.WebView2.Core.xml" \
  "$OUT/Microsoft.Web.WebView2.WinForms.xml"

if [[ ! -f "$OUT/DreamRaster.exe" ]]; then
  echo "[ERROR / ERREUR] DreamRaster.exe was not produced."
  exit 1
fi

echo
echo "Publish output:"
find "$OUT" -maxdepth 1 -type f -printf '  %f\n' 2>/dev/null || ls -1 "$OUT"

extra_count="$(find "$OUT" -maxdepth 1 -type f ! -name 'DreamRaster.exe' | wc -l | tr -d ' ')"
if [[ "$extra_count" != "0" ]]; then
  echo
  echo "[WARNING / ATTENTION] Additional root files were produced."
fi

echo
echo "Windows executable ready:"
echo "  $OUT/DreamRaster.exe"
echo
echo "NOTE: The generated EXE targets Windows x64 and does not run natively on Linux."
