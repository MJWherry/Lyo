#!/usr/bin/env python3
"""Consolidate k6 summary JSON and BenchmarkDotNet CSV artifacts into docs/benchmarks/data/*.json."""

from __future__ import annotations

import argparse
import csv
import json
import re
import shutil
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"

K6_RESULTS = REPO_ROOT / "k6" / "framework-person" / "results"
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

ENCRYPTION_RESULTS = (
    REPO_ROOT
    / "Lyo.Net"
    / "Security"
    / "Encryption"
    / "Lyo.Encryption.Benchmarks"
    / "BenchmarkDotNet.Artifacts"
)
COMPRESSION_RESULTS = (
    REPO_ROOT
    / "Lyo.Net"
    / "Data"
    / "Compression"
    / "Lyo.Compression.Benchmarks"
    / "BenchmarkDotNet.Artifacts"
)

K6_RUN_PREFIXES = ("prod-like-", "prod-matrix-")


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def read_json(path: Path) -> Any:
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)
        f.write("\n")


def write_data_outputs(stem: str, global_name: str, payload: Any) -> None:
    """Write both JSON and a classic JS global for file://-compatible HTML pages."""
    json_path = DATA_DIR / f"{stem}-latest.json"
    js_path = DATA_DIR / f"{stem}-latest.js"
    rotate_latest(json_path)
    rotate_latest(js_path)
    write_json(json_path, payload)
    js_path.parent.mkdir(parents=True, exist_ok=True)
    with js_path.open("w", encoding="utf-8") as f:
        f.write(f"window.{global_name} = ")
        json.dump(payload, f)
        f.write(";\n")


def rotate_latest(output_path: Path) -> None:
    previous_path = output_path.with_name(output_path.name.replace("-latest.", "-previous."))
    if output_path.exists():
        shutil.copy2(output_path, previous_path)


def discover_k6_runs() -> list[tuple[str, Path]]:
    if not K6_RESULTS.is_dir():
        return []
    runs: list[tuple[str, Path]] = []
    for entry in K6_RESULTS.iterdir():
        if not entry.is_dir():
            continue
        if any(entry.name.startswith(prefix) for prefix in K6_RUN_PREFIXES):
            runs.append((entry.name, entry))
    runs.sort(key=lambda item: item[0], reverse=True)
    return runs


def metric_block(metrics: dict[str, Any], key: str) -> dict[str, float | int | None]:
    block = metrics.get(key) or {}
    return {
        "avg": block.get("avg"),
        "p95": block.get("p(95)"),
        "p99": block.get("p(99)"),
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
    block = metrics.get("dropped_iterations") or {}
    return int(block.get("count") or 0)


def query_case_hotspots(metrics: dict[str, Any]) -> list[dict[str, Any]]:
    cases: list[tuple[float, str, dict[str, float | None]]] = []
    for key, value in metrics.items():
        if not key.startswith("query_duration{query_case:"):
            continue
        case = key.split("query_case:", 1)[1].rstrip("}")
        p95 = value.get("p(95)")
        if p95 is None:
            continue
        cases.append((float(p95), case, metric_block(metrics, key)))
    cases.sort(reverse=True)
    return [{"case": case, **stats} for _, case, stats in cases]


def endpoint_rollup(suites: list[dict[str, Any]], prefix: str) -> dict[str, Any]:
    filtered = [s for s in suites if s["name"].startswith(prefix)]
    total_requests = sum(int(s["requests"]) for s in filtered)
    if total_requests == 0:
        return {"totalRequests": 0}

    def weighted_rate(key: str) -> float | None:
        num = 0.0
        for suite in filtered:
            rate = suite.get(key)
            if rate is None:
                continue
            num += float(rate) * int(suite["requests"])
        return num / total_requests if total_requests else None

    return {
        "totalRequests": total_requests,
        "checksPass": weighted_rate("checksPass"),
        "statusPass": weighted_rate("statusPass"),
        "shapePass": weighted_rate("shapePass"),
        "latencyPass": weighted_rate("latencyPass"),
    }


def grade_k6(suites: list[dict[str, Any]]) -> list[dict[str, str]]:
    by_name = {s["name"]: s for s in suites}

    def suite(name: str) -> dict[str, Any]:
        return by_name.get(name, {})

    def letter(category: str, grade: str, rationale: str) -> dict[str, str]:
        return {"category": category, "grade": grade, "rationale": rationale}

    grades: list[dict[str, str]] = []

    query_status = all((s.get("statusPass") or 0) >= 100 for s in suites if s["name"].startswith("query_"))
    query_shape = all((s.get("shapePass") or 0) >= 100 for s in suites if s["name"].startswith("query_"))
    grades.append(
        letter(
            "Query functional correctness (status/shape)",
            "A" if query_status and query_shape else "B",
            "100% status and shape checks across the full rerun"
            if query_status and query_shape
            else "One or more suites missed perfect status/shape checks",
        )
    )

    load_p95 = suite("query_load").get("p95")
    load_checks = suite("query_load").get("checksPass") or 0
    grades.append(
        letter(
            "Query load",
            "A" if load_p95 is not None and load_p95 < 200 and load_checks >= 100 else "B",
            f"{load_p95:.0f} ms p95 with {load_checks:.2f}% checks" if load_p95 is not None else "Missing load suite",
        )
    )

    stress_p95 = suite("query_stress").get("p95")
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
            f"p95 ~{stress_p95 / 1000:.2f}s, {stress_checks:.2f}% checks"
            if stress_p95 is not None
            else "Missing stress suite",
        )
    )

    spike_p95 = suite("query_spike").get("p95")
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

    soak_p95 = suite("query_soak").get("p95")
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
            f"sub-{soak_p95:.0f} ms sustained p95, {soak_checks:.2f}% checks"
            if soak_p95 is not None
            else "Missing soak suite",
        )
    )

    qp_load = suite("queryproject_load").get("p95")
    qp_stress = suite("queryproject_stress").get("p95")
    qp_spike = suite("queryproject_spike").get("p95")
    qp_soak = suite("queryproject_soak").get("p95")
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


def slo_assessment(suites: list[dict[str, Any]]) -> list[dict[str, str]]:
    by_name = {s["name"]: s for s in suites}
    rows: list[dict[str, str]] = []

    def add(area: str, target: str, latest: str, result: str) -> None:
        rows.append({"area": area, "target": target, "latest": latest, "result": result})

    qp_load = by_name.get("queryproject_load", {}).get("p95")
    qp_spike = by_name.get("queryproject_spike", {}).get("p95")
    qp_soak = by_name.get("queryproject_soak", {}).get("p95")
    if all(v is not None for v in (qp_load, qp_spike, qp_soak)):
        latest = f"{min(qp_load, qp_spike, qp_soak):.0f}–{max(qp_load, qp_spike, qp_soak):.0f} ms"
        add("QueryProject load/spike/soak", "100–300 ms", latest, "Exceeds target" if max(qp_load, qp_spike, qp_soak) <= 300 else "Meets" if max(qp_load, qp_spike, qp_soak) <= 700 else "Miss")

    qp_stress = by_name.get("queryproject_stress", {}).get("p95")
    if qp_stress is not None:
        add(
            "QueryProject stress",
            "300–700 ms",
            f"{qp_stress:.0f} ms",
            "Meets" if qp_stress <= 700 else "Slightly above" if qp_stress <= 1500 else "Miss",
        )

    q_load = by_name.get("query_load", {}).get("p95")
    if q_load is not None:
        add("Query load", "300–700 ms", f"{q_load:.0f} ms", "Exceeds target" if q_load <= 300 else "Meets")

    q_stress = by_name.get("query_stress", {}).get("p95")
    if q_stress is not None:
        add(
            "Query stress",
            "500–1,000 ms",
            f"{q_stress:.0f} ms",
            "Meets" if q_stress <= 1000 else "Slightly above" if q_stress <= 2000 else "Miss",
        )

    q_spike = by_name.get("query_spike", {}).get("p95")
    if q_spike is not None:
        add(
            "Query spike",
            "700–1,500 ms",
            f"{q_spike:.0f} ms",
            "Exceeds target" if q_spike <= 700 else "Meets" if q_spike <= 1500 else "Miss",
        )

    q_soak = by_name.get("query_soak", {}).get("p95")
    if q_soak is not None:
        add(
            "Query soak",
            "500–1,000 ms",
            f"{q_soak:.0f} ms",
            "Exceeds target" if q_soak <= 500 else "Meets" if q_soak <= 1000 else "Miss",
        )

    status_shape = all((s.get("statusPass") or 0) >= 100 and (s.get("shapePass") or 0) >= 100 for s in suites)
    add("Status + response-shape correctness", "99.9–100%", "100%", "Meets" if status_shape else "Miss")

    return rows


def build_k6_manifest(run_dir: Path, run_id: str, previous_payload: dict[str, Any] | None = None) -> dict[str, Any]:
    suites: list[dict[str, Any]] = []
    for name in K6_SUITE_NAMES:
        summary_path = run_dir / f"{name}.summary.json"
        if not summary_path.is_file():
            continue
        metrics = read_json(summary_path).get("metrics") or {}
        duration = metric_block(metrics, "http_req_duration")
        reqs = metrics.get("http_reqs") or {}
        suites.append(
            {
                "name": name,
                **duration,
                "throughput": reqs.get("rate"),
                "requests": reqs.get("count"),
                "checksPass": check_pass_rate(metrics),
                "statusPass": (metrics.get("status_success_rate") or {}).get("value", 0) * 100,
                "shapePass": (metrics.get("shape_success_rate") or {}).get("value", 0) * 100,
                "latencyPass": (metrics.get("latency_success_rate") or {}).get("value", 0) * 100,
                "droppedIterations": dropped_iterations(metrics),
                "hotspots": query_case_hotspots(metrics),
            }
        )

    payload: dict[str, Any] = {
        "type": "k6",
        "runId": run_id,
        "generatedAt": utc_now_iso(),
        "sourceDir": str(run_dir.relative_to(REPO_ROOT)),
        "workloadNote": (
            "Randomized multi-key sorting is disabled except for the filter_sort case "
            "(fixed LastName, FirstName, Id); other cases use server default (PK) order."
        ),
        "suites": suites,
        "rollup": {
            "query": endpoint_rollup(suites, "query_"),
            "queryproject": endpoint_rollup(suites, "queryproject_"),
        },
        "grades": grade_k6(suites),
        "sloAssessment": slo_assessment(suites),
    }

    if previous_payload and previous_payload.get("suites"):
        prev_by_name = {s["name"]: s for s in previous_payload["suites"]}
        deltas = []
        for suite in suites:
            prev = prev_by_name.get(suite["name"])
            if not prev:
                continue
            deltas.append(
                {
                    "name": suite["name"],
                    "p95Delta": (suite.get("p95") or 0) - (prev.get("p95") or 0),
                    "throughputDelta": (suite.get("throughput") or 0) - (prev.get("throughput") or 0),
                    "checksPassDelta": (suite.get("checksPass") or 0) - (prev.get("checksPass") or 0),
                    "droppedIterationsDelta": (suite.get("droppedIterations") or 0) - (prev.get("droppedIterations") or 0),
                }
            )
        payload["previous"] = {
            "runId": previous_payload.get("runId"),
            "generatedAt": previous_payload.get("generatedAt"),
            "deltas": deltas,
        }

    return payload


def parse_duration_to_ns(value: str | None) -> float | None:
    if not value or value.strip().upper() == "NA":
        return None
    text = value.strip().replace(",", "")
    match = re.match(r"^([\d.]+)\s*(ns|μs|us|ms|s|m)$", text, re.IGNORECASE)
    if not match:
        return None
    amount = float(match.group(1))
    unit = match.group(2).lower()
    multipliers = {"ns": 1, "μs": 1_000, "us": 1_000, "ms": 1_000_000, "s": 1_000_000_000, "m": 60_000_000_000}
    return amount * multipliers[unit]


def format_data_size(size: int | str | None) -> str:
    if size is None or str(size).upper() == "NA":
        return "NA"
    try:
        n = int(size)
    except (TypeError, ValueError):
        return str(size)
    if n >= 1_048_576:
        return f"{n // 1_048_576} MB" if n % 1_048_576 == 0 else f"{n / 1_048_576:.1f} MB"
    if n >= 1024:
        return f"{n // 1024} KB" if n % 1024 == 0 else f"{n / 1024:.1f} KB"
    return f"{n} B"


def parse_bdn_csv(path: Path) -> tuple[str, list[dict[str, Any]]]:
    class_name = path.name.split(".Benchmarks.", 1)[-1].removesuffix("-report.csv")
    rows: list[dict[str, Any]] = []
    with path.open(newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            mean = row.get("Mean")
            if not mean or mean.strip().upper() == "NA":
                continue
            data_size_raw = row.get("DataSize")
            data_size = None
            if data_size_raw and data_size_raw.upper() != "NA":
                try:
                    data_size = int(float(data_size_raw))
                except ValueError:
                    data_size = data_size_raw
            ratio_raw = row.get("Ratio")
            ratio = None
            if ratio_raw and ratio_raw not in {"?", "NA"}:
                try:
                    ratio = float(ratio_raw)
                except ValueError:
                    ratio = None
            rows.append(
                {
                    "method": row.get("Method"),
                    "dataSize": data_size,
                    "dataSizeLabel": format_data_size(data_size),
                    "mean": mean.strip(),
                    "meanNs": parse_duration_to_ns(mean),
                    "ratio": ratio,
                    "allocated": row.get("Allocated"),
                }
            )
    return class_name, rows


def discover_bdn_run(artifacts_dir: Path) -> tuple[str | None, Path | None]:
    logs = sorted(artifacts_dir.glob("BenchmarkRun-*.log"), reverse=True)
    if not logs:
        return None, None
    latest_log = logs[0]
    run_id = latest_log.stem.removeprefix("BenchmarkRun-")
    return run_id, latest_log


def parse_bdn_environment(log_path: Path | None) -> dict[str, str | None]:
    if not log_path or not log_path.is_file():
        return {}
    text = log_path.read_text(encoding="utf-8", errors="replace")
    env: dict[str, str | None] = {}
    patterns = {
        "benchmarkDotNet": r"BenchmarkDotNet v([\d.]+)",
        "runtime": r"Runtime=(.+)",
        "cpu": r"Processor=(.+)",
        "os": r"OS=(.+)",
    }
    for key, pattern in patterns.items():
        match = re.search(pattern, text)
        env[key] = match.group(1).strip() if match else None
    return env


def build_bdn_manifest(
    artifacts_dir: Path,
    benchmark_type: str,
    baseline_method_prefix: str,
    previous_payload: dict[str, Any] | None = None,
) -> dict[str, Any]:
    run_id, log_path = discover_bdn_run(artifacts_dir)
    results_dir = artifacts_dir / "results"
    classes: dict[str, list[dict[str, Any]]] = {}
    for csv_path in sorted(results_dir.glob("*-report.csv")):
        class_name, rows = parse_bdn_csv(csv_path)
        classes[class_name] = rows

    comparison = classes.get("AlgorithmComparisonBenchmarks", [])
    comparison_tables = build_algorithm_comparison_tables(comparison, baseline_method_prefix)

    payload: dict[str, Any] = {
        "type": benchmark_type,
        "runId": run_id,
        "generatedAt": utc_now_iso(),
        "sourceDir": str(artifacts_dir.relative_to(REPO_ROOT)),
        "environment": parse_bdn_environment(log_path),
        "classes": classes,
        "comparisonTables": comparison_tables,
    }

    if previous_payload and previous_payload.get("classes"):
        payload["previous"] = {"runId": previous_payload.get("runId"), "generatedAt": previous_payload.get("generatedAt")}

    return payload


def build_algorithm_comparison_tables(rows: list[dict[str, Any]], baseline_prefix: str) -> dict[str, Any]:
    sizes = sorted({r["dataSize"] for r in rows if isinstance(r.get("dataSize"), int)})
    size_labels = {size: format_data_size(size) for size in sizes}

    def table_for(operation_suffix: str) -> list[dict[str, Any]]:
        filtered = [r for r in rows if str(r.get("method", "")).endswith(operation_suffix)]
        by_size_algo: dict[tuple[int, str], dict[str, Any]] = {}
        for row in filtered:
            method = str(row["method"])
            algo = method[: -len(operation_suffix)].rstrip("_")
            size = row.get("dataSize")
            if not isinstance(size, int):
                continue
            by_size_algo[(size, algo)] = row

        table_rows: list[dict[str, Any]] = []
        for size in sizes:
            baseline = by_size_algo.get((size, baseline_prefix))
            baseline_ns = baseline.get("meanNs") if baseline else None
            for (row_size, algo), row in sorted(by_size_algo.items(), key=lambda item: (item[0][0], item[0][1])):
                if row_size != size:
                    continue
                ratio_vs_baseline = None
                if baseline_ns and row.get("meanNs"):
                    ratio_vs_baseline = row["meanNs"] / baseline_ns
                table_rows.append(
                    {
                        "size": size,
                        "sizeLabel": size_labels[size],
                        "algorithm": algo,
                        "mean": row.get("mean"),
                        "meanNs": row.get("meanNs"),
                        "ratio": row.get("ratio"),
                        "ratioVsBaseline": ratio_vs_baseline,
                        "allocated": row.get("allocated"),
                    }
                )
        return table_rows

    encrypt_suffix = "_Compress" if baseline_prefix == "GZip" else "_Encrypt"
    decrypt_suffix = "_Decompress" if baseline_prefix == "GZip" else "_Decrypt"
    return {
        "encryptOrCompress": table_for(encrypt_suffix),
        "decryptOrDecompress": table_for(decrypt_suffix),
        "baseline": baseline_prefix,
    }


def build_k6(previous_latest: dict[str, Any] | None, run_dir: Path | None) -> None:
    if run_dir is None:
        runs = discover_k6_runs()
        if not runs:
            print("No k6 runs found.")
            return
        run_id, run_dir = runs[0]
        previous_run = runs[1][1] if len(runs) > 1 else None
        previous_payload = build_k6_manifest(previous_run, runs[1][0]) if previous_run else previous_latest
    else:
        run_id = run_dir.name
        runs = discover_k6_runs()
        previous_payload = previous_latest
        for idx, (candidate_id, candidate_dir) in enumerate(runs):
            if candidate_dir == run_dir and idx + 1 < len(runs):
                previous_payload = build_k6_manifest(runs[idx + 1][1], runs[idx + 1][0])
                break

    output = DATA_DIR / "k6-latest.json"
    payload = build_k6_manifest(run_dir, run_id, previous_payload)
    write_data_outputs("k6", "__BENCHMARK_K6__", payload)
    print(f"Wrote {output.relative_to(REPO_ROOT)}")


def build_encryption(previous_latest: dict[str, Any] | None) -> None:
    if not ENCRYPTION_RESULTS.is_dir():
        print("Encryption artifacts directory not found.")
        return
    payload = build_bdn_manifest(ENCRYPTION_RESULTS, "encryption", "AesGcm", previous_latest)
    write_data_outputs("encryption", "__BENCHMARK_ENCRYPTION__", payload)
    print(f"Wrote {(DATA_DIR / 'encryption-latest.json').relative_to(REPO_ROOT)}")


def build_compression(previous_latest: dict[str, Any] | None) -> None:
    if not COMPRESSION_RESULTS.is_dir():
        print("Compression artifacts directory not found.")
        return
    payload = build_bdn_manifest(COMPRESSION_RESULTS, "compression", "GZip", previous_latest)
    write_data_outputs("compression", "__BENCHMARK_COMPRESSION__", payload)
    print(f"Wrote {(DATA_DIR / 'compression-latest.json').relative_to(REPO_ROOT)}")


def load_previous(output_name: str) -> dict[str, Any] | None:
    path = DATA_DIR / output_name
    return read_json(path) if path.is_file() else None


def main() -> None:
    parser = argparse.ArgumentParser(description="Build benchmark dashboard JSON manifests.")
    parser.add_argument("--k6-only", action="store_true")
    parser.add_argument("--encryption-only", action="store_true")
    parser.add_argument("--compression-only", action="store_true")
    parser.add_argument("--k6-run-dir", type=Path, default=None, help="Explicit k6 results directory")
    args = parser.parse_args()

    run_all = not (args.k6_only or args.encryption_only or args.compression_only)

    if run_all or args.k6_only:
        build_k6(load_previous("k6-latest.json"), args.k6_run_dir.resolve() if args.k6_run_dir else None)
    if run_all or args.encryption_only:
        build_encryption(load_previous("encryption-latest.json"))
    if run_all or args.compression_only:
        build_compression(load_previous("compression-latest.json"))


if __name__ == "__main__":
    main()
