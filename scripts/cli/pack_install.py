#!/usr/bin/env python3
"""Pack and install the Lyo.Cli global dotnet tool locally.

Usage:
  python3 scripts/cli/pack_install.py pack
  python3 scripts/cli/pack_install.py install
  python3 scripts/cli/pack_install.py update
  python3 scripts/cli/pack_install.py uninstall
  python3 scripts/cli/pack_install.py pack-install   # pack then force-reinstall
"""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CSPROJ = REPO_ROOT / "Lyo.Net" / "Tools" / "Lyo.Cli" / "Lyo.Cli.csproj"
DEFAULT_OUT = REPO_ROOT / "artifacts" / "cli"
PACKAGE_ID = "Lyo.Cli"


def run(cmd: list[str]) -> int:
    print("+", " ".join(cmd), flush=True)
    return subprocess.call(cmd, cwd=REPO_ROOT)


def pack(version: str, output: Path, config: str) -> int:
    output.mkdir(parents=True, exist_ok=True)
    return run(
        [
            "dotnet",
            "pack",
            str(CSPROJ),
            "-c",
            config,
            "-o",
            str(output),
            f"/p:Version={version}",
            f"/p:PackageVersion={version}",
        ]
    )


def install(output: Path, version: str | None) -> int:
    cmd = ["dotnet", "tool", "install", "-g", PACKAGE_ID, "--add-source", str(output)]
    if version:
        cmd.extend(["--version", version])
    return run(cmd)


def update(output: Path, version: str | None = None) -> int:
    cmd = ["dotnet", "tool", "update", "-g", PACKAGE_ID, "--add-source", str(output)]
    if version:
        cmd.extend(["--version", version])
    return run(cmd)


def uninstall() -> int:
    return run(["dotnet", "tool", "uninstall", "-g", PACKAGE_ID])


def reinstall(output: Path, version: str) -> int:
    """Uninstall then install so the same version number still picks up a freshly packed nupkg.

    ``dotnet tool update`` is a no-op when the installed version already matches.
    """
    # Ignore uninstall failure (tool may not be installed yet).
    uninstall()
    return install(output, version)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument(
        "action",
        choices=["pack", "install", "update", "uninstall", "pack-install"],
        help="Action to perform",
    )
    parser.add_argument("-v", "--version", default="1.0.0", help="Package version (default: 1.0.0)")
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        default=Path(os.environ.get("LYO_CLI_NUPKG_DIR", str(DEFAULT_OUT))),
        help="nupkg output directory (default: artifacts/cli)",
    )
    parser.add_argument("-c", "--configuration", default=os.environ.get("BUILD_CONFIG", "Release"))
    args = parser.parse_args(argv)

    if not CSPROJ.is_file():
        print(f"error: project not found: {CSPROJ}", file=sys.stderr)
        return 1

    out = args.output.expanduser()
    if args.action == "pack":
        return pack(args.version, out, args.configuration)
    if args.action == "install":
        return install(out, args.version)
    if args.action == "update":
        # Same-version local packs need a reinstall; plain update often no-ops.
        return reinstall(out, args.version)
    if args.action == "uninstall":
        return uninstall()
    if args.action == "pack-install":
        rc = pack(args.version, out, args.configuration)
        if rc != 0:
            return rc
        return reinstall(out, args.version)
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
