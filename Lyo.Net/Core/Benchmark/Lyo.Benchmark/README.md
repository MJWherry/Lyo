# Lyo.Benchmark

Benchmark-only helpers shared by every `*.Benchmarks` executable. The BenchmarkDotNet analogue of [`Lyo.Testing`](../../Lyo.Testing/Lyo.Testing.csproj). References [`Lyo.Benchmark.Models`](../Lyo.Benchmark.Models/README.md) plus BenchmarkDotNet and Testcontainers. Consumer-facing models stay dependency-light. Suites share one config, entry point, exporter, and data/container helpers.

`net10.0`, not packable.

## Examples

### What a benchmark project needs

```csharp
// Program.cs
using Lyo.Benchmark;

[assembly: BenchmarkReport("hashing", "Hashing",
    Description = "SHA-2/MD5 digests ... payloads are random bytes of DataSize.")] // name + title + methodology

BenchmarkEntry.Run(args);
```

## What a benchmark project needs

That's it. No per-class `[SimpleJob]` / `[MemoryDiagnoser]`. `BenchmarkEntry.Run` discovers the
benchmarks in the entry assembly and runs them under `LyoBenchmarkConfig.Default`, which adds a
default job, the memory diagnoser, and the `LyoBenchmarkExporter`.

## Pieces

| Type | Role |
| ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BenchmarkEntry` | `Run(args)` starts a `BenchmarkSwitcher` over the entry assembly with the shared config. |
| `LyoBenchmarkConfig` | `ManualConfig` adding the default job, `MemoryDiagnoser`, and the exporter. |
| `Export.LyoBenchmarkExporter` | `IExporter` that builds a `MicroBenchmarkReport` from the `Summary` (ns means, GC bytes, structured params, baseline ratios, comparison axis) and writes `<name>.lyobench.json` into `BenchmarkDotNet.Artifacts`. |
| `[BenchmarkReport(name, title)]` | Assembly attribute naming the report; optional `Description` becomes the report's suite-level methodology. Defaults to the assembly name (`Lyo.Hashing.Benchmarks` → `hashing`) when omitted. |
| `[BenchmarkDescription("…")]` | Class or method narrative. Copied into the group / measurement `description` so reports explain what each benchmark exercises. |
| `[BenchmarkParameter("DataSize", Unit = "bytes", Description = "…")]` | Class attribute (repeatable) explaining a `[Params]` value; becomes a `ParameterDescriptor` on the group/comparison so values like `DataSize = 1048576` read as "1 MB of bytes". |
| `[BenchmarkDataShape(typeof(SampleRecord))]` | Names the row/model type. The exporter reflects over it to emit a `DatasetDescriptor` (columns, CLR types, scalar/object/collection kind, nesting depth), capturing data structure, including nested complexity, instead of just a row count. |
| `[BenchmarkSla(MaxMeanMs = …, MinThroughputMbps = …, MaxAllocatedKb = …, Standard = "…")]` | Class or method budget (method wins; class is the default). The exporter compares the measured mean / derived throughput / allocation against the budget and emits a `Meets` / `Exceeds` / `Miss` verdict + target string on each measurement and comparison row, and rolls one worst-case row per benchmark into the report's `slo` list. Throughput is derived from `SizeParam` (default `DataSize`). See [SLAs](#slas--business-standards). |
| `[ComparisonSuite(Baseline = "…")]` | Marks the class that drives the comparison table (replaces the magic `AlgorithmComparisonBenchmarks` class name). |
| `[ComparisonAxis("Encrypt")]` | Marks a method as part of the comparison table under an axis. Algorithm name is the method name minus the axis suffix (or set `Algorithm` explicitly). |
| `Data.BenchmarkData` | `CompressibleString(n)`, `RandomBytes(n)` payload generators. |
| `Containers.RedisBenchmarkContainer` | Throwaway Redis Testcontainers wrapper for Docker-dependent suites (`Start()` in `[GlobalSetup]`, `Dispose()` in `[GlobalCleanup]`). |

## Output

The exporter emits the unified `lyo.bench/v1` schema (see [`Lyo.Benchmark.Models`](../Lyo.Benchmark.Models/README.md)). `scripts/benchmarks/build_manifests.py` copies those `*.lyobench.json` files into `docs/benchmarks/data/` and normalizes k6 output to the same schema; the dashboard renders both through one viewer.

## Why data shape is reflected, not collected at runtime

BenchmarkDotNet runs each benchmark in a **separate child process**, so static state populated in a benchmark's `[GlobalSetup]` is invisible to the exporter (which runs in the host process). Auto-derivation is therefore driven by `[BenchmarkDataShape(typeof(T))]`: the exporter has the benchmark `Type` in the host process and reflects over the named `T` there, producing an accurate `DatasetDescriptor` without crossing the process boundary.

## SLAs / business standards

`[BenchmarkSla]` attaches an authored budget and the business-standard reasoning behind it so a number
can be judged against an expectation rather than read in isolation:

* **Latency.** `MaxMeanMs` / `MaxMeanUs` / `MaxMeanNs` (use whichever unit reads best).
* **Throughput.** `MinThroughputMbps` for size-based suites. The exporter derives MB/s from the
  benchmark mean and the byte size in `SizeParam` (default `DataSize`).
* **Allocation.** `MaxAllocatedKb`.
* **`Standard`.** Free text citing the norm, for example "SHA-256 on AES-NI hardware should sustain >= 200 MB/s".

Each declared budget is graded `Miss` (over budget), `Exceeds` (comfortably under, mean ≤ 50% of the
latency/alloc budget or throughput ≥ 1.5× the target), or `Meets`. Verdicts land on every
`BenchmarkMeasurement` and `ComparisonRow`. One worst-case row per benchmark is aggregated into the
report's `slo` list so micro reports get the same SLA summary section the k6 load reports already have.

## Running: always `--join`

`python3 scripts/benchmarks/run_dotnet.py` passes `--join` so every benchmark class in a suite produces **one** joined `Summary`, and the exporter writes a single `<name>.lyobench.json` covering all groups + the comparison table. Without `--join`, `BenchmarkSwitcher` emits one `Summary` per class and the exporter (which writes a fixed `<name>.lyobench.json`) keeps only the last class.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Benchmark.Models` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `BenchmarkDotNet` `0.15.8` (direct, third-party)
- `Testcontainers.Redis` `4.13.0` (direct, third-party)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)