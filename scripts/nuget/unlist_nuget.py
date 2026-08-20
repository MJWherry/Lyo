#!/usr/bin/env python3
"""Unlist nuget.org versions for every packable Lyo.* package that has them.

Does not hard-delete. Exact-version restore still works. Needs a nuget.org
API key with Unlist (Trusted Publishing OIDC cannot unlist).

Usage:
  python3 scripts/nuget/unlist_nuget.py 1.0.5
  python3 scripts/nuget/unlist_nuget.py 1.0.5 1.0.6 --dry-run
  python3 scripts/nuget/unlist_nuget.py 1.0.5 --packages Lyo.Cache.Fusion
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from build_nuget import find_projects, get_project_name  # noqa: E402

FLAT_INDEX = "https://api.nuget.org/v3-flatcontainer/{id}/index.json"
DEFAULT_SOURCE = "https://api.nuget.org/v3/index.json"


def nuget_versions(package_id: str) -> set[str] | None:
    """Published versions on nuget.org, or None if the package id does not exist."""
    url = FLAT_INDEX.format(id=package_id.lower())
    try:
        with urllib.request.urlopen(url, timeout=15) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        if exc.code == 404:
            return None
        raise
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, OSError) as exc:
        raise SystemExit(f"nuget.org lookup failed for {package_id}: {exc}") from exc
    return {str(v) for v in (data.get("versions") or []) if v}


def unlist_one(package_id: str, version: str, *, api_key: str, source: str) -> tuple[str, str]:
    cmd = [
        "dotnet",
        "nuget",
        "delete",
        package_id,
        version,
        "--api-key",
        api_key,
        "--source",
        source,
        "--non-interactive",
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True, check=False)
    text = (proc.stdout or "") + (proc.stderr or "")
    if text:
        sys.stdout.write(text)
        if not text.endswith("\n"):
            sys.stdout.write("\n")
    if proc.returncode == 0:
        return "unlisted", text
    lowered = text.lower()
    if "not found" in lowered or "does not exist" in lowered or "already unlisted" in lowered:
        return "skipped", text
    return "failed", text


def package_ids(patterns: list[str]) -> list[str]:
    found: list[str] = []
    seen: set[str] = set()
    for pattern in patterns or ["Lyo.*"]:
        for project in find_projects(pattern):
            name = get_project_name(project)
            if name in seen:
                continue
            seen.add(name)
            found.append(name)
    return found


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("versions", nargs="+", help="SemVer values to unlist (e.g. 1.0.5)")
    parser.add_argument("--packages", default="", help="Names or globs (space-separated). Empty = every packable Lyo.*")
    parser.add_argument("--api-key", default="", help="nuget.org API key (or $NUGET_API_KEY)")
    parser.add_argument("--source", default=DEFAULT_SOURCE, help="NuGet source used by dotnet nuget delete")
    parser.add_argument("--dry-run", action="store_true", help="Print matches; do not unlist")
    args = parser.parse_args(argv)

    versions = [v.strip() for v in args.versions if v.strip()]
    if not versions:
        print("No versions given", file=sys.stderr)
        return 1

    patterns = (args.packages or "").split()
    ids = package_ids(patterns)
    if not ids:
        print("No packable Lyo.* projects matched", file=sys.stderr)
        return 1

    api_key = (args.api_key or os.environ.get("NUGET_API_KEY") or "").strip()
    if not args.dry_run and not api_key:
        print("NUGET_API_KEY is empty (nuget.org Unlist key, not Trusted Publishing OIDC)", file=sys.stderr)
        return 1

    planned: list[tuple[str, str]] = []
    missing_ids = 0
    for package_id in ids:
        published = nuget_versions(package_id)
        if published is None:
            print(f"[skip] {package_id} is not on nuget.org")
            missing_ids += 1
            continue
        for version in versions:
            if version in published:
                planned.append((package_id, version))
            else:
                print(f"[skip] {package_id} {version} not published")

    print()
    print(f"Packages scanned: {len(ids)}  not on nuget.org: {missing_ids}  to unlist: {len(planned)}")
    if not planned:
        return 0

    unlisted: list[str] = []
    skipped: list[str] = []
    failed: list[str] = []
    for package_id, version in planned:
        label = f"{package_id} {version}"
        if args.dry_run:
            print(f"[dry-run] {label}")
            unlisted.append(label)
            continue
        outcome, _ = unlist_one(package_id, version, api_key=api_key, source=args.source)
        if outcome == "unlisted":
            print(f"[unlisted] {label}")
            unlisted.append(label)
        elif outcome == "skipped":
            print(f"[skip] {label}")
            skipped.append(label)
        else:
            print(f"[error] {label}", file=sys.stderr)
            failed.append(label)

    print()
    print(f"Unlisted: {len(unlisted)}  skipped: {len(skipped)}  failed: {len(failed)}")
    if failed:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
