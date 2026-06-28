# Benchmark Dashboards

Pretty HTML reviews for Lyo benchmarks. Every report — BenchmarkDotNet micro-benchmarks
and k6 load tests — is normalized to one **unified schema** (`lyo.bench/v1`) and rendered by
a single viewer. Data loads from auto-generated files in [`data/`](data/); no manual table
editing after a run.

## How it works

- **Micro (BenchmarkDotNet):** each suite's in-process exporter
  (`LyoBenchmarkExporter` in [`Lyo.Benchmarking`](../../Lyo.Net/Core/Benchmark/Lyo.Benchmarking/README.md))
  writes `<name>.lyobench.json` (`type: "micro"`) into its `BenchmarkDotNet.Artifacts`.
- **Load (k6):** k6 cannot emit the schema, so `build-manifests.py` normalizes the raw
  `*.summary.json` files into a `LoadTestReport` (`type: "load"`).
- `build-manifests.py` then writes, per report, `data/<name>.json` (portable) and
  `data/<name>.js` (sets `window.LyoBench.reports["<name>"]`), plus `data/registry.js`
  listing every report.

Reports are self-describing: each carries a suite `description`, per-class/per-method descriptions,
parameter legends (units + meaning, e.g. `DataSize` in bytes), an auto-derived `dataset` (columns,
types, nesting depth — so a CSV/XLSX row type or a mapping entity's nested children are explicit), and
for k6 a `cases` list describing each query's structure (where clauses, sort, includes, selection field
count). The viewer renders all of this, so a row like `Hash @ 1 MB` reads in full context.

Reports also carry an **SLA / business-standard** assessment. Micro benchmarks declare budgets with
`[BenchmarkSla]` (latency / throughput / allocation + a `Standard` citation); the exporter grades each
measurement and comparison row `Meets` / `Exceeds` / `Miss` and rolls a worst-case row per benchmark into
the report's `slo` list. The viewer renders an SLA verdict-badge column on the measurement and comparison
tables (with a legend and the cited standards), a "baseline" tag on the reference method, and an "SLA
assessment" summary section — the same treatment k6 load reports get for their SLOs.

The models + schema contract live in
[`Lyo.Benchmark.Models`](../../Lyo.Net/Core/Benchmark/Lyo.Benchmark.Models/README.md). For micro
reports the context comes from attributes (`[BenchmarkReport(Description=…)]`, `[BenchmarkDescription]`,
`[BenchmarkParameter]`, `[BenchmarkDataShape]`); for k6 it comes from the `K6_CASE_META` map in
`build-manifests.py` (mirroring `k6/framework-person/lib/cases.js`).

## Pages

| Page | Role |
|------|------|
| [index.html](index.html) | Card hub, built from `data/registry.js`. |
| [report.html](report.html) | Single viewer; `report.html#<name>` dispatches on `type` to a micro or load layout. |

## Generate / refresh

```bash
# Run the BenchmarkDotNet suites (Release) and rebuild all dashboard data
scripts/benchmarks/run-dotnet-benchmarks.sh                 # all suites
scripts/benchmarks/run-dotnet-benchmarks.sh --no-docker hashing csv

# Or just rebuild dashboard data from existing artifacts / k6 results
python3 scripts/benchmarks/build-manifests.py               # micro + k6
python3 scripts/benchmarks/build-manifests.py --k6-only
python3 scripts/benchmarks/build-manifests.py --hashing-only
```

k6 matrix runs also refresh automatically at the end of
[`k6/framework-person/run_all.sh`](../../k6/framework-person/run_all.sh).

## View

Open [`index.html`](index.html) after regenerating, then click into any report. Files work
from `file://` (data is loaded via classic `<script>` globals, not `fetch`).
