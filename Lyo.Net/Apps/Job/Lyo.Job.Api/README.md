# Lyo.Job.Api

Maps job definition CRUD, run lifecycle, stats, next-runs, and scheduler batch routes. Register the store with `AddPostgresJobManagement` from `Lyo.Job.Postgres` first, then call `BuildJobGroup`. Standalone host: `Lyo.Job.Api.Host`.

## Features

- `BuildJobGroup` maps Job CRUD plus run lifecycle (`Started`, `Finished`, `Cancel`, `Requeue`, `Rerun`, `Heartbeat`, child runs).
- Export contributors are optional on the host (`AddLyoApiExport` + CSV/XLSX).

## Examples

### Map the Job API

```csharp
services.AddPostgresJobManagementFromConfiguration(configuration);
var app = builder.Build();
app.BuildJobGroup();
```

## Endpoint matrix (`BuildJobGroup`)

Tag `"Job"`. Definitions get CRUD + export; runs expose Query/Get/Delete/DeleteBulk + export.

| Route prefix | Entity | Notes |
| ---------------------------------------------------------- | ------------------ | -------------------------------------------------------------------------------------------------------------------- |
| `Job/Definition` | `JobDefinition` | Version bump + audit on update |
| `Job/Definition/Parameter` | `JobParameter` | Encryption on write |
| `Job/Schedule` | `JobSchedule` | Misfire policy, calendar link, cron |
| `Job/Triggers` | `JobTrigger` | |
| `Job/BlackoutCalendar`, `Job/BlackoutCalendar/Window` | Blackout calendars | |
| `Job/Workflow`, `Job/Workflow/Step`, `Job/Workflow/Run`, … | Workflows | |
| `Job/WorkerInstance` | Worker registry | Created by workers |
| `Job/Run` | `JobRun` | Progress, SLA, idempotency fields |
| `Job/Run/{id}/Children` | Batch fan-out | `JobCreateChildRunsReq` |
| `POST Job/Definition/LatestRuns` | Batch latest-runs | Latest / latest-successful / latest-failed run per definition id (scheduler refresh) |
| `GET Job/Definition/{id}/Stats` | Stats projection | Rolling window aggregates plus current `RunningCount` / `QueuedCount` |
| `GET Job/Definition/{id}/NextRuns` | Upcoming slots | Merged next-run timestamps across enabled schedules (blackout-aware) |
| `POST Job/Run/Resync` | Queued-run resync | Optional `definitionId`; peeks worker + `.wait` queues and republishes missing due `Queued` runs (`JobRunResyncRes`) |

Alongside CRUD, lifecycle routes (`RunStarted`, `RunFinished`, `RunRequeue`, `RunsResync`, `RunHeartbeat`, `RunLog`, …) are mapped for scheduler/worker hosts.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.Job.Postgres` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.MessageQueue` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Scheduler` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)