# Lyo.Benchmark

Helpers used only by `*.Benchmarks` executables. The BenchmarkDotNet counterpart to [`Lyo.Testing`](../../Lyo.Testing/Lyo.Testing.csproj). References [`Lyo.Benchmark.Models`](../Lyo.Benchmark.Models/README.md) plus BenchmarkDotNet and Testcontainers. Consumer-facing models stay dependency-light. Suites share one config, entry point, exporter, and data/container helpers.

`net10.0`, not packable.

## Features

- **`SampleRecord`** (`Data/`). Shared benchmark row type (`Id`, `Name`, `Email`, `Age`, `Balance`, `IsActive`, `CreatedAt`) with `Generate(count, startId)`. CSV, XLSX, and Query benchmark projects use this instead of each defining an identical record and generator.

## Examples

### What a benchmark project must have

```csharp
// Program.cs
using Lyo.Benchmark;

[assembly: BenchmarkReport("hashing", "Hashing",
    Description = "SHA-2/MD5 digests ... payloads are random bytes of DataSize.")] // name + title + methodology

BenchmarkEntry.Run(args);
```

## What a benchmark project must have

That's it. No per-class `[MemoryDiagnoser]` / `[SimpleJob]`. `BenchmarkEntry.Run` discovers the
benchmarks in the entry assembly and runs them under `LyoBenchmarkConfig.Default`, which adds a
default job, the memory diagnoser, and the `LyoBenchmarkExporter`.

## Building blocks

| Type | Role |
| ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BenchmarkEntry` | `Run(args)` starts a `BenchmarkSwitcher` over the entry assembly with the shared config. |
| `LyoBenchmarkConfig` | `ManualConfig` adding the default job, `MemoryDiagnoser`, and the exporter. |
| `Export.LyoBenchmarkExporter` | `IExporter` that builds a `MicroBenchmarkReport` from the `Summary` (GC bytes, ns means, structured params, comparison axis, baseline ratios) and writes `<name>.lyobench.json` into `BenchmarkDotNet.Artifacts`. |
| `[BenchmarkReport(name, title)]` | Assembly attribute naming the report; optional `Description` becomes the report's suite-level methodology. Defaults to the assembly name (`Lyo.Hashing.Benchmarks` → `hashing`) when omitted. |
| `[BenchmarkDescription("…")]` | Method or class narrative. Copied into the measurement / group `description` so reports explain what each benchmark exercises. |
| `[BenchmarkParameter("DataSize", Unit = "bytes", Description = "…")]` | Class attribute (repeatable) explaining a `[Params]` value; becomes a `ParameterDescriptor` on the comparison/group so values like `DataSize = 1048576` read as "1 MB of bytes". |
| `[BenchmarkDataShape(typeof(SampleRecord))]` | Names the model/row type. The exporter reflects over it to emit a `DatasetDescriptor` (CLR types, columns, object/scalar/collection kind, nesting depth), capturing data structure, including nested complexity, instead of just a row count. |
| `[BenchmarkSla(MaxMeanMs = …, MinThroughputMbps = …, MaxAllocatedKb = …, Standard = "…")]` | Method or class budget (method wins; class is the default). The exporter compares the measured mean / derived throughput / allocation against the budget and emits a `Exceeds` / `Meets` / `Miss` verdict + target string on each measurement and comparison row, and rolls one worst-case row per benchmark into the report's `slo` list. Throughput is derived from `SizeParam` (default `DataSize`). See [SLAs](#slas--business-standards). |
| `[ComparisonSuite(Baseline = "…")]` | Marks the class that drives the comparison table (replaces the magic `AlgorithmComparisonBenchmarks` class name). |
| `[ComparisonAxis("Encrypt")]` | Marks a method as part of the comparison table under an axis. Algorithm name is the method name minus the axis suffix (or set `Algorithm` explicitly). |
| `Data.BenchmarkData` | `RandomBytes(n)`, `CompressibleString(n)` payload generators. |
| `Containers.RedisBenchmarkContainer` | Throwaway Redis Testcontainers wrapper for Docker-dependent suites (`Start()` in `[GlobalSetup]`, `Dispose()` in `[GlobalCleanup]`). |

## Output

Output is the shared `lyo.bench/v1` schema (see [`Lyo.Benchmark.Models`](../Lyo.Benchmark.Models/README.md)). `scripts/benchmarks/build_manifests.py` copies the `*.lyobench.json` files into `docs/benchmarks/data/` and rewrites k6 output onto that same schema so the dashboard can render both through one viewer.

## Why data shape is reflected instead of collected at runtime

Each BenchmarkDotNet case runs in a **separate child process**. Static fields filled in `[GlobalSetup]` therefore never reach the exporter, which lives in the host process. `[BenchmarkDataShape(typeof(T))]` is how auto-derivation works instead: the host already has the benchmark `Type`, reflects over that `T` there, and builds a `DatasetDescriptor` without crossing the process boundary.

## Business standards / SLAs

`[BenchmarkSla]` attaches an authored budget and the business-standard reasoning behind it so a number
can be judged against an expectation rather than read in isolation:

* **Latency.** `MaxMeanUs` / `MaxMeanMs` / `MaxMeanNs` (use whichever unit reads best).
* **Throughput.** `MinThroughputMbps` for size-based suites. The exporter derives MB/s from the
  benchmark mean and the byte size in `SizeParam` (default `DataSize`).
* **Allocation.** `MaxAllocatedKb`.
* **`Standard`.** Free text citing the norm, for example "SHA-256 on AES-NI hardware should sustain >= 200 MB/s".

Each declared budget is graded `Exceeds` (comfortably under, mean ≤ 50% of the
latency/alloc budget or throughput ≥ 1.5× the target), `Miss` (over budget), or `Meets`. Verdicts land on every
`ComparisonRow` and `BenchmarkMeasurement`. One worst-case row per benchmark is aggregated into the
report's `slo` list so micro reports get the same SLA summary section the k6 load reports already have.

## Always run with `--join`

`--join` is passed by `python3 scripts/benchmarks/run_dotnet.py` so a suite's benchmark classes collapse into **one** joined `Summary`. The exporter then writes a single `<name>.lyobench.json` that covers every group plus the comparison table. Drop `--join` and `BenchmarkSwitcher` emits one `Summary` per class; the exporter still writes a fixed `<name>.lyobench.json` and only the last class survives.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Benchmark.Models` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `BenchmarkDotNet` `0.15.8` (direct, third-party)
- `Testcontainers.Redis` `4.13.0` (direct, third-party)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)