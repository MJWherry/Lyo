#!/usr/bin/env python3
"""Linux native libs for charlesw/Tesseract NuGet.

- Leptonica/Tesseract: InteropDotNet looks under $(OutputPath)x64/ (charlesw/tesseract#687).
- libdl: loader asks for "libdl.so"; glibc only ships libdl.so.2 — symlink beside the app output.

Apt deps (example):
  sudo apt-get update && sudo apt-get install -y libtesseract5 libleptonica-dev tesseract-ocr-eng

Usage (pass the **TFM output folder**, i.e. .../bin/Debug/net10.0 — not .../x64):
  python3 scripts/setup_linux_tesseract_nuget_libs.py /path/to/bin/Debug/net10.0
  python3 scripts/setup_linux_tesseract_nuget_libs.py /path/to/bin/Debug/net10.0 --also-system
  python3 scripts/setup_linux_tesseract_nuget_libs.py /path/to/bin/Debug/net10.0 --allow-missing

If you still pass .../x64, it is treated as .../net10.0 (parent).

Environment:
  ARCH_LIBDIR  default /usr/lib/$(uname -m)-linux-gnu
"""

from __future__ import annotations

import argparse
import os
import platform
import subprocess
import sys
from pathlib import Path


def _realpath(path: Path) -> Path:
    return path.resolve()


def resolve_leptonica(arch_libdir: Path) -> Path | None:
    for name in ("libleptonica.so", "libleptonica.so.6", "liblept.so.5"):
        candidate = arch_libdir / name
        if candidate.exists():
            return _realpath(candidate)
    return None


def resolve_tesseract(arch_libdir: Path) -> Path | None:
    for name in ("libtesseract.so", "libtesseract.so.5"):
        candidate = arch_libdir / name
        if candidate.exists():
            return _realpath(candidate)
    return None


def resolve_libdl(arch: str) -> Path | None:
    for base in (Path(f"/lib/{arch}-linux-gnu"), Path(f"/usr/lib/{arch}-linux-gnu")):
        candidate = base / "libdl.so.2"
        if candidate.exists():
            return _realpath(candidate)
    return None


def report_missing(message: str, allow_missing: bool) -> int:
    kind = "warning" if allow_missing else "error"
    print(f"{kind}: {message}", file=sys.stderr)
    if allow_missing:
        print("Skipping native lib setup.", file=sys.stderr)
        return 0
    return 1


def symlink_pair_into_x64(dest_dir: Path, lep: Path, tes: Path) -> None:
    dest_dir.mkdir(parents=True, exist_ok=True)
    lep_link = dest_dir / "libleptonica-1.82.0.so"
    tes_link = dest_dir / "libtesseract50.so"
    if lep_link.exists() or lep_link.is_symlink():
        lep_link.unlink()
    if tes_link.exists() or tes_link.is_symlink():
        tes_link.unlink()
    lep_link.symlink_to(lep)
    tes_link.symlink_to(tes)
    print(f"Symlinked into {dest_dir}:")
    for p in (lep_link, tes_link):
        print(f"  {p} -> {p.readlink()}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("outdir", nargs="?", help="TFM output directory (.../bin/Debug/net10.0)")
    parser.add_argument("--also-system", action="store_true", help="Also link into /usr/local/lib (sudo)")
    parser.add_argument(
        "--allow-missing",
        action="store_true",
        help="Exit 0 if distro Leptonica/Tesseract are not installed (AfterBuild / compile-only CI)",
    )
    args = parser.parse_args(argv)
    allow_missing = args.allow_missing and not args.also_system

    arch = platform.machine()
    arch_libdir = Path(os.environ.get("ARCH_LIBDIR", f"/usr/lib/{arch}-linux-gnu"))
    if not arch_libdir.is_dir():
        return report_missing(f"missing {arch_libdir}", allow_missing)

    lep = resolve_leptonica(arch_libdir)
    if lep is None:
        return report_missing(
            f"no Leptonica library under {arch_libdir} (install libleptonica-dev or libleptonica6).",
            allow_missing,
        )
    tes = resolve_tesseract(arch_libdir)
    if tes is None:
        return report_missing(f"no libtesseract under {arch_libdir} (install libtesseract5).", allow_missing)
    dl = resolve_libdl(arch)
    if dl is None:
        return report_missing(f"no libdl.so.2 under /lib or /usr/lib for {arch}-linux-gnu.", allow_missing)

    print(f"Using Leptonica: {lep}")
    print(f"Using Tesseract: {tes}")
    print(f"Using libdl:     {dl}")

    outdir = Path(args.outdir) if args.outdir else None
    if outdir is not None:
        out_root = outdir
        if out_root.name == "x64":
            out_root = out_root.parent
            print(f"note: normalized output path (use TFM folder, not x64): {out_root}")
        symlink_pair_into_x64(out_root / "x64", lep, tes)
        libdl_link = out_root / "libdl.so"
        if libdl_link.exists() or libdl_link.is_symlink():
            libdl_link.unlink()
        libdl_link.symlink_to(dl)
        print(f"libdl -> {libdl_link}")
        print(f"  {libdl_link} -> {libdl_link.readlink()}")

    if args.also_system:
        dest = Path(os.environ.get("DEST", "/usr/local/lib"))
        print(f"Also linking into {dest} (requires sudo)...")
        subprocess.run(["sudo", "mkdir", "-p", str(dest)], check=True)
        subprocess.run(["sudo", "ln", "-sf", str(lep), str(dest / "libleptonica-1.82.0.so")], check=True)
        subprocess.run(["sudo", "ln", "-sf", str(tes), str(dest / "libtesseract50.so")], check=True)
        subprocess.run(["sudo", "ldconfig"], check=True)
        dl_dir = dl.parent
        print(f"Also libdl.so beside system libdl.so.2 ({dl_dir})...")
        subprocess.run(["sudo", "ln", "-sf", str(dl), str(dl_dir / "libdl.so")], check=True)
        print("System copy done.")

    if outdir is None and not args.also_system:
        script = Path(__file__).name
        print("", file=sys.stderr)
        print("error: pass your app's **TFM output directory** (e.g. .../bin/Debug/net10.0).", file=sys.stderr)
        print("from repo root (after dotnet build):", file=sys.stderr)
        print(
            f'  python3 Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract/scripts/{script} '
            f'"$PWD/Lyo.Net/Data/Images/Lyo.Images.Ocr.Tesseract.Tests/bin/Debug/net10.0"',
            file=sys.stderr,
        )
        print("", file=sys.stderr)
        print(f"Optional: python3 .../{script} .../net10.0 --also-system", file=sys.stderr)
        return 1

    print("Done.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
