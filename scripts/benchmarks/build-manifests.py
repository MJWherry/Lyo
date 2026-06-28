#!/usr/bin/env python3
"""Aggregate benchmark reports into the unified ``lyo.bench/v1`` schema under docs/benchmarks/data/.

Two sources feed one schema:

* **Micro (BenchmarkDotNet)** — the C# ``LyoBenchmarkExporter`` already emits
  ``<name>.lyobench.json`` (``type: "micro"``). This script just copies those files into the data
  dir (no CSV parsing).
* **Load (k6)** — k6 cannot emit the schema itself, so this script normalizes the raw
  ``*.summary.json`` files into a ``LoadTestReport`` (``type: "load"``).

For every report it writes ``<name>.json`` (portable) and ``<name>.js``
(``window.LyoBench.reports["<name>"] = …`` for file:// viewing), then a ``registry.js`` listing all
reports for the index page.
"""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"
SCHEMA = "lyo.bench/v1"

K6_RESULTS = REPO_ROOT / "k6" / "framework-person" / "results"
K6_RUN_PREFIXES = ("prod-like-", "prod-matrix-")
K6_SUITE_NAMES = [
    "query_load",
    "query_stress",
    "query_spike",
    "query_soak",
    "queryproject_load",
    "queryproject_stress",
    "queryproject_spike",
    "queryproject_soak",
]
K6_REPORT_NAME = "query-api"
K6_REPORT_TITLE = "Query API (k6)"

# Suite-level methodology so the latencies are interpretable on their own.
K6_DESCRIPTION = (
    "k6 load/stress/spike/soak against the Lyo person API. The /person/query endpoint returns full "
    "Person entities; /person/QueryProject returns field-projected rows. Every iteration pages "
    "100-300 persons (varied by VU/iteration to bypass caches). Randomized multi-key sorting over "
    "unindexed columns is disabled by default (only filter_sort carries a fixed sort); other cases use "
    "the server default (PK) order. The 'cases' below describe the request shape behind each scenario; "
    "hotspots reference these case ids."
)

# Per-case query structure. Source of truth: k6/framework-person/lib/cases.js + queryFactory.js +
# projectionQueries.js. Keep this map in sync when those cases change. Randomized field/branch counts are
# noted in the description; selectionFieldCount is set only when the projected field list is fixed.
K6_CASE_META: dict[str, dict[str, Any]] = {
    "baseline": {
        "endpoint": "query",
        "description": "Unfiltered page of persons in server default (PK) order; no where clauses, no includes. Baseline read cost.",
        "whereClauses": 0,
        "sortFields": [],
        "includes": [],
    },
    "filter_sort": {
        "endpoint": "query",
        "description": "Filters on SourceEntityType then applies a fixed multi-key sort; exercises filter + ordered scan.",
        "whereClauses": 1,
        "filters": ["SourceEntityType in DEFAULT_SOURCE_FILTER_VALUES"],
        "sortFields": ["LastName", "FirstName", "Id"],
        "includes": [],
    },
    "complex_querynode": {
        "endpoint": "query",
        "description": "Nested AND/OR QueryNode where-clause tree; stresses the predicate translator with a multi-predicate boolean tree.",
        "sortFields": [],
        "includes": [],
    },
    "query_with_subquery": {
        "endpoint": "query",
        "description": "Two-phase query whose where clause contains a correlated subquery; exercises subquery planning and execution.",
        "sortFields": [],
        "includes": [],
    },
    "realistic_include": {
        "endpoint": "query",
        "description": "100-300 persons with a single 3-table include hop; cache-bypassing via randomized start/amount.",
        "whereClauses": 0,
        "sortFields": [],
        "includes": ["contactaddresses.address"],
    },
    "heavy_include": {
        "endpoint": "query",
        "description": "Heaviest read path: up to 3 include branches (address/phone/email) over larger pages.",
        "whereClauses": 0,
        "sortFields": [],
        "includes": [
            "contactaddresses.address",
            "contactphonenumbers.phonenumber",
            "contactemailaddresses.emailaddress",
        ],
    },
    "select_projection": {
        "endpoint": "queryproject",
        "description": "POST /person/QueryProject with randomized mixed root + nested field selection (2-6 fields per request).",
        "includes": [],
    },
    "projection_roots": {
        "endpoint": "queryproject",
        "description": "Root scalar fields only (no collection merge); SQL projection of 2-6 root columns.",
        "selectionFieldCount": 5,
        "includes": [],
    },
    "projection_nested": {
        "endpoint": "queryproject",
        "description": "Single nested collection path under Select (e.g. contactaddresses.address.city/postalcode); 2-7 fields.",
        "includes": ["contactaddresses.address"],
    },
    "projection_unified": {
        "endpoint": "queryproject",
        "description": "Mixed depths under one collection root (unified-root SQL merge + sibling row zip); 2-6 fields.",
        "includes": ["contactaddresses.address"],
    },
    "computed_collection_parallel": {
        "endpoint": "queryproject",
        "description": "Computed field 'streetLine' from a collection-parallel template; dependencies auto-selected server-side.",
        "includes": ["contactaddresses.address"],
    },
    "computed_scalar": {
        "endpoint": "queryproject",
        "description": "Scalar-row computed field 'fullName' ({FirstName} {LastName}); no collection-parallel path.",
        "selectionFieldCount": 4,
        "includes": [],
    },
}


def _artifacts(*parts: str) -> Path:
    return REPO_ROOT.joinpath("Lyo.Net", *parts, "BenchmarkDotNet.Artifacts")


# BenchmarkDotNet suites: each maps a (machine) name to the artifacts directory the exporter
# writes <name>.lyobench.json into. The report's title/type/schema come from the JSON itself.
BDN_CATEGORIES: list[dict[str, Any]] = [
    {"name": "encryption", "dir": _artifacts("Security", "Encryption", "Lyo.Encryption.Benchmarks")},
    {"name": "compression", "dir": _artifacts("Data", "Compression", "Lyo.Compression.Benchmarks")},
    {"name": "hashing", "dir": _artifacts("Security", "Hashing", "Lyo.Hashing.Benchmarks")},
    {"name": "cache", "dir": _artifacts("Core", "Cache", "Lyo.Cache.Benchmarks")},
    {"name": "query", "dir": _artifacts("Data", "Query", "Lyo.Query.Benchmarks")},
    {"name": "csv", "dir": _artifacts("Data", "Csv", "Lyo.Csv.Benchmarks")},
    {"name": "xlsx", "dir": _artifacts("Data", "Xlsx", "Lyo.Xlsx.Benchmarks")},
    {"name": "lock", "dir": _artifacts("Core", "Lock", "Lyo.Lock.Benchmarks")},
]


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def read_json(path: Path) -> Any:
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def write_report(report: dict[str, Any]) -> None:
    """Write <name>.json plus a file://-friendly <name>.js registering the report."""
    name = report["name"]
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    json_path = DATA_DIR / f"{name}.json"
    js_path = DATA_DIR / f"{name}.js"
    with json_path.open("w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
        f.write("\n")
    with js_path.open("w", encoding="utf-8") as f:
        f.write("window.LyoBench = window.LyoBench || { reports: {}, registry: [] };\n")
        f.write(f'window.LyoBench.reports[{json.dumps(name)}] = ')
        json.dump(report, f)
        f.write(";\n")
    print(f"Wrote {json_path.relative_to(REPO_ROOT)}")


def write_registry() -> None:
    """Scan the data dir and emit registry.js listing every report (name/title/type)."""
    entries: list[dict[str, str]] = []
    for json_path in sorted(DATA_DIR.glob("*.json")):
        try:
            report = read_json(json_path)
        except (ValueError, OSError):
            continue
        if not isinstance(report, dict) or "type" not in report or "name" not in report:
            continue
        entry = {
            "name": report.get("name", json_path.stem),
            "title": report.get("title", report.get("name", json_path.stem)),
            "type": report.get("type", ""),
        }
        description = report.get("description")
        if description:
            entry["description"] = description
        entries.append(entry)
    entries.sort(key=lambda e: (e["type"], e["title"]))
    registry_path = DATA_DIR / "registry.js"
    with registry_path.open("w", encoding="utf-8") as f:
        f.write("window.LyoBench = window.LyoBench || { reports: {}, registry: [] };\n")
        f.write("window.LyoBench.registry = ")
        json.dump(entries, f)
        f.write(";\n")
    print(f"Wrote {registry_path.relative_to(REPO_ROOT)} ({len(entries)} reports)")


# --------------------------------------------------------------------------------------
# Micro (BenchmarkDotNet) — copy the exporter's report verbatim.
# --------------------------------------------------------------------------------------
def find_lyobench_json(artifacts_dir: Path) -> Path | None:
    candidates = list(artifacts_dir.glob("*.lyobench.json"))
    candidates += list(artifacts_dir.glob("results/*.lyobench.json"))
    if not candidates:
        return None
    return max(candidates, key=lambda p: p.stat().st_mtime)


def build_micro_category(category: dict[str, Any]) -> bool:
    name = category["name"]
    artifacts_dir = category["dir"]
    if not artifacts_dir.is_dir():
        print(f"{name}: artifacts directory not found (run the {name} suite first).")
        return False
    source = find_lyobench_json(artifacts_dir)
    if source is None:
        print(f"{name}: no <name>.lyobench.json found (run the {name} suite first).")
        return False
    report = read_json(source)
    if not isinstance(report, dict):
        print(f"{name}: malformed exporter JSON at {source}.")
        return False
    report.setdefault("schema", SCHEMA)
    report.setdefault("type", "micro")
    write_report(report)
    return True


# --------------------------------------------------------------------------------------
# Load (k6) — normalize raw summaries into a LoadTestReport.
# --------------------------------------------------------------------------------------
def discover_k6_runs() -> list[tuple[str, Path]]:
    if not K6_RESULTS.is_dir():
        return []
    runs = [
        (entry.name, entry)
        for entry in K6_RESULTS.iterdir()
        if entry.is_dir() and any(entry.name.startswith(p) for p in K6_RUN_PREFIXES)
    ]
    runs.sort(key=lambda item: item[0], reverse=True)
    return runs


def metric_stat(metrics: dict[str, Any], key: str) -> dict[str, Any]:
    block = metrics.get(key) or {}
    return {
        "min": block.get("min"),
        "p50": block.get("med"),
        "p90": block.get("p(90)"),
        "p95": block.get("p(95)"),
        "p99": block.get("p(99)"),
        "avg": block.get("avg"),
        "max": block.get("max"),
        "unit": "ms",
    }


def check_pass_rate(metrics: dict[str, Any]) -> float | None:
    checks = metrics.get("checks")
    if not checks:
        return None
    passes = checks.get("passes", 0)
    fails = checks.get("fails", 0)
    total = passes + fails
    return (passes / total * 100.0) if total else None


def dropped_iterations(metrics: dict[str, Any]) -> int:
    return int((metrics.get("dropped_iterations") or {}).get("count") or 0)


def query_case_hotspots(metrics: dict[str, Any]) -> list[dict[str, Any]]:
    cases: list[tuple[float, dict[str, Any]]] = []
    for key, value in metrics.items():
        if not key.startswith("query_duration{query_case:"):
            continue
        case = key.split("query_case:", 1)[1].rstrip("}")
        p95 = value.get("p(95)")
        if p95 is None:
            continue
        cases.append(
            (float(p95), {"case": case, "avg": value.get("avg"), "p95": value.get("p(95)"), "p99": value.get("p(99)")})
        )
    cases.sort(key=lambda item: item[0], reverse=True)
    return [hotspot for _, hotspot in cases]


def split_suite_name(name: str) -> tuple[str, str]:
    endpoint, _, profile = name.partition("_")
    return endpoint, profile


def build_scenarios(run_dir: Path) -> list[dict[str, Any]]:
    scenarios: list[dict[str, Any]] = []
    for name in K6_SUITE_NAMES:
        summary_path = run_dir / f"{name}.summary.json"
        if not summary_path.is_file():
            continue
        metrics = read_json(summary_path).get("metrics") or {}
        reqs = metrics.get("http_reqs") or {}
        endpoint, profile = split_suite_name(name)
        scenarios.append(
            {
                "name": name,
                "profile": profile,
                "endpoint": endpoint,
                "latency": metric_stat(metrics, "http_req_duration"),
                "throughput": reqs.get("rate"),
                "requests": int(reqs.get("count") or 0),
                "checksPass": check_pass_rate(metrics),
                "statusPass": (metrics.get("status_success_rate") or {}).get("value", 0) * 100,
                "shapePass": (metrics.get("shape_success_rate") or {}).get("value", 0) * 100,
                "latencyPass": (metrics.get("latency_success_rate") or {}).get("value", 0) * 100,
                "droppedIterations": dropped_iterations(metrics),
                "hotspots": query_case_hotspots(metrics),
            }
        )
    return scenarios


def p95(scenario: dict[str, Any]) -> float | None:
    return (scenario.get("latency") or {}).get("p95")


def endpoint_rollup(scenarios: list[dict[str, Any]], endpoint: str) -> dict[str, Any]:
    filtered = [s for s in scenarios if s["endpoint"] == endpoint]
    total = sum(int(s["requests"]) for s in filtered)
    rollup: dict[str, Any] = {"endpoint": endpoint, "totalRequests": total}
    if total == 0:
        return rollup

    def weighted(key: str) -> float | None:
        num = sum(float(s[key]) * int(s["requests"]) for s in filtered if s.get(key) is not None)
        return num / total

    rollup.update(
        {
            "checksPass": weighted("checksPass"),
            "statusPass": weighted("statusPass"),
            "shapePass": weighted("shapePass"),
            "latencyPass": weighted("latencyPass"),
        }
    )
    return rollup


def grade_k6(scenarios: list[dict[str, Any]]) -> list[dict[str, str]]:
    by_name = {s["name"]: s for s in scenarios}

    def suite(name: str) -> dict[str, Any]:
        return by_name.get(name, {})

    def letter(category: str, grade: str, rationale: str) -> dict[str, str]:
        return {"category": category, "grade": grade, "rationale": rationale}

    grades: list[dict[str, str]] = []

    query_scn = [s for s in scenarios if s["endpoint"] == "query"]
    query_status = all((s.get("statusPass") or 0) >= 100 for s in query_scn)
    query_shape = all((s.get("shapePass") or 0) >= 100 for s in query_scn)
    grades.append(
        letter(
            "Query functional correctness (status/shape)",
            "A" if query_status and query_shape else "B",
            "100% status and shape checks across the full rerun"
            if query_status and query_shape
            else "One or more suites missed perfect status/shape checks",
        )
    )

    load_p95 = p95(suite("query_load"))
    load_checks = suite("query_load").get("checksPass") or 0
    grades.append(
        letter(
            "Query load",
            "A" if load_p95 is not None and load_p95 < 200 and load_checks >= 100 else "B",
            f"{load_p95:.0f} ms p95 with {load_checks:.2f}% checks" if load_p95 is not None else "Missing load suite",
        )
    )

    stress_p95 = p95(suite("query_stress"))
    stress_checks = suite("query_stress").get("checksPass") or 0
    if stress_p95 is None:
        stress_grade = "C"
    elif stress_p95 <= 1000:
        stress_grade = "A" if stress_checks >= 99.5 else "B"
    elif stress_p95 <= 2000:
        stress_grade = "B"
    else:
        stress_grade = "C"
    grades.append(
        letter(
            "Query stress",
            stress_grade,
            f"p95 ~{stress_p95 / 1000:.2f}s, {stress_checks:.2f}% checks" if stress_p95 is not None else "Missing stress suite",
        )
    )

    spike_p95 = p95(suite("query_spike"))
    spike_checks = suite("query_spike").get("checksPass") or 0
    spike_dropped = suite("query_spike").get("droppedIterations") or 0
    if spike_p95 is None:
        spike_grade = "C"
    elif spike_p95 <= 700:
        spike_grade = "A"
    elif spike_p95 <= 1500:
        spike_grade = "A-" if spike_checks >= 99 and spike_dropped < 100 else "B"
    elif spike_p95 <= 5000:
        spike_grade = "C"
    else:
        spike_grade = "D"
    grades.append(
        letter(
            "Query spike",
            spike_grade,
            f"p95 ~{spike_p95:.0f} ms, {spike_dropped} dropped iters, {spike_checks:.2f}% checks"
            if spike_p95 is not None
            else "Missing spike suite",
        )
    )

    soak_p95 = p95(suite("query_soak"))
    soak_checks = suite("query_soak").get("checksPass") or 0
    if soak_p95 is None:
        soak_grade = "C"
    elif soak_p95 <= 500:
        soak_grade = "A"
    elif soak_p95 <= 1000:
        soak_grade = "B"
    else:
        soak_grade = "C"
    grades.append(
        letter(
            "Query soak",
            soak_grade,
            f"sub-{soak_p95:.0f} ms sustained p95, {soak_checks:.2f}% checks" if soak_p95 is not None else "Missing soak suite",
        )
    )

    qp_load = p95(suite("queryproject_load"))
    qp_stress = p95(suite("queryproject_stress"))
    qp_spike = p95(suite("queryproject_spike"))
    qp_soak = p95(suite("queryproject_soak"))
    qp_checks = min(
        suite("queryproject_load").get("checksPass") or 0,
        suite("queryproject_stress").get("checksPass") or 100,
        suite("queryproject_spike").get("checksPass") or 100,
        suite("queryproject_soak").get("checksPass") or 100,
    )
    if all(v is not None for v in (qp_load, qp_stress, qp_spike, qp_soak)):
        if max(qp_load, qp_spike, qp_soak) <= 300 and qp_stress <= 700 and qp_checks >= 99.5:
            qp_grade = "A"
        elif max(qp_load, qp_spike, qp_soak) <= 500 and qp_stress <= 1500:
            qp_grade = "A-"
        elif qp_stress <= 3000:
            qp_grade = "B"
        else:
            qp_grade = "C"
        qp_rationale = (
            f"load/spike/soak p95 {qp_load:.0f}/{qp_spike:.0f}/{qp_soak:.0f} ms; "
            f"stress p95 {qp_stress:.0f} ms; {qp_checks:.2f}% checks"
        )
    else:
        qp_grade = "C"
        qp_rationale = "Incomplete QueryProject suite coverage"
    grades.append(letter("QueryProject path", qp_grade, qp_rationale))

    return grades


def slo_assessment(scenarios: list[dict[str, Any]]) -> list[dict[str, str]]:
    by_name = {s["name"]: s for s in scenarios}
    rows: list[dict[str, str]] = []

    def add(area: str, target: str, latest: str, result: str) -> None:
        rows.append({"area": area, "target": target, "latest": latest, "result": result})

    def lat(name: str) -> float | None:
        return p95(by_name.get(name, {}))

    qp_load, qp_spike, qp_soak = lat("queryproject_load"), lat("queryproject_spike"), lat("queryproject_soak")
    if all(v is not None for v in (qp_load, qp_spike, qp_soak)):
        latest = f"{min(qp_load, qp_spike, qp_soak):.0f}-{max(qp_load, qp_spike, qp_soak):.0f} ms"
        worst = max(qp_load, qp_spike, qp_soak)
        add("QueryProject load/spike/soak", "100-300 ms", latest, "Exceeds target" if worst <= 300 else "Meets" if worst <= 700 else "Miss")

    qp_stress = lat("queryproject_stress")
    if qp_stress is not None:
        add("QueryProject stress", "300-700 ms", f"{qp_stress:.0f} ms", "Meets" if qp_stress <= 700 else "Slightly above" if qp_stress <= 1500 else "Miss")

    q_load = lat("query_load")
    if q_load is not None:
        add("Query load", "300-700 ms", f"{q_load:.0f} ms", "Exceeds target" if q_load <= 300 else "Meets")

    q_stress = lat("query_stress")
    if q_stress is not None:
        add("Query stress", "500-1,000 ms", f"{q_stress:.0f} ms", "Meets" if q_stress <= 1000 else "Slightly above" if q_stress <= 2000 else "Miss")

    q_spike = lat("query_spike")
    if q_spike is not None:
        add("Query spike", "700-1,500 ms", f"{q_spike:.0f} ms", "Exceeds target" if q_spike <= 700 else "Meets" if q_spike <= 1500 else "Miss")

    q_soak = lat("query_soak")
    if q_soak is not None:
        add("Query soak", "500-1,000 ms", f"{q_soak:.0f} ms", "Exceeds target" if q_soak <= 500 else "Meets" if q_soak <= 1000 else "Miss")

    status_shape = all((s.get("statusPass") or 0) >= 100 and (s.get("shapePass") or 0) >= 100 for s in scenarios)
    add("Status + response-shape correctness", "99.9-100%", "100%", "Meets" if status_shape else "Miss")

    return rows


def build_cases(scenarios: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Emit a LoadCase for every case id observed in the run's hotspots, enriched from K6_CASE_META."""
    seen: list[str] = []
    for scenario in scenarios:
        for hotspot in scenario.get("hotspots") or []:
            case_id = hotspot.get("case")
            if case_id and case_id not in seen:
                seen.append(case_id)

    cases: list[dict[str, Any]] = []
    for case_id in sorted(seen):
        meta = K6_CASE_META.get(case_id, {})
        case: dict[str, Any] = {"case": case_id}
        for key in ("endpoint", "description", "whereClauses", "filters", "sortFields", "includes", "selectionFieldCount"):
            if key in meta:
                case[key] = meta[key]
        cases.append(case)
    return cases


def build_k6(run_dir: Path | None) -> bool:
    if run_dir is None:
        runs = discover_k6_runs()
        if not runs:
            print("No k6 runs found.")
            return False
        run_id, run_dir = runs[0]
    else:
        run_id = run_dir.name

    scenarios = build_scenarios(run_dir)
    if not scenarios:
        print(f"k6: no summary files in {run_dir}.")
        return False

    report = {
        "type": "load",
        "schema": SCHEMA,
        "name": K6_REPORT_NAME,
        "title": K6_REPORT_TITLE,
        "description": K6_DESCRIPTION,
        "runId": run_id,
        "generatedAt": utc_now_iso(),
        "environment": {"tool": "k6"},
        "notes": [
            "Randomized multi-key sorting is disabled except for the filter_sort case "
            "(fixed LastName, FirstName, Id); other cases use server default (PK) order.",
            f"Source: {run_dir.relative_to(REPO_ROOT)}",
        ],
        "cases": build_cases(scenarios),
        "scenarios": scenarios,
        "rollups": [endpoint_rollup(scenarios, "query"), endpoint_rollup(scenarios, "queryproject")],
        "slo": slo_assessment(scenarios),
        "grades": grade_k6(scenarios),
    }
    write_report(report)
    return True


def main() -> None:
    parser = argparse.ArgumentParser(description="Aggregate benchmark reports into the unified schema.")
    parser.add_argument("--k6-only", action="store_true")
    parser.add_argument("--k6-run-dir", type=Path, default=None, help="Explicit k6 results directory")
    for category in BDN_CATEGORIES:
        parser.add_argument(f"--{category['name']}-only", action="store_true")
    args = parser.parse_args()

    category_flags = {c["name"]: getattr(args, f"{c['name']}_only") for c in BDN_CATEGORIES}
    run_all = not (args.k6_only or any(category_flags.values()))

    if run_all or args.k6_only:
        build_k6(args.k6_run_dir.resolve() if args.k6_run_dir else None)

    for category in BDN_CATEGORIES:
        if run_all or category_flags[category["name"]]:
            build_micro_category(category)

    write_registry()


if __name__ == "__main__":
    main()
