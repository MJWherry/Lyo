#!/usr/bin/env python3
"""Build and run the Lyo workbench stack.

Starts TestApi, TestGateway, the job worker/scheduler examples, Postgres, RabbitMQ, and Redis
on one Docker network. Separate from the benchmark runner (scripts/docker/run.py).

Usage:
  python3 scripts/docker/run_workbench.py up
  python3 scripts/docker/run_workbench.py down
  python3 scripts/docker/run_workbench.py build
  python3 scripts/docker/run_workbench.py logs
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

_SCRIPTS = Path(__file__).resolve().parents[1]
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402

COMPOSE_FILE = REPO_ROOT / "docker" / "workbench" / "compose.yml"
PROJECT_NAME = "lyo-workbench"


def _compose(args: list[str]) -> int:
    cmd = [
        "docker",
        "compose",
        "-f",
        str(COMPOSE_FILE),
        "--project-name",
        PROJECT_NAME,
        *args,
    ]
    print(f"==> {' '.join(cmd)}", flush=True)
    return subprocess.run(cmd, cwd=REPO_ROOT).returncode


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument(
        "action",
        nargs="?",
        default="up",
        choices=["up", "down", "build", "logs"],
        help="up (default): start detached with --build; down: stop; build: images only; logs: follow",
    )
    parser.add_argument("extra", nargs=argparse.REMAINDER, help="Extra args forwarded to docker compose")
    args = parser.parse_args(argv)

    extra = list(args.extra)
    if extra and extra[0] == "--":
        extra = extra[1:]

    if args.action == "up":
        return _compose(["up", "-d", "--build", *extra])
    if args.action == "down":
        return _compose(["down", *extra])
    if args.action == "build":
        return _compose(["build", *extra])
    return _compose(["logs", "-f", *extra])


if __name__ == "__main__":
    raise SystemExit(main())
