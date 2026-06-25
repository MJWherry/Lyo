# Benchmark Dashboards

Pretty HTML reviews for Lyo single-instance benchmarks. Data is loaded from auto-generated JSON in [`data/`](data/) — no manual table editing after each run.

## Pages

| Page | Source artifacts |
|------|-------------------|
| [index.html](index.html) | Hub linking all three reviews |
| [k6.html](k6.html) | `k6/framework-person/results/*/ *.summary.json` |
| [encryption.html](encryption.html) | `Lyo.Encryption.Benchmarks/BenchmarkDotNet.Artifacts/results/*.csv` |
| [compression.html](compression.html) | `Lyo.Compression.Benchmarks/BenchmarkDotNet.Artifacts/results/*.csv` |

## Refresh manifests

```bash
# All three
python3 scripts/benchmarks/build-manifests.py

# Or individually
python3 scripts/benchmarks/build-manifests.py --k6-only
python3 scripts/benchmarks/build-manifests.py --encryption-only
python3 scripts/benchmarks/build-manifests.py --compression-only
```

k6 matrix runs also refresh automatically at the end of [`k6/framework-person/run_all.sh`](../k6/framework-person/run_all.sh).

## View

Open the HTML files directly after regenerating manifests:

- [`index.html`](index.html)
- [`k6.html`](k6.html)
- [`encryption.html`](encryption.html)
- [`compression.html`](compression.html)

## Previous-run comparison

When a prior `*-latest.json` exists, the builder copies it to `*-previous.json` before overwriting. The k6 page shows p95/throughput/check deltas when a previous k6 run is available.

## BenchmarkDotNet JSON export (optional)

v1 reads existing CSV reports. Future runs may add `--exporters json`, but CSV parsing is sufficient today.
