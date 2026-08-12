# Lyo.Benchmark.Models

Consumer-facing models and builders for the **unified Lyo benchmark report schema** (`lyo.bench/v1`). One polymorphic document represents both BenchmarkDotNet micro-benchmarks and
k6 load tests, so a single viewer (or your portfolio / test gateway) can render any report file by switching on one discriminator.

Minimal dependencies (only `System.Text.Json`); targets `netstandard2.0;net10.0`. No BenchmarkDotNet / Testcontainers baggage — the benchmark-only helpers live in [
`Lyo.Benchmark`](../Lyo.Benchmark/README.md).

## Examples

### Consuming a report

```csharp
using System.Text.Json;
using Lyo.Benchmark.Models;

var report = JsonSerializer.Deserialize<BenchmarkReport>(json)!;
switch (report) {
    case MicroBenchmarkReport micro:
        foreach (var group in micro.Groups) { /* render Method x Parameters */ }
        break;
    case LoadTestReport load:
        foreach (var scenario in load.Scenarios) { /* render p95 / throughput */ }
        break;
}
```

## Polymorphic report tree

| Type                         | Discriminator | Role                                                                                                       |
|------------------------------|---------------|------------------------------------------------------------------------------------------------------------|
| `BenchmarkReport` (abstract) | —             | Shared envelope: `Schema`, `Name`, `Title`, `Description`, `RunId`, `GeneratedAt`, `Environment`, `Notes`. |
| `MicroBenchmarkReport`       | `micro`       | BenchmarkDotNet: `Groups` (classes -> measurements) + optional `Comparison` table + `Slo` / `Grades`.      |
| `LoadTestReport`             | `load`        | k6: `Cases`, `Scenarios`, `Rollups`, `Slo`, `Grades`.                                                      |

## Polymorphic report tree — Descriptive context

- `BenchmarkReport.Description` — suite-level methodology ("what / how", the data set, payload kinds).
- `BenchmarkGroup.Description` — what a class measures; `BenchmarkMeasurement.Description` — what a single method does.
- `BenchmarkGroup.Parameters` / `ComparisonTable.Parameters` — a list of `ParameterDescriptor { Name, Unit, Description }` explaining each `[Params]` value (e.g. `DataSize` is
  bytes, `RowCount` is rows).
- `BenchmarkGroup.Dataset` — a `DatasetDescriptor` capturing the data structure: `TypeName`, `ColumnCount`, `MaxNestingDepth`, and a `Columns` tree of
  `ColumnDescriptor { Name, Type, Kind (scalar|object|collection), Children }`. This is what surfaces nested-property complexity (e.g. a CSV/XLSX row type or a mapping entity with
  a nested child collection) that a row count alone hides.
- `LoadTestReport.Cases` — a list of `LoadCase { Case, Endpoint, Description, WhereClauses, Filters, SortFields, Includes, SelectionFieldCount }` describing each k6 query case's
  structure (so `query_with_subquery` vs `baseline` is interpretable). `Hotspot.Case` joins to it.

## Polymorphic report tree — SLAs / business standards (micro)

Micro reports carry the same SLA assessment k6 reports do. From a `[BenchmarkSla]` budget the exporter sets, per measurement and comparison row:

- `BenchmarkMeasurement` — `ThroughputMbps` (size-based suites), `SlaTarget` (e.g. `<= 2 ms`, `>= 300 MB/s`), `SlaResult` (`Meets` / `Exceeds` / `Miss`), `SlaStandard` (the
  business-standard text).
- `ComparisonRow` — `ThroughputMbps`, `SlaTarget`, `SlaResult`.
- `MicroBenchmarkReport.Slo` — one worst-case `SloRow` per benchmark (reusing the load report's `SloRow` type); `MicroBenchmarkReport.Grades` reuses `GradeRow`. The viewer renders
  an "SLA assessment" section from these, plus a verdict badge column on the measurement and comparison tables.

The discriminator property is named `type` (via
`[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]`), mirroring the
`WhereClause` AST in [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md). JSON property names follow the default camelCase policy.

```jsonc
// micro report (truncated)
{
  "type": "micro",
  "schema": "lyo.bench/v1",
  "name": "hashing",
  "title": "Hashing",
  "runId": "20260627-101500",
  "generatedAt": "2026-06-27T14:15:00+00:00",
  "environment": { "tool": "BenchmarkDotNet", "toolVersion": "0.15.8", "runtime": ".NET 10.0", "cpu": "..." },
  "description": "SHA-2/MD5 content digests ... payloads are random bytes of DataSize.",
  "groups": [
    {
      "name": "AlgorithmComparisonBenchmarks",
      "description": "Hashes the same random buffer with SHA-256/384/512 and MD5 ...",
      "parameters": [ { "name": "DataSize", "unit": "bytes", "description": "Size of the random input buffer (1 KB, 1 MB, 10 MB)." } ],
      "measurements": [
        { "method": "Sha256_Hash", "description": "SHA-256 digest of the payload (baseline).",
          "parameters": { "DataSize": "1048576" },
          "meanNs": 512345.6, "allocatedBytes": 80, "ratioToBaseline": 1.0, "isBaseline": true, "axis": "Hash",
          "throughputMbps": 2046.0, "slaTarget": ">= 150 MB/s", "slaResult": "Exceeds",
          "slaStandard": "SHA-2 on AES-NI hardware should sustain >= 150 MB/s." }
      ]
    }
  ],
  "comparison": {
    "baseline": "Sha256",
    "parameters": [ { "name": "DataSize", "unit": "bytes" } ],
    "groups": [ { "axis": "Hash", "rows": [ { "algorithm": "Sha256", "paramLabel": "1 MB", "meanNs": 512345.6, "ratioToBaseline": 1.0, "throughputMbps": 2046.0, "slaTarget": ">= 150 MB/s", "slaResult": "Exceeds" } ] } ]
  },
  "slo": [ { "area": "Sha256_Hash", "target": ">= 150 MB/s — SHA-2 on AES-NI hardware should sustain >= 150 MB/s.", "latest": "512.35 µs (2046 MB/s)", "result": "Exceeds" } ]
}
```

A `csv` micro report adds a `dataset` per group, capturing the row structure behind the row count:

```jsonc
"dataset": {
  "typeName": "SampleRecord", "columnCount": 7, "maxNestingDepth": 0,
  "columns": [
    { "name": "Id", "type": "int", "kind": "scalar" },
    { "name": "Balance", "type": "decimal", "kind": "scalar" },
    { "name": "CreatedAt", "type": "DateTime", "kind": "scalar" }
  ],
  "notes": "Flat record generated by SampleRecord.Generate ..."
}
```

```jsonc
// load report (truncated)
{
  "type": "load",
  "schema": "lyo.bench/v1",
  "name": "query-api",
  "title": "Query API (k6)",
  "description": "k6 load/stress/spike/soak against the person API ...",
  "cases": [
    { "case": "complex_querynode", "endpoint": "query",
      "description": "Nested AND/OR QueryNode where-clause tree ...", "sortFields": [], "includes": [] },
    { "case": "projection_roots", "endpoint": "queryproject",
      "description": "Root scalar fields only ...", "selectionFieldCount": 5 }
  ],
  "scenarios": [
    { "name": "query_load", "profile": "load", "endpoint": "query",
      "latency": { "p95": 86.9, "p99": 122.8, "avg": 18.4, "unit": "ms" },
      "throughput": 6.99, "requests": 1260, "checksPass": 100.0, "droppedIterations": 0, "hotspots": [] }
  ],
  "rollups": [ { "endpoint": "query", "totalRequests": 1260, "checksPass": 100.0 } ],
  "slo": [ { "area": "Query load", "target": "300-700 ms", "latest": "86 ms", "result": "Exceeds target" } ],
  "grades": [ { "category": "Query load", "grade": "A", "rationale": "86 ms p95 with 100% checks" } ]
}
```

`MetricStat` is shared by both kinds; micro values are in `ns`, load values in `ms`
(see `Unit`).

## Consuming a report

The base type round-trips polymorphically — deserialize as `BenchmarkReport` and pattern-match the concrete type:

## Builders

| Builder                       | Produces               | Notes                                                                                                                                                                                                            |
|-------------------------------|------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `MicroBenchmarkReportBuilder` | `MicroBenchmarkReport` | `Create(name, title)`, `WithDescription`, `WithRun`, `WithEnvironment`, `AddNote`, `AddMeasurement(group, m)`, `DescribeGroup(group, description, parameters, dataset)`, `WithComparison`, `AddSlo`, `AddGrade`. |
| `LoadTestReportBuilder`       | `LoadTestReport`       | `Create(name, title)`, `WithDescription`, `WithRun`, `WithEnvironment`, `AddNote`, `AddCase`, `AddScenario`, `AddRollup`, `AddSlo`, `AddGrade`.                                                                  |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)