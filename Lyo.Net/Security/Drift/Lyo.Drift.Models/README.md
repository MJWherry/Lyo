# Lyo.Drift.Models

Contract library for Drift. Agents and the API share instance upsert payloads, file-tree and system-info snapshots, persistable object-graph difference DTOs, and a configurable SystemInfoDriftProjection. The default is identity-only (OS, CPU, RAM total, host, drives, NIC names); volatile fields are never mapped.

## Features

- **Identity default.** SystemInfoDriftProjection.From omits env vars, NIC addresses, monitors, locale, uptime, free disk, and process lifetime unless named groups are passed.
- **DTO diffs.** ObjectGraphDifferenceDto stores JSON leaves, not object?.
- **Routes.** Constants.Rest.Drift names Instance, Snapshot, Diff, and Change ingest paths.

## Examples

### Project system info

```csharp
var projection = SystemInfoDriftProjection.From(SystemInfoCollector.Collect());
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileSystemWatcher.Models` (direct, lyo)
- `Lyo.SystemInformation` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)