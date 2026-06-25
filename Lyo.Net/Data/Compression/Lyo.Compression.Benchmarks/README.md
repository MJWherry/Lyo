# Lyo.Compression.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) harness for the `Lyo.Compression` library. It produces reproducible micro and macro benchmarks for every supported algorithm and is
the source of the numbers behind [`BENCHMARK_SUMMARY.md`](BENCHMARK_SUMMARY.md).

> Console executable (`<OutputType>Exe</OutputType>`, `net10.0`). Run from a Release build; do not run benchmarks under the debugger.

## What ships

| File                                                                   | Suite                                                                                                                                                                                                                                                                     |
|------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [`Program.cs`](Program.cs)                                             | `BenchmarkSwitcher.FromAssembly(...).Run(args)` — exposes filtering, `--list`, and the interactive picker built into BenchmarkDotNet.                                                                                                                                     |
| [`GZipCompressionBenchmarks.cs`](GZipCompressionBenchmarks.cs)         | Baseline GZip compression / decompression at three fixed payload sizes (1 KB, 1 MB, 10 MB). Useful for tracking GZip-specific regressions over time.                                                                                                                      |
| [`AlgorithmComparisonBenchmarks.cs`](AlgorithmComparisonBenchmarks.cs) | Side-by-side compress + decompress for every algorithm on `Lyo.Compression` (GZip, Deflate, ZstdSharp, Snappier, LZ4, LZMA, BZip2, XZ; plus Brotli and ZLib on non-`netstandard2.0` targets). Parameterized over `DataSize` `[1 KB, 1 MB, 10 MB, 100 MB]` via `[Params]`. |
| [`LargeFileStreamingBenchmarks.cs`](LargeFileStreamingBenchmarks.cs)   | Stream-based compression / decompression for GZip and Zstd at 100 MB, 1 GB, and 2 GB. Validates that the streaming API stays memory-bounded — note the 2 GB run requires enough disk space for the temp file.                                                             |
| [`BENCHMARK_SUMMARY.md`](BENCHMARK_SUMMARY.md)                         | Pointer to [HTML benchmark dashboard](../../../docs/benchmarks/compression.html) (auto-generated from CSV).                                                                                                                                                             |

All benchmark types are decorated with `[SimpleJob(RuntimeMoniker.HostProcess)]` and `[MemoryDiagnoser]` so the results include managed allocations and run inside the host process
for fast iteration.

## Dependencies

| Package                               | Version  |
|---------------------------------------|----------|
| `BenchmarkDotNet`                     | `0.15.8` |
| `Lyo.Compression` (project reference) | —        |

## Running the benchmarks

From the solution root:

```bash
# List available benchmarks
dotnet run -c Release --project Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks -- --list flat

# Run all benchmarks (long — expect tens of minutes for the full matrix)
dotnet run -c Release --project Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks

# Filter by class
dotnet run -c Release --project Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks -- --filter '*GZipCompressionBenchmarks*'

# Filter by single benchmark name
dotnet run -c Release --project Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks -- --filter '*AlgorithmComparison*.GZip_Compress'

# Limit AlgorithmComparison data sizes
dotnet run -c Release --project Lyo.Net/Data/Compression/Lyo.Compression.Benchmarks -- --filter '*AlgorithmComparison*' --runtimes net10.0 --memory
```

Results are written under `BenchmarkDotNet.Artifacts/` next to the binary. Refresh the HTML dashboard after runs:

```bash
python3 scripts/benchmarks/build-manifests.py --compression-only
```

See [HTML benchmark dashboard](../../../docs/benchmarks/compression.html) and [`BENCHMARK_SUMMARY.md`](BENCHMARK_SUMMARY.md).

## Workflow when modifying compression code

1. Capture a clean baseline before your change: `dotnet run -c Release ...` and archive the artifacts folder.
2. Apply the code change and rerun the same filter set.
3. Compare with BenchmarkDotNet's built-in `--statisticalTest` flag or paste both `*.md` tables into a diff.
4. Regenerate the dashboard manifest: `python3 scripts/benchmarks/build-manifests.py --compression-only`.

## Notes / caveats

- Benchmark payloads use `RandomNumberGenerator.Fill(...)` so the data is incompressible by design. That isolates **throughput**; absolute compression ratios on real-world payloads
  will be very different.
- `LargeFileStreamingBenchmarks` uses real `FileStream`s and can consume several GB of disk space — run only on a host with sufficient free space.
- `Brotli` and `ZLib` benchmarks are gated by `#if !NETSTANDARD2_0`. The executable targets `net10.0` today, so both run; if the project is ever multi-targeted, those benchmarks
  will fall off the `netstandard2.0` matrix.
- Benchmarks construct `CompressionService` directly (no DI) with `EnableMetrics = false` so metric overhead does not pollute the measurements.

## See also

- [`Lyo.Compression`](../Lyo.Compression/README.md) — algorithms, streaming API, and `CompressionServiceOptions`.
