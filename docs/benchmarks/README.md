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
  listing every report. Each export also archives a timestamped snapshot under
  `history/<name>/`, computes Δ columns vs the immediately prior snapshot, and embeds a
  run-history summary in the report JSON for the viewer.

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
scripts/benchmarks/run-dotnet-benchmarks.sh                 # all suites — exports each category as it finishes
scripts/benchmarks/run-dotnet-benchmarks.sh --no-docker hashing csv

# Or just rebuild dashboard data from existing artifacts / k6 results
python3 scripts/benchmarks/build-manifests.py               # micro + k6
python3 scripts/benchmarks/build-manifests.py --k6-only
python3 scripts/benchmarks/build-manifests.py --hashing-only
```

Each successful export appends to `history/<name>/` (unless that `runId` was already
archived). Re-open a report to use the **Snapshot** dropdown (older runs load from
`history/<name>/*.js`). **Δ** columns compare each snapshot to the **immediately prior
archived run** (green = better; red = worse). The first archived run has no Δ columns.

### Troubleshooting stale runs

`build-manifests.py` searches the suite's project `BenchmarkDotNet.Artifacts/` plus fallback
`BenchmarkDotNet.Artifacts/` at the repo root and under `Lyo.Net/`. It prefers **joined**
runs (`runId` contains `joined`) over ad-hoc filtered runs, then the newest timestamp.

If the dashboard still shows a June run after you benchmarked in July:

1. **Use the run script** — it passes `--join` and `--artifacts` so the exporter writes
   `<name>.lyobench.json` next to the project:
   `scripts/benchmarks/run-dotnet-benchmarks.sh csv`
2. **Avoid bare `dotnet run` from the repo root** — without `--artifacts`, BenchmarkDotNet
   writes to `./BenchmarkDotNet.Artifacts/` and without `--join` you only get the last
   benchmark class, not the full suite.
3. **Regenerate** — `python3 scripts/benchmarks/build-manifests.py --csv-only` (or `--<name>-only`
   for the suite you ran).
4. Check the manifest output for `using …` / `synced …` lines to see which artifact file
   was picked.

k6 matrix runs also refresh automatically at the end of
[`k6/framework-person/run_all.sh`](../../k6/framework-person/run_all.sh).

## View

Open [`index.html`](index.html) after regenerating, then click into any report. Files work
from `file://` (data is loaded via classic `<script>` globals, not `fetch`).
