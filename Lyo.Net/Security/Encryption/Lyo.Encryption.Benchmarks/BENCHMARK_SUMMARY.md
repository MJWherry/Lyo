# Encryption benchmarks summary

The interactive review lives in the HTML dashboard (auto-loaded from latest BenchmarkDotNet CSV artifacts):

- **[Encryption benchmark dashboard](../../../docs/benchmarks/encryption.html)**
- Hub: [`docs/benchmarks/index.html`](../../../docs/benchmarks/index.html)

## Refresh after a benchmark run

```bash
dotnet run -c Release --project Lyo.Net/Security/Encryption/Lyo.Encryption.Benchmarks
python3 scripts/benchmarks/build_manifests.py --encryption-only
```

Generated data: `docs/benchmarks/data/encryption-latest.json`.

Raw CSV/HTML reports: `BenchmarkDotNet.Artifacts/results/` in this project.

## Running benchmarks

See [README.md](./README.md).
