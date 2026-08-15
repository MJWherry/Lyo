#!/usr/bin/env python3
"""Aggregate benchmark reports into the unified ``lyo.bench/v1`` schema under docs/benchmarks/data/.

Two sources feed one schema:

* **Micro (BenchmarkDotNet)** — the C# ``LyoBenchmarkExporter`` already emits
  ``<name>.lyobench.json`` (``type: "micro"``). This script just copies those files into the data
  dir (no CSV parsing).
* **Load (k6)** — k6 cannot emit the schema itself, so this script normalizes the raw
  ``*.summary.json`` files into a ``LoadTestReport`` (``type: "load"``).

For every report it writes ``<name>.json`` (portable) and ``<name>.js``
(``window.LyoBench.reports["<name>"] = …`` for file:// viewing), archives a timestamped
snapshot under ``docs/benchmarks/history/<name>/``, attaches Δ-vs-prior-run fields and a
run-history summary, then a ``registry.js`` listing all reports for the static
``docs/benchmarks/index.html`` hub. By default it also copies history into
``apps/gateway/public/benchmarks/history`` when that tree exists (``--sync-portfolio-only`` is a local-dev alias; no-op after the marketing site moved to Lyo-Public).
"""

from __future__ import annotations

import argparse
import copy
import json
import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from lyo_tooling.bench import sync_portfolio_history  # noqa: E402
from lyo_tooling.dotnet import (  # noqa: E402
    REPO_ROOT,
    find_project_csproj,
    load_central_package_versions,
    tfm_condition_applies,
)

DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"
HISTORY_DIR = REPO_ROOT / "docs" / "benchmarks" / "history"
SCHEMA = "lyo.bench/v1"

# Benchmark executables target net10.0 only; ItemGroups conditioned on other TFMs don't apply.
BENCHMARK_TFM = "net10.0"

# BenchmarkDotNet defaults to cwd/BenchmarkDotNet.Artifacts when --artifacts is omitted.
FALLBACK_ARTIFACT_ROOTS = (
    REPO_ROOT / "BenchmarkDotNet.Artifacts",
    REPO_ROOT / "Lyo.Net" / "BenchmarkDotNet.Artifacts",
)

K6_RESULTS = REPO_ROOT / "k6" / "framework-person" / "results"
# Run dirs are named "<optional-prefix->YYYYMMDD-HHMMSS" (run_all.py default is the bare timestamp).
K6_RUN_TIMESTAMP = re.compile(r"(\d{8}-\d{6})$")
# Legacy: endpoint_profile.summary.json
# Matrix cell: endpoint_profile_intensity_cacheMode.summary.json
K6_SUMMARY_NAME = re.compile(
    r"^(?P<endpoint>query|queryproject|queryroot)_"
    r"(?P<profile>load|stress|spike|soak|ceiling)"
    r"(?:_(?P<intensity>low|med|high)_(?P<cache>cached|uncached))?$"
)
K6_LEGACY_SUITE_NAMES = [
    "query_load",
    "query_stress",
    "query_spike",
    "query_soak",
    "queryproject_load",
    "queryproject_stress",
    "queryproject_spike",
    "queryproject_soak",
    "queryroot_load",
    "queryroot_stress",
    "queryroot_spike",
    "queryroot_soak",
]
K6_REPORT_NAME = "query-api"
K6_REPORT_TITLE = "Query API (k6)"

# Suite-level methodology so the latencies are interpretable on their own.
K6_DESCRIPTION = (
    "k6 load/stress/spike/soak/ceiling matrix against the Lyo person API across intensity "
    "(low/med/high) and cache mode (uncached/cached). /person/QueryConcrete returns full "
    "Person entities; /person/QueryProject returns field-projected rows; POST /Query is root "
    "From/Joins sparse projection. Uncached iterations page 100-300 persons (varied by VU/iteration); "
    "cached mode pins shapes for query-cache hits. Query generation uses a fixed RANDOM_SEED. "
    "The 'cases' below describe the request shape behind each scenario; hotspots reference these case ids."
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
    "root_flat": {
        "endpoint": "queryroot",
        "description": "POST /Query From Person with no joins; Select FirstName/LastName (From-side paging).",
        "selectionFieldCount": 2,
        "includes": [],
    },
    "root_left_join": {
        "endpoint": "queryroot",
        "description": "POST /Query Person left join ContactAddress; fan-out collapsed to nested bags per person.",
        "selectionFieldCount": 3,
        "includes": [],
    },
    "root_chained_join": {
        "endpoint": "queryroot",
        "description": "POST /Query Person→ContactAddress→Address chained left joins with sparse Select.",
        "selectionFieldCount": 4,
        "includes": [],
    },
    "root_chained_exact_count": {
        "endpoint": "queryroot",
        "description": "Same chained joins as root_chained_join with TotalCountMode=Exact (From-side count).",
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
    {"name": "filestorage", "dir": _artifacts("Data", "FileStorage", "Lyo.FileStorage.Benchmarks")},
]


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def read_json(path: Path) -> Any:
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def _history_dir(name: str) -> Path:
    return HISTORY_DIR / name


def _history_sort_key(path: Path) -> tuple[str, float]:
    """Order snapshots oldest-first by embedded timestamp prefix, then mtime."""
    stem = path.stem
    if len(stem) >= 16 and stem[8] == "T":
        return (stem[:16], path.stat().st_mtime)
    return (stem, path.stat().st_mtime)


def list_history_paths(name: str) -> list[Path]:
    hist_dir = _history_dir(name)
    if not hist_dir.is_dir():
        return []
    return sorted(hist_dir.glob("*.json"), key=_history_sort_key)


def _run_id_datetime(report: dict[str, Any]) -> datetime | None:
    """Run time embedded in the runId (k6 run-dir stamp, local time), if any."""
    match = K6_RUN_TIMESTAMP.search(str(report.get("runId") or ""))
    if not match:
        return None
    try:
        return datetime.strptime(match.group(1), "%Y%m%d-%H%M%S").astimezone()
    except ValueError:
        return None


def _archive_stamp(report: dict[str, Any]) -> str:
    run_dt = _run_id_datetime(report)
    if run_dt is not None:
        return run_dt.astimezone(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    for key in ("runEnded", "runStarted", "generatedAt"):
        raw = report.get(key)
        if not raw:
            continue
        try:
            dt = datetime.fromisoformat(str(raw).replace("Z", "+00:00"))
            return dt.astimezone(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        except ValueError:
            continue
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def archive_report(report: dict[str, Any]) -> Path:
    """Persist a timestamped snapshot under docs/benchmarks/history/<name>/."""
    clean = strip_view_metadata(report)
    name = clean["name"]
    run_id = clean.get("runId")
    for path in list_history_paths(name):
        snap = strip_view_metadata(_load_snapshot(path) or {})
        if snap.get("runId") == run_id:
            print(f"{name}: snapshot already archived for run {run_id}")
            return path

    hist_dir = _history_dir(name)
    hist_dir.mkdir(parents=True, exist_ok=True)
    run_id = run_id or "run"
    safe_run = re.sub(r"[^\w.-]+", "_", str(run_id))[:80]
    path = hist_dir / f"{_archive_stamp(clean)}_{safe_run}.json"
    with path.open("w", encoding="utf-8") as f:
        json.dump(clean, f, indent=2)
        f.write("\n")
    print(f"Archived {path.relative_to(REPO_ROOT)}")
    return path


def _history_entry(filename: str, snap: dict[str, Any], *, is_current: bool) -> dict[str, Any]:
    entry: dict[str, Any] = {
        "file": filename,
        "runId": snap.get("runId"),
        "runStarted": snap.get("runStarted"),
        "runEnded": snap.get("runEnded"),
        "generatedAt": snap.get("generatedAt"),
        "isCurrent": is_current,
    }
    if snap.get("type") == "micro":
        means = [
            m.get("meanNs")
            for g in snap.get("groups") or []
            for m in g.get("measurements") or []
            if m.get("meanNs") is not None
        ]
        if means:
            entry["measurementCount"] = len(means)
            entry["medianMeanNs"] = sorted(means)[len(means) // 2]
    elif snap.get("type") == "load":
        p95s = [
            (s.get("latency") or {}).get("p95")
            for s in snap.get("scenarios") or []
            if (s.get("latency") or {}).get("p95") is not None
        ]
        if p95s:
            entry["scenarioCount"] = len(p95s)
            entry["medianP95Ms"] = sorted(p95s)[len(p95s) // 2]
    return entry


def write_history_snapshot(name: str, filename: str, report: dict[str, Any]) -> None:
    hist_dir = _history_dir(name)
    hist_dir.mkdir(parents=True, exist_ok=True)
    json_path = hist_dir / filename
    js_path = hist_dir / f"{Path(filename).stem}.js"
    with json_path.open("w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
        f.write("\n")
    with js_path.open("w", encoding="utf-8") as f:
        f.write("window.LyoBench = window.LyoBench || { reports: {}, history: {}, historyIndex: {} };\n")
        f.write(f"window.LyoBench.history[{json.dumps(name)}] = window.LyoBench.history[{json.dumps(name)}] || {{}};\n")
        f.write(f"window.LyoBench.history[{json.dumps(name)}][{json.dumps(filename)}] = ")
        json.dump(report, f)
        f.write(";\n")


def write_history_index(name: str, entries: list[dict[str, Any]]) -> None:
    hist_dir = _history_dir(name)
    hist_dir.mkdir(parents=True, exist_ok=True)
    index_path = hist_dir / "index.js"
    with index_path.open("w", encoding="utf-8") as f:
        f.write("window.LyoBench = window.LyoBench || { reports: {}, history: {}, historyIndex: {} };\n")
        f.write(f"window.LyoBench.historyIndex[{json.dumps(name)}] = ")
        json.dump(entries, f)
        f.write(";\n")
    print(f"Wrote {index_path.relative_to(REPO_ROOT)} ({len(entries)} snapshots)")


def rebuild_history_views(name: str) -> dict[str, Any] | None:
    """Rebuild every archived snapshot with Δ vs the immediately prior run; return the latest view."""
    paths = list_history_paths(name)
    if not paths:
        return None

    snapshots: list[tuple[str, dict[str, Any]]] = []
    for path in paths:
        snap = strip_view_metadata(_load_snapshot(path) or {})
        if snap:
            snapshots.append((path.name, snap))

    if not snapshots:
        return None

    latest_index = max(range(len(snapshots)), key=lambda i: _report_rank(snapshots[i][1]))
    history_meta = [
        _history_entry(filename, snap, is_current=(index == latest_index))
        for index, (filename, snap) in enumerate(snapshots)
    ]

    latest_view: dict[str, Any] | None = None
    for index, (filename, snap) in enumerate(snapshots):
        previous = snapshots[index - 1][1] if index > 0 else None
        view = copy.deepcopy(snap)
        if previous:
            if view.get("type") == "micro":
                attach_micro_deltas(view, previous)
            elif view.get("type") == "load":
                attach_load_deltas(view, previous)
        view["history"] = history_meta
        if previous:
            view["deltaBaseline"] = {
                "kind": "previousRun",
                "runId": previous.get("runId"),
                "runStarted": previous.get("runStarted"),
                "runEnded": previous.get("runEnded"),
            }
        else:
            view.pop("deltaBaseline", None)
        write_history_snapshot(name, filename, view)
        if index == latest_index:
            latest_view = view

    write_history_index(name, history_meta)
    return latest_view


def build_history_meta(name: str) -> list[dict[str, Any]]:
    """Summarize every snapshot on disk (oldest first); marks the newest as current."""
    paths = list_history_paths(name)
    snapshots = []
    for path in paths:
        snap = strip_view_metadata(_load_snapshot(path) or {})
        if snap:
            snapshots.append((path.name, snap))
    if not snapshots:
        return []
    latest_index = max(range(len(snapshots)), key=lambda i: _report_rank(snapshots[i][1]))
    return [
        _history_entry(filename, snap, is_current=(index == latest_index))
        for index, (filename, snap) in enumerate(snapshots)
    ]


def _parse_report_timestamp(report: dict[str, Any]) -> float:
    """When the benchmark actually ran, for report ranking.

    The runId's embedded run-dir stamp wins; generatedAt is checked LAST because it is
    refreshed to 'now' on every manifest rebuild — trusting it first let a republished
    old run permanently outrank a genuinely newer one on every subsequent rebuild.
    """
    run_dt = _run_id_datetime(report)
    if run_dt is not None:
        return run_dt.timestamp()
    for key in ("runEnded", "runStarted", "generatedAt"):
        raw = report.get(key)
        if not raw:
            continue
        try:
            return datetime.fromisoformat(str(raw).replace("Z", "+00:00")).timestamp()
        except ValueError:
            continue
    return 0.0


def _report_richness(report: dict[str, Any]) -> int:
    if report.get("type") == "load":
        return len(report.get("scenarios") or [])
    return sum(len(group.get("measurements") or []) for group in report.get("groups") or [])


def _report_rank(report: dict[str, Any]) -> tuple[int, float | int, float | int]:
    """Higher is better.

    Load reports: joined, then richness, then timestamp — a partial/ceiling run must not
    displace a fuller matrix just because it finished later.

    Micro reports: joined, then timestamp, then richness — intentional suite changes (e.g.
    larger payloads → fewer Params) and Docker exports must not lose to an older host
    ``data/<name>.json`` that happens to have more measurements.
    """
    ts = _parse_report_timestamp(report)
    joined = 1 if "joined" in str(report.get("runId", "")).lower() else 0
    richness = _report_richness(report)
    if report.get("type") == "load":
        return (joined, richness, ts)
    return (joined, ts, richness)


def _collect_report_candidates(name: str) -> list[dict[str, Any]]:
    """Every known snapshot for a suite: dashboard data file plus archived history."""
    candidates: list[dict[str, Any]] = []
    seen_run_ids: set[str | None] = set()
    data_path = DATA_DIR / f"{name}.json"
    if data_path.is_file():
        snap = strip_view_metadata(_load_snapshot(data_path) or {})
        if snap:
            candidates.append(snap)
            seen_run_ids.add(snap.get("runId"))
    for path in list_history_paths(name):
        snap = strip_view_metadata(_load_snapshot(path) or {})
        if snap and snap.get("runId") not in seen_run_ids:
            candidates.append(snap)
            seen_run_ids.add(snap.get("runId"))
    return candidates


def _best_report(name: str, incoming: dict[str, Any]) -> dict[str, Any]:
    best = incoming
    for candidate in _collect_report_candidates(name):
        if _report_rank(candidate) > _report_rank(best):
            best = candidate
    return best


def publish_report(report: dict[str, Any]) -> None:
    """Archive the incoming artifact, pick the ranked-best snapshot for the dashboard, rebuild views."""
    name = report["name"]
    incoming = strip_view_metadata(report)
    # Always archive what just ran — even if an older snapshot still ranks as "latest" for
    # the dashboard. Docker only persists mounted paths; discarding the artifact loses the run.
    archive_report(incoming)
    best = _best_report(name, incoming)
    if best.get("runId") != incoming.get("runId"):
        print(
            f"{name}: publishing {best.get('runId')} "
            f"(incoming {incoming.get('runId')} archived but ranked lower)"
        )
    latest_view = rebuild_history_views(name)
    if latest_view is None:
        latest_view = copy.deepcopy(best)
        latest_view["history"] = []
    write_report(latest_view)


def _pct_delta(current: float | None, previous: float | None) -> float | None:
    if current is None or previous is None or previous == 0:
        return None
    return (current - previous) / previous * 100.0


def _param_key(parameters: dict[str, Any] | None) -> tuple[tuple[str, str], ...]:
    return tuple(sorted((k, str(v)) for k, v in (parameters or {}).items()))


def _micro_measurement_key(group: str, measurement: dict[str, Any]) -> tuple[Any, ...]:
    return (group, measurement.get("method"), _param_key(measurement.get("parameters")))


def _comparison_row_key(axis: str, row: dict[str, Any]) -> tuple[Any, ...]:
    return (axis, row.get("algorithm"), _param_key(row.get("parameters")))


def _load_snapshot(path: Path) -> dict[str, Any] | None:
    try:
        data = read_json(path)
    except (ValueError, OSError, json.JSONDecodeError):
        return None
    return data if isinstance(data, dict) else None


def _previous_snapshot(name: str) -> dict[str, Any] | None:
    """Deprecated: kept for compatibility; deltas now use historical averages."""
    paths = list_history_paths(name)
    if paths:
        return _load_snapshot(paths[-1])
    current_path = DATA_DIR / f"{name}.json"
    if current_path.is_file():
        return _load_snapshot(current_path)
    return None


def strip_view_metadata(report: dict[str, Any]) -> dict[str, Any]:
    """Return a copy without dashboard-only fields (history, deltas, baseline refs)."""
    clean = copy.deepcopy(report)
    clean.pop("history", None)
    clean.pop("deltaBaseline", None)
    for group in clean.get("groups") or []:
        for measurement in group.get("measurements") or []:
            measurement.pop("deltaMeanPct", None)
            measurement.pop("deltaAllocPct", None)
    comparison = clean.get("comparison")
    if comparison:
        for group in comparison.get("groups") or []:
            for row in group.get("rows") or []:
                row.pop("deltaMeanPct", None)
                row.pop("deltaAllocPct", None)
    for scenario in clean.get("scenarios") or []:
        scenario.pop("deltaP95Pct", None)
        scenario.pop("deltaP99Pct", None)
        scenario.pop("deltaThroughputPct", None)
    return clean


def _mean(values: list[float]) -> float | None:
    if not values:
        return None
    return sum(values) / len(values)


def attach_micro_deltas_vs_average(report: dict[str, Any], others: list[dict[str, Any]]) -> None:
    if not others:
        return
    mean_values: dict[tuple[Any, ...], list[float]] = {}
    alloc_values: dict[tuple[Any, ...], list[float]] = {}
    for snap in others:
        for group in snap.get("groups") or []:
            gname = group.get("name", "")
            for measurement in group.get("measurements") or []:
                key = _micro_measurement_key(gname, measurement)
                if measurement.get("meanNs") is not None:
                    mean_values.setdefault(key, []).append(float(measurement["meanNs"]))
                if measurement.get("allocatedBytes") is not None:
                    alloc_values.setdefault(key, []).append(float(measurement["allocatedBytes"]))

    for group in report.get("groups") or []:
        gname = group.get("name", "")
        for measurement in group.get("measurements") or []:
            key = _micro_measurement_key(gname, measurement)
            avg_mean = _mean(mean_values.get(key, []))
            avg_alloc = _mean(alloc_values.get(key, []))
            if avg_mean is not None:
                measurement["deltaMeanPct"] = _pct_delta(measurement.get("meanNs"), avg_mean)
            if avg_alloc is not None:
                measurement["deltaAllocPct"] = _pct_delta(measurement.get("allocatedBytes"), avg_alloc)

    mean_cmp: dict[tuple[Any, ...], list[float]] = {}
    alloc_cmp: dict[tuple[Any, ...], list[float]] = {}
    for snap in others:
        for group in (snap.get("comparison") or {}).get("groups") or []:
            axis = group.get("axis", "")
            for row in group.get("rows") or []:
                key = _comparison_row_key(axis, row)
                if row.get("meanNs") is not None:
                    mean_cmp.setdefault(key, []).append(float(row["meanNs"]))
                if row.get("allocatedBytes") is not None:
                    alloc_cmp.setdefault(key, []).append(float(row["allocatedBytes"]))

    comparison = report.get("comparison")
    if not comparison:
        return
    for group in comparison.get("groups") or []:
        axis = group.get("axis", "")
        for row in group.get("rows") or []:
            key = _comparison_row_key(axis, row)
            avg_mean = _mean(mean_cmp.get(key, []))
            avg_alloc = _mean(alloc_cmp.get(key, []))
            if avg_mean is not None:
                row["deltaMeanPct"] = _pct_delta(row.get("meanNs"), avg_mean)
            if avg_alloc is not None:
                row["deltaAllocPct"] = _pct_delta(row.get("allocatedBytes"), avg_alloc)


def attach_load_deltas_vs_average(report: dict[str, Any], others: list[dict[str, Any]]) -> None:
    if not others:
        return
    p95_values: dict[str, list[float]] = {}
    p99_values: dict[str, list[float]] = {}
    throughput_values: dict[str, list[float]] = {}
    for snap in others:
        for scenario in snap.get("scenarios") or []:
            sname = scenario.get("name")
            if not sname:
                continue
            lat = scenario.get("latency") or {}
            if lat.get("p95") is not None:
                p95_values.setdefault(sname, []).append(float(lat["p95"]))
            if lat.get("p99") is not None:
                p99_values.setdefault(sname, []).append(float(lat["p99"]))
            if scenario.get("throughput") is not None:
                throughput_values.setdefault(sname, []).append(float(scenario["throughput"]))

    for scenario in report.get("scenarios") or []:
        sname = scenario.get("name")
        if not sname:
            continue
        lat = scenario.get("latency") or {}
        avg_p95 = _mean(p95_values.get(sname, []))
        avg_p99 = _mean(p99_values.get(sname, []))
        avg_tp = _mean(throughput_values.get(sname, []))
        if avg_p95 is not None:
            scenario["deltaP95Pct"] = _pct_delta(lat.get("p95"), avg_p95)
        if avg_p99 is not None:
            scenario["deltaP99Pct"] = _pct_delta(lat.get("p99"), avg_p99)
        if avg_tp is not None:
            scenario["deltaThroughputPct"] = _pct_delta(scenario.get("throughput"), avg_tp)


def attach_micro_deltas(report: dict[str, Any], previous: dict[str, Any] | None) -> None:
    if previous:
        attach_micro_deltas_vs_average(report, [previous])


def attach_load_deltas(report: dict[str, Any], previous: dict[str, Any] | None) -> None:
    if previous:
        attach_load_deltas_vs_average(report, [previous])


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
        f.write("window.LyoBench = window.LyoBench || { reports: {}, history: {}, historyIndex: {} };\n")
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
        f.write("window.LyoBench = window.LyoBench || { reports: {}, history: {}, historyIndex: {} };\n")
        f.write("window.LyoBench.registry = ")
        json.dump(entries, f)
        f.write(";\n")
    print(f"Wrote {registry_path.relative_to(REPO_ROOT)} ({len(entries)} reports)")


# --------------------------------------------------------------------------------------
# Dependency versions — resolve each benchmark project's PackageReference graph against the
# centralized Directory.Packages.props (Central Package Management). Parsing helpers live
# in scripts/lyo_tooling/dotnet.py (shared with gen_graph.py).
# --------------------------------------------------------------------------------------
def collect_project_packages(csproj: Path, visited: set[Path] | None = None) -> set[str]:
    """Direct plus transitive (via ProjectReference) PackageReference names for a project."""
    if visited is None:
        visited = set()
    resolved = csproj.resolve()
    if resolved in visited or not csproj.is_file():
        return set()
    visited.add(resolved)
    try:
        root = ET.parse(csproj).getroot()
    except ET.ParseError as exc:
        print(f"warning: could not parse {csproj.relative_to(REPO_ROOT)}: {exc}")
        return set()
    packages: set[str] = set()
    for group in root.iter("ItemGroup"):
        if not tfm_condition_applies(group.get("Condition"), BENCHMARK_TFM):
            continue
        for ref in group.findall("PackageReference"):
            name = ref.get("Include")
            if name:
                packages.add(name)
        for ref in group.findall("ProjectReference"):
            include = ref.get("Include")
            if include:
                child = (csproj.parent / include.replace("\\", "/")).resolve()
                packages |= collect_project_packages(child, visited)
    return packages


def project_dependency_versions(name: str, project_dir: Path) -> dict[str, str] | None:
    """Package -> version map for a benchmark project, resolved from the centralized package file."""
    csproj = find_project_csproj(project_dir)
    if csproj is None:
        print(f"{name}: no csproj found in {project_dir.relative_to(REPO_ROOT)}; skipping dependency versions.")
        return None
    packages = collect_project_packages(csproj)
    if not packages:
        return None
    central = load_central_package_versions()
    dependencies: dict[str, str] = {}
    missing: list[str] = []
    for package in sorted(packages, key=str.lower):
        version = central.get(package)
        if version is None:
            missing.append(package)
        dependencies[package] = version or "unknown"
    if missing:
        print(f"{name}: no central version for: {', '.join(missing)}")
    return dependencies


# --------------------------------------------------------------------------------------
# Micro (BenchmarkDotNet) — copy the exporter's report and derive feature grades from its SLAs.
# --------------------------------------------------------------------------------------
def _micro_letter(exceeds: int, meets: int, miss: int, total: int) -> str:
    """Letter rating for a feature from its SLA verdict mix (vs the declared business/computing standards)."""
    if total == 0:
        return "N/A"
    if miss == 0:
        # Everything is within budget: reward how much headroom there is.
        if meets == 0:
            return "A"  # all comfortably under budget
        if exceeds >= meets:
            return "A-"
        return "B"
    # One or more targets missed: grade by how much of the feature still holds.
    ok_fraction = (exceeds + meets) / total
    if ok_fraction >= 0.8:
        return "C"
    if ok_fraction >= 0.5:
        return "D"
    return "F"


def grade_micro(report: dict[str, Any]) -> list[dict[str, str]]:
    """Roll a micro report's per-measurement SLA verdicts up into one letter grade per benchmark class.

    Each benchmark class is treated as a "feature" and rated against the SLA budgets declared on its
    methods (the same Meets/Exceeds/Miss verdicts the C# exporter computed against business/computing
    standards). Classes without any SLA'd measurements are skipped.
    """
    grades: list[dict[str, str]] = []
    for group in report.get("groups") or []:
        verdicts = [m.get("slaResult") for m in (group.get("measurements") or []) if m.get("slaResult")]
        total = len(verdicts)
        if total == 0:
            continue
        exceeds = verdicts.count("Exceeds")
        meets = verdicts.count("Meets")
        miss = verdicts.count("Miss")
        grade = _micro_letter(exceeds, meets, miss, total)
        parts = []
        if exceeds:
            parts.append(f"{exceeds} exceed")
        if meets:
            parts.append(f"{meets} meet")
        if miss:
            parts.append(f"{miss} miss")
        rationale = f"{', '.join(parts)} of {total} SLA target{'s' if total != 1 else ''} vs declared standards"
        grades.append({"category": group.get("name", "?"), "grade": grade, "rationale": rationale})
    return grades


def find_lyobench_json(name: str, *artifact_dirs: Path) -> Path | None:
    """Find the best ``<name>.lyobench.json`` across the project dir and fallback cwd artifact roots."""
    candidates: list[Path] = []
    seen: set[Path] = set()
    for artifacts_dir in artifact_dirs:
        if not artifacts_dir.is_dir():
            continue
        path = artifacts_dir / f"{name}.lyobench.json"
        resolved = path.resolve()
        if path.is_file() and resolved not in seen:
            candidates.append(path)
            seen.add(resolved)
    if not candidates:
        return None
    return max(candidates, key=_lyobench_rank)


def _lyobench_rank(path: Path) -> tuple[int, int, float, float]:
    """Rank candidates: joined, then richness, then newest timestamp, then mtime."""
    report = _load_snapshot(path) or {}
    joined = 1 if "joined" in str(report.get("runId", "")).lower() else 0
    return (joined, _report_richness(report), _parse_report_timestamp(report), path.stat().st_mtime)


def artifact_dirs_for(category: dict[str, Any]) -> list[Path]:
    return [category["dir"], *FALLBACK_ARTIFACT_ROOTS]


def sync_lyobench_source(name: str, source: Path, canonical_dir: Path) -> None:
    """Copy a fallback-root export into the suite's canonical artifacts directory when newer."""
    if not canonical_dir.is_dir():
        return
    canonical = canonical_dir / f"{name}.lyobench.json"
    if source.resolve() == canonical.resolve():
        return
    source_report = strip_view_metadata(_load_snapshot(source) or {})
    if canonical.is_file():
        canonical_report = strip_view_metadata(_load_snapshot(canonical) or {})
        if _report_rank(canonical_report) >= _report_rank(source_report):
            return
    shutil.copy2(source, canonical)
    print(f"{name}: synced {source.relative_to(REPO_ROOT)} -> {canonical.relative_to(REPO_ROOT)}")


def build_micro_category(category: dict[str, Any]) -> bool:
    name = category["name"]
    search_dirs = artifact_dirs_for(category)
    if not any(path.is_dir() for path in search_dirs):
        print(f"{name}: no artifacts directory found (run the {name} suite first).")
        return False
    source = find_lyobench_json(name, *search_dirs)
    if source is None:
        searched = ", ".join(str(path.relative_to(REPO_ROOT)) for path in search_dirs)
        print(f"{name}: no {name}.lyobench.json found (searched: {searched}).")
        return False
    sync_lyobench_source(name, source, category["dir"])
    canonical = category["dir"] / f"{name}.lyobench.json"
    if source.resolve() != canonical.resolve():
        print(f"{name}: using {source.relative_to(REPO_ROOT)}")
    report = read_json(canonical if canonical.is_file() else source)
    if not isinstance(report, dict):
        print(f"{name}: malformed exporter JSON at {source}.")
        return False
    report.setdefault("schema", SCHEMA)
    report.setdefault("type", "micro")
    dependencies = project_dependency_versions(name, category["dir"].parent)
    if dependencies:
        environment = report.setdefault("environment", {})
        if isinstance(environment, dict):
            environment["dependencies"] = dependencies
    grades = grade_micro(report)
    if grades:
        report["grades"] = grades
    publish_report(report)
    return True


# --------------------------------------------------------------------------------------
# Load (k6) — normalize raw summaries into a LoadTestReport.
# --------------------------------------------------------------------------------------
def _k6_run_timestamp(run_id: str) -> str:
    match = K6_RUN_TIMESTAMP.search(run_id)
    return match.group(1) if match else ""


def list_k6_summary_stems(run_dir: Path) -> list[str]:
    """Return summary basenames (without .summary.json) that match matrix or legacy naming."""
    if not run_dir.is_dir():
        return []
    stems: list[str] = []
    for path in sorted(run_dir.glob("*.summary.json")):
        stem = path.name.removesuffix(".summary.json")
        if K6_SUMMARY_NAME.match(stem):
            stems.append(stem)
    return stems


def _k6_run_has_summaries(run_dir: Path) -> bool:
    return bool(list_k6_summary_stems(run_dir))


def discover_k6_runs() -> list[tuple[str, Path]]:
    """Usable run dirs, newest first by the timestamp embedded in the dir name (prefix-agnostic)."""
    if not K6_RESULTS.is_dir():
        return []
    runs = [
        (entry.name, entry)
        for entry in K6_RESULTS.iterdir()
        if entry.is_dir() and _k6_run_timestamp(entry.name) and _k6_run_has_summaries(entry)
    ]
    runs.sort(key=lambda item: _k6_run_timestamp(item[0]), reverse=True)
    return runs


def _k6_run_times(run_id: str, run_dir: Path) -> tuple[str | None, str | None]:
    """(runStarted, runEnded) ISO strings: start from the dir-name timestamp (local time),
    end from the newest summary file's mtime. Keeps report ranking tied to when the run
    actually happened rather than when the manifest was rebuilt."""
    started = None
    stamp = _k6_run_timestamp(run_id)
    if stamp:
        started = (
            datetime.strptime(stamp, "%Y%m%d-%H%M%S")
            .astimezone()
            .astimezone(timezone.utc)
            .replace(microsecond=0)
            .isoformat()
        )
    mtimes = [
        (run_dir / f"{stem}.summary.json").stat().st_mtime for stem in list_k6_summary_stems(run_dir)
    ]
    ended = None
    if mtimes:
        ended = datetime.fromtimestamp(max(mtimes), tz=timezone.utc).replace(microsecond=0).isoformat()
    return started, ended


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


def parse_suite_name(name: str) -> dict[str, str | None]:
    match = K6_SUMMARY_NAME.match(name)
    if not match:
        endpoint, _, profile = name.partition("_")
        return {"endpoint": endpoint, "profile": profile, "intensity": None, "cacheMode": None}
    return {
        "endpoint": match.group("endpoint"),
        "profile": match.group("profile"),
        "intensity": match.group("intensity"),
        "cacheMode": match.group("cache"),
    }


def split_suite_name(name: str) -> tuple[str, str]:
    parsed = parse_suite_name(name)
    return str(parsed["endpoint"] or ""), str(parsed["profile"] or "")


def find_suite(
    by_name: dict[str, dict[str, Any]],
    endpoint: str,
    profile: str,
    *,
    intensity: str = "med",
    cache_mode: str = "uncached",
) -> dict[str, Any]:
    """Prefer intensity×cache cell, then legacy endpoint_profile name."""
    preferred = f"{endpoint}_{profile}_{intensity}_{cache_mode}"
    if preferred in by_name:
        return by_name[preferred]
    legacy = f"{endpoint}_{profile}"
    if legacy in by_name:
        return by_name[legacy]
    # Fall back to any matching endpoint/profile (first by name).
    for name, scenario in sorted(by_name.items()):
        if scenario.get("endpoint") == endpoint and scenario.get("profile") == profile:
            return scenario
    return {}


def build_scenarios(run_dir: Path) -> list[dict[str, Any]]:
    scenarios: list[dict[str, Any]] = []
    stems = list_k6_summary_stems(run_dir) or [
        name for name in K6_LEGACY_SUITE_NAMES if (run_dir / f"{name}.summary.json").is_file()
    ]
    for name in stems:
        summary_path = run_dir / f"{name}.summary.json"
        if not summary_path.is_file():
            continue
        metrics = read_json(summary_path).get("metrics") or {}
        reqs = metrics.get("http_reqs") or {}
        parsed = parse_suite_name(name)
        scenarios.append(
            {
                "name": name,
                "profile": parsed["profile"],
                "endpoint": parsed["endpoint"],
                "intensity": parsed["intensity"],
                "cacheMode": parsed["cacheMode"],
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

    def suite(endpoint: str, profile: str) -> dict[str, Any]:
        return find_suite(by_name, endpoint, profile)

    def letter(category: str, grade: str, rationale: str) -> dict[str, str]:
        return {"category": category, "grade": grade, "rationale": rationale}

    grades: list[dict[str, str]] = []

    query_scn = [s for s in scenarios if s["endpoint"] == "query"]
    query_status = all((s.get("statusPass") or 0) >= 100 for s in query_scn) if query_scn else False
    query_shape = all((s.get("shapePass") or 0) >= 100 for s in query_scn) if query_scn else False
    grades.append(
        letter(
            "Query functional correctness (status/shape)",
            "A" if query_status and query_shape else "B",
            "100% status and shape checks across the full rerun"
            if query_status and query_shape
            else "One or more suites missed perfect status/shape checks",
        )
    )

    load = suite("query", "load")
    load_p95 = p95(load)
    load_checks = load.get("checksPass") or 0
    grades.append(
        letter(
            "Query load",
            "A" if load_p95 is not None and load_p95 < 200 and load_checks >= 100 else "B",
            f"{load_p95:.0f} ms p95 with {load_checks:.2f}% checks" if load_p95 is not None else "Missing load suite",
        )
    )

    stress = suite("query", "stress")
    stress_p95 = p95(stress)
    stress_checks = stress.get("checksPass") or 0
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

    spike = suite("query", "spike")
    spike_p95 = p95(spike)
    spike_checks = spike.get("checksPass") or 0
    spike_dropped = spike.get("droppedIterations") or 0
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

    soak = suite("query", "soak")
    soak_p95 = p95(soak)
    soak_checks = soak.get("checksPass") or 0
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

    qp_load = p95(suite("queryproject", "load"))
    qp_stress = p95(suite("queryproject", "stress"))
    qp_spike = p95(suite("queryproject", "spike"))
    qp_soak = p95(suite("queryproject", "soak"))
    qp_checks = min(
        suite("queryproject", "load").get("checksPass") or 0,
        suite("queryproject", "stress").get("checksPass") or 100,
        suite("queryproject", "spike").get("checksPass") or 100,
        suite("queryproject", "soak").get("checksPass") or 100,
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

    qr_load = p95(suite("queryroot", "load"))
    qr_stress = p95(suite("queryroot", "stress"))
    qr_spike = p95(suite("queryroot", "spike"))
    qr_soak = p95(suite("queryroot", "soak"))
    qr_checks = min(
        suite("queryroot", "load").get("checksPass") or 0,
        suite("queryroot", "stress").get("checksPass") or 100,
        suite("queryroot", "spike").get("checksPass") or 100,
        suite("queryroot", "soak").get("checksPass") or 100,
    )
    if all(v is not None for v in (qr_load, qr_stress, qr_spike, qr_soak)):
        if max(qr_load, qr_spike, qr_soak) <= 400 and qr_stress <= 1000 and qr_checks >= 99.5:
            qr_grade = "A"
        elif max(qr_load, qr_spike, qr_soak) <= 700 and qr_stress <= 2000:
            qr_grade = "A-"
        elif qr_stress <= 3000:
            qr_grade = "B"
        else:
            qr_grade = "C"
        qr_rationale = (
            f"load/spike/soak p95 {qr_load:.0f}/{qr_spike:.0f}/{qr_soak:.0f} ms; "
            f"stress p95 {qr_stress:.0f} ms; {qr_checks:.2f}% checks"
        )
    else:
        qr_grade = "C"
        qr_rationale = "Incomplete QueryRoot suite coverage"
    grades.append(letter("QueryRoot path", qr_grade, qr_rationale))

    return grades


def slo_assessment(scenarios: list[dict[str, Any]]) -> list[dict[str, str]]:
    by_name = {s["name"]: s for s in scenarios}
    rows: list[dict[str, str]] = []

    def add(area: str, target: str, latest: str, result: str) -> None:
        rows.append({"area": area, "target": target, "latest": latest, "result": result})

    def lat(endpoint: str, profile: str) -> float | None:
        return p95(find_suite(by_name, endpoint, profile))

    qp_load, qp_spike, qp_soak = lat("queryproject", "load"), lat("queryproject", "spike"), lat("queryproject", "soak")
    if all(v is not None for v in (qp_load, qp_spike, qp_soak)):
        latest = f"{min(qp_load, qp_spike, qp_soak):.0f}-{max(qp_load, qp_spike, qp_soak):.0f} ms"
        worst = max(qp_load, qp_spike, qp_soak)
        add("QueryProject load/spike/soak", "100-300 ms", latest, "Exceeds target" if worst <= 300 else "Meets" if worst <= 700 else "Miss")

    qp_stress = lat("queryproject", "stress")
    if qp_stress is not None:
        add("QueryProject stress", "300-700 ms", f"{qp_stress:.0f} ms", "Meets" if qp_stress <= 700 else "Slightly above" if qp_stress <= 1500 else "Miss")

    q_load = lat("query", "load")
    if q_load is not None:
        add("Query load", "300-700 ms", f"{q_load:.0f} ms", "Exceeds target" if q_load <= 300 else "Meets")

    q_stress = lat("query", "stress")
    if q_stress is not None:
        add("Query stress", "500-1,000 ms", f"{q_stress:.0f} ms", "Meets" if q_stress <= 1000 else "Slightly above" if q_stress <= 2000 else "Miss")

    q_spike = lat("query", "spike")
    if q_spike is not None:
        add("Query spike", "700-1,500 ms", f"{q_spike:.0f} ms", "Exceeds target" if q_spike <= 700 else "Meets" if q_spike <= 1500 else "Miss")

    q_soak = lat("query", "soak")
    if q_soak is not None:
        add("Query soak", "500-1,000 ms", f"{q_soak:.0f} ms", "Exceeds target" if q_soak <= 500 else "Meets" if q_soak <= 1000 else "Miss")

    qr_load, qr_spike, qr_soak = lat("queryroot", "load"), lat("queryroot", "spike"), lat("queryroot", "soak")
    if all(v is not None for v in (qr_load, qr_spike, qr_soak)):
        latest = f"{min(qr_load, qr_spike, qr_soak):.0f}-{max(qr_load, qr_spike, qr_soak):.0f} ms"
        worst = max(qr_load, qr_spike, qr_soak)
        add("QueryRoot load/spike/soak", "100-500 ms", latest, "Exceeds target" if worst <= 300 else "Meets" if worst <= 700 else "Miss")

    qr_stress = lat("queryroot", "stress")
    if qr_stress is not None:
        add("QueryRoot stress", "300-1,000 ms", f"{qr_stress:.0f} ms", "Meets" if qr_stress <= 1000 else "Slightly above" if qr_stress <= 2000 else "Miss")

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

    run_started, run_ended = _k6_run_times(run_id, run_dir)
    report = {
        "type": "load",
        "schema": SCHEMA,
        "name": K6_REPORT_NAME,
        "title": K6_REPORT_TITLE,
        "description": K6_DESCRIPTION,
        "runId": run_id,
        "runStarted": run_started,
        "runEnded": run_ended,
        "generatedAt": utc_now_iso(),
        "environment": {"tool": "k6"},
        "notes": [
            "Randomized multi-key sorting is disabled except for the filter_sort case "
            "(fixed LastName, FirstName, Id); other cases use server default (PK) order.",
            f"Source: {run_dir.relative_to(REPO_ROOT)}",
        ],
        "cases": build_cases(scenarios),
        "scenarios": scenarios,
        "rollups": [
            endpoint_rollup(scenarios, "query"),
            endpoint_rollup(scenarios, "queryproject"),
            endpoint_rollup(scenarios, "queryroot"),
        ],
        "slo": slo_assessment(scenarios),
        "grades": grade_k6(scenarios),
    }
    publish_report(report)
    return True


def republish_from_history() -> None:
    """Re-pick each suite's latest from data + history using current ranking rules."""
    names: set[str] = set()
    if DATA_DIR.is_dir():
        names.update(p.stem for p in DATA_DIR.glob("*.json"))
    history_root = REPO_ROOT / "docs" / "benchmarks" / "history"
    if history_root.is_dir():
        names.update(p.name for p in history_root.iterdir() if p.is_dir())

    for name in sorted(names):
        candidates = _collect_report_candidates(name)
        if not candidates:
            continue
        best = max(candidates, key=_report_rank)
        print(f"{name}: selecting {best.get('runId')} (richness={_report_richness(best)})")
        publish_report(best)


def main() -> None:
    parser = argparse.ArgumentParser(description="Aggregate benchmark reports into the unified schema.")
    parser.add_argument("--k6-only", action="store_true")
    parser.add_argument("--k6-run-dir", type=Path, default=None, help="Explicit k6 results directory")
    parser.add_argument(
        "--republish-history",
        action="store_true",
        help="Re-select latest per suite from archived history (no new artifact ingest).",
    )
    parser.add_argument(
        "--sync-portfolio-only",
        action="store_true",
        help="Only copy docs/benchmarks/history → apps/gateway/public/benchmarks/history when that tree exists.",
    )
    parser.add_argument(
        "--no-sync-portfolio",
        action="store_true",
        help="Skip copying history into the Gateway public tree (use in Docker; history is already mounted).",
    )
    parser.add_argument(
        "--publish-s3",
        action="store_true",
        help="After writing manifests, aws s3 sync data/ + history/ JSON (LYO_BENCH_S3_BUCKET or --bucket).",
    )
    parser.add_argument(
        "--bucket",
        default=os.environ.get("LYO_BENCH_S3_BUCKET", ""),
        help="S3 bucket for --publish-s3 (or LYO_BENCH_S3_BUCKET).",
    )
    for category in BDN_CATEGORIES:
        parser.add_argument(f"--{category['name']}-only", action="store_true")
    args = parser.parse_args()

    if args.sync_portfolio_only:
        sync_portfolio_history()
        if args.publish_s3:
            from publish_s3 import publish as publish_s3

            publish_s3(args.bucket)
        return

    if args.republish_history:
        republish_from_history()
        write_registry()
        if not args.no_sync_portfolio:
            sync_portfolio_history()
        if args.publish_s3:
            from publish_s3 import publish as publish_s3

            publish_s3(args.bucket)
        return

    category_flags = {c["name"]: getattr(args, f"{c['name']}_only") for c in BDN_CATEGORIES}
    run_all = not (args.k6_only or any(category_flags.values()))

    if run_all or args.k6_only:
        build_k6(args.k6_run_dir.resolve() if args.k6_run_dir else None)

    for category in BDN_CATEGORIES:
        if run_all or category_flags[category["name"]]:
            build_micro_category(category)

    write_registry()
    if not args.no_sync_portfolio:
        sync_portfolio_history()
    if args.publish_s3:
        from publish_s3 import publish as publish_s3

        publish_s3(args.bucket)


if __name__ == "__main__":
    main()
