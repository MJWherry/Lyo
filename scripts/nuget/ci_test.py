#!/usr/bin/env python3
"""Discover sibling *.Tests projects for a pack selection and run them.

Usage:
  python3 scripts/nuget/ci_test.py --scope all
  python3 scripts/nuget/ci_test.py --scope named --packages "Lyo.Encryption"
  python3 scripts/nuget/ci_test.py --scope changed --since v1.0.0
  python3 scripts/nuget/ci_test.py --scope all --list-only

Missing tests are skipped (exit 0). Zero selected test projects is success.
"""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
LYO_NET = REPO_ROOT / "Lyo.Net"

sys.path.insert(0, str(Path(__file__).parent))
import build_nuget as n  # noqa: E402


def select_packable(scope: str, packages: str, since: str) -> list[Path]:
    if scope == "all":
        return n.find_projects("Lyo.*")
    if scope == "named":
        selected: list[Path] = []
        for pattern in packages.split():
            for project in n.find_projects(pattern):
                if project not in selected:
                    selected.append(project)
        return selected
    if scope == "changed":
        ref = since.strip() or n.default_changed_since()
        return n.find_changed_projects(ref)
    raise SystemExit(f"unknown scope: {scope}")


def find_test_project(library: Path) -> Path | None:
    name = n.get_project_name(library) + ".Tests"
    sibling = library.parent / f"{name}.csproj"
    if sibling.is_file():
        return sibling
    matches = sorted(LYO_NET.rglob(f"{name}.csproj"))
    return matches[0] if matches else None


def discover_tests(libraries: list[Path], *, scope: str) -> tuple[list[Path], list[str]]:
    if scope == "all":
        tests = sorted(p for p in LYO_NET.rglob("*.Tests.csproj") if n.get_project_name(p).endswith(".Tests"))
        return tests, []
    tests: list[Path] = []
    skipped: list[str] = []
    seen: set[Path] = set()
    for lib in libraries:
        test = find_test_project(lib)
        if test is None:
            skipped.append(n.get_project_name(lib))
            continue
        if test not in seen:
            seen.add(test)
            tests.append(test)
    return tests, skipped


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--scope", choices=["all", "changed", "named"], default="all")
    parser.add_argument("--packages", default="")
    parser.add_argument("--since", default="")
    parser.add_argument("--list-only", action="store_true")
    parser.add_argument("--configuration", default=os.environ.get("BUILD_CONFIG", "Release"))
    args = parser.parse_args(argv)

    if args.scope == "named" and not args.packages.strip():
        raise SystemExit("scope=named requires --packages")

    libraries = select_packable(args.scope, args.packages, args.since)
    tests, skipped = discover_tests(libraries, scope=args.scope)

    print(f"Selected libraries: {len(libraries)}")
    print(f"Test projects: {len(tests)}")
    for path in tests:
        print(f"  - {n.get_project_name(path)}")
    if skipped:
        print(f"No tests (skipped): {len(skipped)}")
        for name in skipped:
            print(f"  - {name}")

    if not tests:
        print("No test projects to run; exiting 0")
        return 0
    if args.list_only:
        return 0

    failed: list[str] = []
    for path in tests:
        name = n.get_project_name(path)
        print(f"Testing {name}...", flush=True)
        cmd = ["dotnet", "test", str(path), "-c", args.configuration, "--no-restore"]
        if subprocess.call(cmd, cwd=LYO_NET) != 0:
            failed.append(name)
    if failed:
        print("Failed tests:")
        for name in failed:
            print(f"  - {name}")
        return 1
    print("All selected tests passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
