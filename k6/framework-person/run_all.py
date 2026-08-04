#!/usr/bin/env python3
"""Run the framework-person k6 matrix and refresh the load-test dashboard manifest.

Usage:
  python3 k6/framework-person/run_all.py [keyword ...]

Keywords (substring or group aliases):
  load | stress | spike | soak | ceiling
  query | queryproject | queryroot
  nonsoak (alias: no-soak, nosoak)
  all (or matrix)

Examples:
  python3 k6/framework-person/run_all.py spike
  python3 k6/framework-person/run_all.py query spike
  CACHE_HIT_MODE=true python3 k6/framework-person/run_all.py ceiling
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

ROOT_DIR = Path(__file__).resolve().parent
REPO_ROOT = ROOT_DIR.parents[1]
MANIFESTS = REPO_ROOT / "scripts" / "benchmarks" / "build_manifests.py"

MATRIX_TESTS = [
    "query_load.js",
    "query_stress.js",
    "query_spike.js",
    "query_soak.js",
    "queryproject_load.js",
    "queryproject_stress.js",
    "queryproject_spike.js",
    "queryproject_soak.js",
    "queryroot_load.js",
    "queryroot_stress.js",
    "queryroot_spike.js",
    "queryroot_soak.js",
    "query_ceiling.js",
    "queryproject_ceiling.js",
    "queryroot_ceiling.js",
]


def _normalize(keyword: str) -> str:
    return "".join(keyword.split()).lower()


def _matches(test_name: str, test_file: str, keyword: str) -> bool:
    if keyword in ("all", "matrix"):
        return True
    if keyword in ("load", "stress", "spike", "soak", "ceiling"):
        return test_name.endswith(f"_{keyword}")
    if keyword == "query":
        return test_name.startswith("query_")
    if keyword in ("queryproject", "projected", "projection"):
        return test_name.startswith("queryproject_")
    if keyword in ("queryroot", "rootquery", "root"):
        return test_name.startswith("queryroot_")
    if keyword in ("nonsoak", "no-soak", "nosoak"):
        return not test_name.endswith("_soak")
    return keyword in test_name or keyword in test_file


def _collect_keywords(cli: list[str], env_filter: str) -> list[str]:
    out: list[str] = []
    if env_filter.strip():
        for part in env_filter.split(","):
            n = _normalize(part)
            if n:
                out.append(n)
    for arg in cli:
        for part in arg.split(","):
            n = _normalize(part)
            if n:
                out.append(n)
    return out


def _select_tests(keywords: list[str]) -> list[str]:
    if not keywords:
        return list(MATRIX_TESTS)
    selected: list[str] = []
    for test_file in MATRIX_TESTS:
        test_name = test_file.removesuffix(".js")
        if any(_matches(test_name, test_file, kw) for kw in keywords):
            selected.append(test_file)
    return selected


def _build_shared_packages() -> None:
    print("Building shared packages...")
    for rel in (
        "packages/typescript/lyo-api-client",
        "packages/typescript/lyo-person-api-client",
    ):
        pkg = REPO_ROOT / rel
        subprocess.run(["npm", "install"], cwd=pkg, check=True)
        subprocess.run(["npm", "run", "build"], cwd=pkg, check=True)
    print("Shared packages built.\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("keywords", nargs="*", help="Scenario filters / group aliases")
    args = parser.parse_args(argv)

    run_label = os.environ.get("RUN_LABEL", "").strip()
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    default_out = ROOT_DIR / "results" / (f"{run_label}-{stamp}" if run_label else stamp)
    out_dir = Path(os.environ.get("OUT_DIR", str(default_out)))
    out_dir.mkdir(parents=True, exist_ok=True)

    k6_bin = os.environ.get("K6_BIN", "k6")
    mode = os.environ.get("MODE", "full")
    continue_on_failure = os.environ.get("CONTINUE_ON_FAILURE", "false").lower() == "true"
    keywords = _collect_keywords(args.keywords, os.environ.get("TEST_FILTER", ""))
    selected = _select_tests(keywords)

    if not selected:
        print(f"No tests matched filter(s): {' '.join(keywords)}", file=sys.stderr)
        print(f"Available tests: {' '.join(MATRIX_TESTS)}", file=sys.stderr)
        return 1

    print(f"Running framework-person matrix suite in: {ROOT_DIR}")
    print(f"Results directory: {out_dir}")
    print(f"Mode: {mode}")
    print(f"Continue on failure: {continue_on_failure}")
    if keywords:
        print(f"Filter keywords: {' '.join(keywords)}")
    print(f"Selected tests: {' '.join(selected)}\n")

    _build_shared_packages()

    base_url = os.environ.get("BASE_URL", "http://localhost:5251")
    endpoint_path = os.environ.get("ENDPOINT_PATH", "/person/QueryConcrete")
    query_project_path = os.environ.get("QUERY_PROJECT_PATH", "/person/QueryProject")
    root_query_path = os.environ.get("ROOT_QUERY_PATH", "/Query")
    token = os.environ.get("TOKEN", "")

    for test_file in selected:
        test_name = test_file.removesuffix(".js")
        summary_file = out_dir / f"{test_name}.summary.json"
        log_file = out_dir / f"{test_name}.log"
        test_path = ROOT_DIR / "scenarios" / test_file

        print(f"=== Running {test_file} ===")
        cmd = [
            k6_bin,
            "run",
            "-e",
            f"BASE_URL={base_url}",
            "-e",
            f"ENDPOINT_PATH={endpoint_path}",
            "-e",
            f"QUERY_PROJECT_PATH={query_project_path}",
            "-e",
            f"ROOT_QUERY_PATH={root_query_path}",
            "--summary-export",
            str(summary_file),
            str(test_path),
        ]
        if token:
            cmd.extend(["-e", f"TOKEN={token}"])
        if os.environ.get("CACHE_HIT_MODE"):
            cmd.extend(["-e", f"CACHE_HIT_MODE={os.environ['CACHE_HIT_MODE']}"])
        for ceiling_var in (
            "CEILING_RATES",
            "CEILING_STEP_DURATION",
            "CEILING_MAX_VUS",
            "CEILING_GRACEFUL_STOP",
        ):
            if os.environ.get(ceiling_var):
                cmd.extend(["-e", f"{ceiling_var}={os.environ[ceiling_var]}"])
        if mode == "smoke":
            cmd.extend(["--vus", "1", "--iterations", "1"])
        extra = os.environ.get("EXTRA_K6_ARGS", "").strip()
        if extra:
            cmd.extend(extra.split())

        with log_file.open("w", encoding="utf-8") as log:
            proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
            log.write(proc.stdout or "")
            sys.stdout.write(proc.stdout or "")

        if proc.returncode != 0:
            print(f"Test failed: {test_file} (exit {proc.returncode})")
            print(f"See: {log_file}")
            if not continue_on_failure:
                print("Stopping because CONTINUE_ON_FAILURE is not true.")
                return proc.returncode
            print("Continuing because CONTINUE_ON_FAILURE=true.")

        print(f"Saved summary: {summary_file}")
        print(f"Saved log:     {log_file}\n")

    print("All framework-person matrix tests completed.")
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
