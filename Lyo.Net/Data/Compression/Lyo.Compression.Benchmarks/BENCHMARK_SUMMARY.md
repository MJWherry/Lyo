# Compression Benchmarks Summary

The interactive review lives in the HTML dashboard (auto-loaded from latest BenchmarkDotNet CSV artifacts):

- **[Compression benchmark dashboard](../../../docs/benchmarks/compression.html)**
- Hub: [`docs/benchmarks/index.html`](../../../docs/benchmarks/index.html)

## Refresh after a benchmark run

```bash
dotnet run -c Release --project Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks
python3 scripts/benchmarks/build-manifests.py --compression-only
```

Generated data: `docs/benchmarks/data/compression-latest.json`.

Raw CSV reports: `BenchmarkDotNet.Artifacts/results/` in this project.

## Running benchmarks

See [README.md](./README.md).
