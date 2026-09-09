#!/usr/bin/env python3
"""Run Lyo BenchmarkDotNet suites in Release and regenerate dashboard manifests.

Usage:
  python3 scripts/benchmarks/run_dotnet.py [--no-docker] [--filter GLOB] [category ...]

  category       One or more of: encryption compression hashing cache query csv xlsx lock filestorage.
                 When omitted, every suite is run.
  --no-docker    Skip Testcontainers-backed classes via positive --filter globs.
  --filter GLOB  BenchmarkDotNet --filter (default '*').
  --no-sync-portfolio  Skip copying history into apps/gateway/public after export (no-op if that tree is gone).

Each suite's LyoBenchmarkExporter writes <name>.lyobench.json into BenchmarkDotNet.Artifacts
next to its project. After each suite, build_manifests.py exports that category (archives a
history snapshot, updates data/<name>.{json,js}; copies into apps/gateway only if present).
"""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

_SCRIPTS = Path(__file__).resolve().parents[1]
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from lyo_tooling.bench import (  # noqa: E402
    ALL_BDN_CATEGORIES,
    BDN_PROJECTS,
    NODOCKER_FILTERS,
    artifacts_dir,
    project_path,
)
from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402

MANIFESTS = REPO_ROOT / "scripts" / "benchmarks" / "build_manifests.py"


def _run(argv: list[str], *, env: dict[str, str] | None = None) -> None:
    print("==>", " ".join(argv), flush=True)
    subprocess.run(argv, check=True, cwd=REPO_ROOT, env=env)


def run_category(category: str, *, filter_glob: str, no_docker: bool, sync_portfolio: bool) -> None:
    if category not in BDN_PROJECTS:
        raise SystemExit(f"Unknown category: {category} (expected one of {', '.join(ALL_BDN_CATEGORIES)})")

    project = project_path(category)
    artifacts = artifacts_dir(category)
    print(f"==> Running {category} benchmarks", flush=True)

    run_opts = ["dotnet", "run", "-c", "Release", "--project", str(project)]
    if os.environ.get("BENCH_NO_BUILD") == "1":
        run_opts += ["--no-build", "--no-restore"]

    # --join: one Summary for the suite so the exporter keeps all groups (not only the last class).
    args = [*run_opts, "--", "--join", "--artifacts", str(artifacts), "--filter"]
    if no_docker and category in NODOCKER_FILTERS:
        args.extend(NODOCKER_FILTERS[category])
    else:
        args.append(filter_glob)

    _run(args)

    print(f"==> Exporting {category} dashboard data (static + portfolio)", flush=True)
    manifest_args = [sys.executable, str(MANIFESTS), f"--{category}-only"]
    # On-the-fly: sync portfolio after every suite so both surfaces update before the next run.
    if not sync_portfolio:
        manifest_args.append("--no-sync-portfolio")
    _run(manifest_args)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--no-docker", action="store_true", help="Skip Testcontainers-backed benchmark classes")
    parser.add_argument("--filter", default="*", dest="filter_glob", help="BenchmarkDotNet --filter glob")
    parser.add_argument(
        "--no-sync-portfolio",
        action="store_true",
        help="Do not copy docs/benchmarks/history into apps/gateway/public after each suite (no-op if absent)",
    )
    parser.add_argument(
        "categories",
        nargs="*",
        help=f"Suites to run (default: all). One of: {', '.join(ALL_BDN_CATEGORIES)}",
    )
    args = parser.parse_args(argv)

    categories = list(args.categories) if args.categories else list(ALL_BDN_CATEGORIES)
    for category in categories:
        run_category(
            category,
            filter_glob=args.filter_glob,
            no_docker=args.no_docker,
            sync_portfolio=not args.no_sync_portfolio,
        )

    print("Done. Open docs/benchmarks/index.html (S3 publish is separate).", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
