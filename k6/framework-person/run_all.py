#!/usr/bin/env python3
"""Run the framework-person intensity × cache k6 matrix and refresh the dashboard.

Usage:
  python3 k6/framework-person/run_all.py [keyword ...]

Keywords (axis groups are AND-ed; within a group, OR):
  Profiles:   load | stress | spike | soak | ceiling
  Endpoints:  query | queryproject | queryroot
  Intensity:  low | med | high          (default: all three)
  Cache:      cached | uncached         (default: both)
  Groups:     nonsoak | all | matrix

Every cell pins RANDOM_SEED=20260623. Results are named
  {endpoint}_{profile}_{intensity}_{cached|uncached}.summary.json

Examples:
  python3 k6/framework-person/run_all.py spike
  python3 k6/framework-person/run_all.py query load med
  python3 k6/framework-person/run_all.py nonsoak med uncached
  TEST_FILTER=query_spike,queryproject_load python3 k6/framework-person/run_all.py
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

from matrix import K6ProcessRunner, MatrixAxes, MatrixPlanner
from matrix.k6_compat import rewrite_bare_imports_for_k6

ROOT_DIR = Path(__file__).resolve().parent
REPO_ROOT = ROOT_DIR.parents[1]
MANIFESTS = REPO_ROOT / "scripts" / "benchmarks" / "build_manifests.py"


def _build_shared_packages() -> None:
    print("Building shared packages...")
    # lyo-query first: person-api-client re-exports it; k6 needs a relative path after build.
    for rel in (
        "packages/typescript/lyo-query",
        "packages/typescript/lyo-api-client",
        "packages/typescript/lyo-person-api-client",
    ):
        pkg = REPO_ROOT / rel
        subprocess.run(["npm", "install"], cwd=pkg, check=True)
        subprocess.run(["npm", "run", "build"], cwd=pkg, check=True)
    touched = rewrite_bare_imports_for_k6(REPO_ROOT)
    if touched:
        print("Rewrote bare imports for k6:")
        for path in touched:
            print(f"  - {path.relative_to(REPO_ROOT)}")
    print("Shared packages built.\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("keywords", nargs="*", help="Scenario / intensity / cache filters")
    args = parser.parse_args(argv)

    run_label = os.environ.get("RUN_LABEL", "").strip()
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    default_out = ROOT_DIR / "results" / (f"{run_label}-{stamp}" if run_label else stamp)
    out_dir = Path(os.environ.get("OUT_DIR", str(default_out)))
    out_dir.mkdir(parents=True, exist_ok=True)

    planner = MatrixPlanner(seed=MatrixAxes.MATRIX_SEED)
    keywords = planner.collect_keywords(args.keywords, os.environ.get("TEST_FILTER", ""))
    cells = planner.plan(keywords)

    if not cells:
        print(f"No cells matched filter(s): {' '.join(keywords)}", file=sys.stderr)
        print(f"Available scenarios: {' '.join(MatrixAxes.SCENARIO_FILES)}", file=sys.stderr)
        return 1

    mode = os.environ.get("MODE", "full")
    continue_on_failure = os.environ.get("CONTINUE_ON_FAILURE", "false").lower() == "true"

    print(f"Running framework-person matrix suite in: {ROOT_DIR}")
    print(f"Results directory: {out_dir}")
    print(f"Mode: {mode}")
    print(f"Continue on failure: {continue_on_failure}")
    print(f"Matrix seed: {MatrixAxes.MATRIX_SEED}")
    if keywords:
        print(f"Filter keywords: {' '.join(keywords)}")
    print(f"Planned cells ({len(cells)}):")
    for cell in cells:
        print(f"  - {cell.cell_id}")
    print()

    _build_shared_packages()

    runner = K6ProcessRunner(
        root_dir=ROOT_DIR,
        out_dir=out_dir,
        k6_bin=os.environ.get("K6_BIN", "k6"),
        mode=mode,
        base_url=os.environ.get("BASE_URL", "http://localhost:5251"),
        endpoint_path=os.environ.get("ENDPOINT_PATH", "/person/QueryConcrete"),
        query_project_path=os.environ.get("QUERY_PROJECT_PATH", "/person/QueryProject"),
        root_query_path=os.environ.get("ROOT_QUERY_PATH", "/Query"),
        token=os.environ.get("TOKEN", ""),
    )

    for cell in cells:
        rc = runner.run(cell)
        if rc != 0:
            if not continue_on_failure:
                print("Stopping because CONTINUE_ON_FAILURE is not true.")
                return rc
            print("Continuing because CONTINUE_ON_FAILURE=true.")

    print("All framework-person matrix cells completed.")
    print(f"Results: {out_dir}")

    if shutil.which("python3") or sys.executable:
        print("Refreshing k6 benchmark dashboard manifest...")
        rc = subprocess.run(
            [sys.executable, str(MANIFESTS), "--k6-only", "--k6-run-dir", str(out_dir)],
            cwd=REPO_ROOT,
        ).returncode
        if rc != 0:
            print("Warning: benchmark manifest refresh failed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
