#!/usr/bin/env python3
"""Push nupkgs to a NuGet feed, one package at a time.

``dotnet nuget push --skip-duplicate`` treats HTTP 409 Conflict as success.
A glob push can then finish exit 0 after every package already existed.
This script still skips true duplicates, but fails if any package hits a
non-duplicate error, or if every package was a duplicate (nothing new published).

Usage:
  python3 scripts/nuget/ci_push.py --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY" artifacts/nuget
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

_DUP_RE = re.compile(
    r"already exists|^\s*Conflict\s+https?://|\b409\b",
    re.IGNORECASE | re.MULTILINE,
)


def classify(text: str, returncode: int) -> str:
    if _DUP_RE.search(text or ""):
        return "skipped"
    if returncode == 0:
        return "pushed"
    return "failed"


def push_one(nupkg: Path, *, source: str, api_key: str) -> tuple[str, str]:
    cmd = [
        "dotnet",
        "nuget",
        "push",
        str(nupkg),
        "--api-key",
        api_key,
        "--source",
        source,
        "--skip-duplicate",
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True, check=False)
    text = (proc.stdout or "") + (proc.stderr or "")
    if text:
        sys.stdout.write(text)
        if not text.endswith("\n"):
            sys.stdout.write("\n")
    return classify(text, proc.returncode), text


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", required=True, help="NuGet source URL")
    parser.add_argument("--api-key", default="", help="API key (or $NUGET_API_KEY)")
    parser.add_argument("directory", nargs="?", default="artifacts/nuget", help="Directory of nupkgs")
    args = parser.parse_args(argv)

    api_key = (args.api_key or os.environ.get("NUGET_API_KEY") or "").strip()
    if not api_key:
        print("NUGET_API_KEY is empty", file=sys.stderr)
        return 1

    folder = Path(args.directory)
    pkgs = sorted(p for p in folder.glob("*.nupkg") if p.is_file()) if folder.is_dir() else []
    if not pkgs:
        print("No nupkgs to push")
        return 0

    pushed: list[str] = []
    skipped: list[str] = []
    failed: list[str] = []
    for pkg in pkgs:
        outcome, _ = push_one(pkg, source=args.source, api_key=api_key)
        name = pkg.name
        if outcome == "pushed":
            print(f"[PUSHED] {name}")
            pushed.append(name)
        elif outcome == "skipped":
            print(f"[SKIP] {name} already on feed")
            skipped.append(name)
        else:
            print(f"[ERROR] {name}", file=sys.stderr)
            failed.append(name)

    print()
    print(f"Pushed: {len(pushed)}  skipped (duplicate): {len(skipped)}  failed: {len(failed)}")
    if failed:
        print("Failed packages:", file=sys.stderr)
        for name in failed:
            print(f"  - {name}", file=sys.stderr)
        return 1
    if not pushed:
        print("No packages were published. Every nupkg already exists on the feed or the push was rejected as Conflict.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
