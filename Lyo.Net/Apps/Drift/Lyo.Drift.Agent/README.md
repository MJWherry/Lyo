# Lyo.Drift.Agent

Hosted agent: upserts an InstanceKey, constructs one FileSystemWatcher per watch, copies DTOs on ScanCompleted onto a bounded Channel, and POSTs from the hosted-service loop so the debounce thread never awaits HTTP. System-info uses SystemInfoDriftProjection with DriftAgent:SystemInfo:Include (empty = identity-only). Identical collector hashes are not re-inserted.

## Features

- **Identity system-info.** Empty Include projects OS/CPU/RAM total/host/drives/NIC names only. Opt in with Variables, InterfaceAddresses, Monitors, Locale.
- **Off-thread ingest.** ScanCompleted and the system-info timer only enqueue DTOs.
- **Regex include/exclude.** Include/exclude patterns are .NET regular expressions passed through to the watcher.
- **404 re-upsert.** Heartbeat 404 reuses the same InstanceKey.

## Examples

### Register the agent

```csharp
services.AddLyoApiClient(o => o.BaseUrl = configuration["DriftAgent:ApiBaseUrl"]);
services.AddDriftClient(new() { RoutePrefix = configuration["DriftAgent:ApiBaseUrl"] });
services.AddDriftAgentFromConfiguration(configuration);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Diff` (direct, lyo)
- `Lyo.Drift.Client` (direct, lyo)
- `Lyo.Drift.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileSystemWatcher` (direct, lyo)
- `Lyo.SystemInformation` (direct, lyo)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.FileSystemWatcher.Models` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)