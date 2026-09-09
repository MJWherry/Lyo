#!/usr/bin/env python3
"""Build and run the target-driven Lyo runner container.

Derives a per-target image tag, then ``docker compose build`` / ``run`` the ``run`` service.
Default: start detached and return immediately (true background). Mid-run mounts still
update docs/benchmarks as suites finish.

Usage:
  python3 scripts/docker/run.py [options] <target...>

Targets (see scripts/docker/resolve_targets.py):
  benchmarks | tests | all
  Lyo.Lock.Benchmarks
  Lyo.Lock.Benchmarks Lyo.Cache.Tests

Options:
  --fg                 stream logs in the foreground (waits for exit)
  --wait               detached run, but block until the container exits
  --build-only         build image only
  --no-docker          skip Testcontainers-backed benchmark classes (NO_DOCKER=1)
  --filter GLOB        BenchmarkDotNet --filter (BENCH_FILTER)
  --test-filter EXPR   xUnit --filter (TEST_FILTER)
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
import uuid
from pathlib import Path

_SCRIPTS = Path(__file__).resolve().parents[1]
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402

MANIFESTS = REPO_ROOT / "scripts" / "benchmarks" / "build_manifests.py"


def _slug(target: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", target.lower()).strip("-")
    return slug or "all"


def _sync_portfolio_host() -> None:
    print("==> Host safety-net: sync portfolio history", flush=True)
    subprocess.run(
        [sys.executable, str(MANIFESTS), "--sync-portfolio-only"],
        check=False,
        cwd=REPO_ROOT,
    )
    print(
        "==> Static hub: docs/benchmarks/index.html · Portfolio: restart/refresh Next if data/*.json changed",
        flush=True,
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--fg", action="store_true", help="Stream logs in the foreground (waits for exit)")
    parser.add_argument("--wait", action="store_true", help="Detached run, but block until the container exits")
    parser.add_argument("--build-only", action="store_true", help="Build the image but do not run")
    parser.add_argument("--no-docker", action="store_true", help="Skip Testcontainers-backed classes")
    parser.add_argument("--filter", default=os.environ.get("BENCH_FILTER", "*"), dest="bench_filter")
    parser.add_argument("--test-filter", default=os.environ.get("TEST_FILTER", ""), dest="test_filter")
    parser.add_argument("targets", nargs="+", help="Target tokens (group / project name / list)")
    args = parser.parse_args(argv)

    if args.fg and args.wait:
        print("error: use either --fg or --wait, not both", file=sys.stderr)
        return 2

    target = " ".join(args.targets)
    run_image = f"lyo-runner-{_slug(target)}"
    print(f"==> TARGET={target}", flush=True)
    print(f"==> RUN_IMAGE={run_image}", flush=True)

    env = os.environ.copy()
    env["TARGET"] = target
    env["RUN_IMAGE"] = run_image
    env["NO_DOCKER"] = "1" if args.no_docker else env.get("NO_DOCKER", "0")
    env["BENCH_FILTER"] = args.bench_filter
    env["TEST_FILTER"] = args.test_filter

    # Ensure benchmark data/history mount targets exist on the host.
    (REPO_ROOT / "docs" / "benchmarks" / "data").mkdir(parents=True, exist_ok=True)
    (REPO_ROOT / "docs" / "benchmarks" / "history").mkdir(parents=True, exist_ok=True)
    if (REPO_ROOT / "apps" / "gateway").is_dir():
        (REPO_ROOT / "apps" / "gateway" / "public" / "benchmarks" / "history").mkdir(parents=True, exist_ok=True)

    subprocess.run(["docker", "compose", "build", "run"], check=True, cwd=REPO_ROOT, env=env)

    if args.build_only:
        print(f"==> built {run_image} (--build-only)", flush=True)
        return 0

    if args.fg:
        result = subprocess.run(["docker", "compose", "run", "--rm", "run"], cwd=REPO_ROOT, env=env)
        if result.returncode == 0:
            _sync_portfolio_host()
        return result.returncode

    name = f"lyo-run-{_slug(target)[:40]}-{uuid.uuid4().hex[:8]}"
    create = subprocess.run(
        ["docker", "compose", "run", "-d", "--name", name, "run"],
        cwd=REPO_ROOT,
        env=env,
        check=True,
        capture_output=True,
        text=True,
    )
    container_id = (create.stdout or "").strip() or name

    if not args.wait:
        print(f"==> started in background: {container_id}", flush=True)
        print(f"==> logs:  docker logs -f {name}", flush=True)
        print(f"==> status: docker ps -a --filter name={name}", flush=True)
        print(
            "==> manifests update on-the-fly via mounts (docs/benchmarks)",
            flush=True,
        )
        return 0

    print(f"==> started {container_id}; waiting for exit…", flush=True)
    wait = subprocess.run(["docker", "wait", container_id], capture_output=True, text=True)
    try:
        rc = int((wait.stdout or "1").strip() or "1")
    except ValueError:
        rc = 1
    subprocess.run(["docker", "rm", "-f", container_id], check=False, capture_output=True)
    if rc != 0:
        print(f"==> container exited {rc}", file=sys.stderr, flush=True)
    else:
        _sync_portfolio_host()
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
