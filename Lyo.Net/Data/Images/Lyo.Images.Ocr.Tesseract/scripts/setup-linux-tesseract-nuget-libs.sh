#!/usr/bin/env bash
# Linux native libs for charlesw/Tesseract NuGet:
# - Leptonica/Tesseract: InteropDotNet looks under $(OutputPath)x64/ (charlesw/tesseract#687).
# - libdl: loader asks for "libdl.so"; glibc only ships libdl.so.2 — symlink beside the app output.
#
# Apt deps (example):
#   sudo apt-get update && sudo apt-get install -y libtesseract5 libleptonica-dev tesseract-ocr-eng
#
# Usage (pass the **TFM output folder**, i.e. .../bin/Debug/net10.0 — not .../x64):
#   bash scripts/setup-linux-tesseract-nuget-libs.sh /path/to/bin/Debug/net10.0
#   bash scripts/setup-linux-tesseract-nuget-libs.sh /path/to/bin/Debug/net10.0 --also-system
#
# If you still pass .../x64, it is treated as .../net10.0 (parent).
#
# System copies only (sudo): --also-system
#
# Environment:
#   ARCH_LIBDIR  default /usr/lib/$(uname -m)-linux-gnu

set -euo pipefail

ARCH="$(uname -m)"
ARCH_LIBDIR="${ARCH_LIBDIR:-/usr/lib/${ARCH}-linux-gnu}"

resolve_leptonica() {
  local f
  for f in "$ARCH_LIBDIR/libleptonica.so" "$ARCH_LIBDIR/libleptonica.so.6" "$ARCH_LIBDIR/liblept.so.5"; do
    if [[ -e "$f" ]]; then
      readlink -f "$f"
      return 0
    fi
  done
  return 1
}

resolve_tesseract() {
  local f
  for f in "$ARCH_LIBDIR/libtesseract.so" "$ARCH_LIBDIR/libtesseract.so.5"; do
    if [[ -e "$f" ]]; then
      readlink -f "$f"
      return 0
    fi
  done
  return 1
}

resolve_libdl() {
  local d
  for d in "/lib/${ARCH}-linux-gnu" "/usr/lib/${ARCH}-linux-gnu"; do
    if [[ -e "$d/libdl.so.2" ]]; then
      readlink -f "$d/libdl.so.2"
      return 0
    fi
  done
  return 1
}

symlink_pair_into_x64() {
  local dest_dir="$1"
  mkdir -p "$dest_dir"
  ln -sf "$LEP" "$dest_dir/libleptonica-1.82.0.so"
  ln -sf "$TES" "$dest_dir/libtesseract50.so"
  echo "Symlinked into $dest_dir:"
  ls -la "$dest_dir/libleptonica-1.82.0.so" "$dest_dir/libtesseract50.so"
}

OUTDIR=""
ALSO_SYSTEM=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --also-system)
      ALSO_SYSTEM=true
      shift
      ;;
    -h|--help)
      sed -n '1,28p' "$0" | sed 's/^# \{0,1\}//'
      exit 0
      ;;
    -*)
      echo "error: unknown option: $1" >&2
      exit 1
      ;;
    *)
      if [[ -n "$OUTDIR" ]]; then
        echo "error: only one output directory allowed (got extra: $1)" >&2
        exit 1
      fi
      OUTDIR="$1"
      shift
      ;;
  esac
done

[[ -d "$ARCH_LIBDIR" ]] || {
  echo "error: missing $ARCH_LIBDIR" >&2
  exit 1
}

LEP="$(resolve_leptonica)" || {
  echo "error: no Leptonica library under $ARCH_LIBDIR (install libleptonica-dev or libleptonica6)." >&2
  exit 1
}

TES="$(resolve_tesseract)" || {
  echo "error: no libtesseract under $ARCH_LIBDIR (install libtesseract5)." >&2
  exit 1
}

DL="$(resolve_libdl)" || {
  echo "error: no libdl.so.2 under /lib or /usr/lib for ${ARCH}-linux-gnu." >&2
  exit 1
}

echo "Using Leptonica: $LEP"
echo "Using Tesseract: $TES"
echo "Using libdl:     $DL"

if [[ -n "$OUTDIR" ]]; then
  OUT_ROOT="$OUTDIR"
  if [[ "$OUT_ROOT" == */x64 ]]; then
    OUT_ROOT="${OUT_ROOT%/x64}"
    echo "note: normalized output path (use TFM folder, not x64): $OUT_ROOT"
  fi
  symlink_pair_into_x64 "$OUT_ROOT/x64"
  ln -sf "$DL" "$OUT_ROOT/libdl.so"
  echo "libdl -> $OUT_ROOT/libdl.so"
  ls -la "$OUT_ROOT/libdl.so"
fi

if [[ "$ALSO_SYSTEM" == true ]]; then
  DEST="${DEST:-/usr/local/lib}"
  echo "Also linking into $DEST (requires sudo)..."
  sudo mkdir -p "$DEST"
  sudo ln -sf "$LEP" "$DEST/libleptonica-1.82.0.so"
  sudo ln -sf "$TES" "$DEST/libtesseract50.so"
  sudo ldconfig
  DL_DIR="$(dirname "$DL")"
  echo "Also libdl.so beside system libdl.so.2 ($DL_DIR)..."
  sudo ln -sf "$DL" "$DL_DIR/libdl.so"
  echo "System copy done."
fi

if [[ -z "$OUTDIR" ]] && [[ "$ALSO_SYSTEM" != true ]]; then
  echo "" >&2
  echo "error: pass your app's **TFM output directory** (e.g. .../bin/Debug/net10.0)." >&2
  echo "from repo root (after dotnet build):" >&2
  echo "  $0 \"\$PWD/Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract.Tests/bin/Debug/net10.0\"" >&2
  echo "from scripts/:" >&2
  echo "  $0 \"\$PWD/../Lyo.Images.Ocr.Tesseract.Tests/bin/Debug/net10.0\"" >&2
  echo "" >&2
  echo "Optional: $0 .../net10.0 --also-system" >&2
  exit 1
fi

echo "Done."
