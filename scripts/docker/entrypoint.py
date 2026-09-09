#!/usr/bin/env python3
"""Target-driven entrypoint for the Lyo runner container.

Projects are prebuilt into the image. This resolves TARGET and runs baked binaries:
  *.Benchmarks -> scripts/benchmarks/run_dotnet.py (BENCH_NO_BUILD=1)
  *.Tests      -> dotnet test --no-build --no-restore

Environment:
  TARGET, BENCH_FILTER, NO_DOCKER, TEST_FILTER, HOST_UID, HOST_GID
"""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path

_HERE = Path(__file__).resolve().parent
_SCRIPTS = _HERE.parent
for _p in (_SCRIPTS, _HERE):
    if str(_p) not in sys.path:
        sys.path.insert(0, str(_p))

from lyo_tooling.bench import category_from_csproj_name  # noqa: E402
from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402
from resolve_targets import resolve_targets  # noqa: E402

DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"
HISTORY_DIR = REPO_ROOT / "docs" / "benchmarks" / "history"
PORTFOLIO_HISTORY_DIR = REPO_ROOT / "apps" / "gateway" / "public" / "benchmarks" / "history"
RUN_DOTNET = REPO_ROOT / "scripts" / "benchmarks" / "run_dotnet.py"


def _chown_mounts() -> None:
    uid = os.environ.get("HOST_UID", "1000")
    gid = os.environ.get("HOST_GID", "1000")
    for path in (DATA_DIR, HISTORY_DIR, PORTFOLIO_HISTORY_DIR):
        if path.is_dir():
            subprocess.run(["chown", "-R", f"{uid}:{gid}", str(path)], check=False)


def _run_benchmarks(categories: list[str]) -> None:
    # History is bind-mounted; sync after every suite so the static hub updates on-the-fly.
    args = [sys.executable, str(RUN_DOTNET)]
    if os.environ.get("NO_DOCKER", "0") == "1":
        args.append("--no-docker")
    args.extend(["--filter", os.environ.get("BENCH_FILTER", "*"), *categories])
    print(f"==> bench: {' '.join(args)}", flush=True)
    env = os.environ.copy()
    env["BENCH_NO_BUILD"] = "1"
    if (REPO_ROOT / "apps" / "gateway").is_dir():
        PORTFOLIO_HISTORY_DIR.mkdir(parents=True, exist_ok=True)
    subprocess.run(args, check=True, cwd=REPO_ROOT, env=env)


def _run_tests(projects: list[str]) -> None:
    common = ["dotnet", "test", "-c", "Release", "--no-build", "--no-restore"]
    test_filter = os.environ.get("TEST_FILTER", "").strip()
    if test_filter:
        common.extend(["--filter", test_filter])
    print(f"==> test: running {len(projects)} xUnit test project(s)", flush=True)
    failed: list[str] = []
    for proj in projects:
        cmd = [*common, proj]
        print(f"==> {' '.join(cmd)}", flush=True)
        if subprocess.run(cmd, cwd=REPO_ROOT).returncode != 0:
            failed.append(proj)
    if failed:
        print(f"==> {len(failed)} test project(s) failed:", file=sys.stderr, flush=True)
        for p in failed:
            print(f"   {p}", file=sys.stderr, flush=True)
        raise SystemExit(1)
    print("==> all test projects passed", flush=True)


def main() -> int:
    os.chdir(REPO_ROOT)
    try:
        target = os.environ.get("TARGET", "all")
        projects = resolve_targets(target)
        if not projects:
            print(f"TARGET '{target}' resolved to no projects", file=sys.stderr)
            return 2

        categories: list[str] = []
        test_projects: list[str] = []
        for proj in projects:
            base = Path(proj).name.removesuffix(".csproj")
            cat = category_from_csproj_name(base)
            if cat is not None:
                categories.append(cat)
            elif base.endswith(".Tests"):
                test_projects.append(proj)
            else:
                print(f"==> skipping {proj} (not a *.Benchmarks or *.Tests project)", file=sys.stderr, flush=True)

        if categories:
            _run_benchmarks(categories)
        if test_projects:
            _run_tests(test_projects)
        return 0
    finally:
        _chown_mounts()


if __name__ == "__main__":
    raise SystemExit(main())
