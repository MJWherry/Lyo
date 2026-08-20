# Lyo.Job.Web.Components

Blazor / MudBlazor dashboard for the Lyo job stack. Add `JobManagement` to a host page for Statistics, Definitions, Schedules, Runs (progress and SLA breach indicators), worker registry, and workflow views. Uses an injected `IApiClient` and a configurable base route prefix.

Pair with [`Lyo.Job.SignalR`](../Lyo.Job.SignalR/README.md) for a live dashboard that receives `JobEvent` broadcasts without polling.

Targets server-side or interactive Blazor on `net10.0` and pulls in MudBlazor `>= 9.3`.

This package is a Razor component library. It has no `AddXxx` DI registration. The host must already register `IApiClient` (and optionally [`Lyo.Job.SignalR`](../Lyo.Job.SignalR/README.md) for live updates).

## Examples

### Architecture

```mermaid
flowchart LR
    UI[JobManagement tabs] --> API[IApiClient → Job API]
    UI -. optional .-> SR[SignalR JobHub]
    SR --> MQ[job.events exchange]
    MQ --> API
```

### Live dashboard (SignalR)

```csharp
// Program.cs
services.AddJobSignalR();
var app = builder.Build();
app.MapJobHub(); // default /hubs/job
```

## Top-level entry point

```razor
@using Lyo.Job.Web.Components

<JobManagement BaseRoute="Job" StatisticsRoute="api/job-stats/recent" />
```

| Parameter | Notes |
| ----------------- | ---------------------------------------------------------------------------------------------- |
| `BaseRoute` | Route prefix for job endpoints. Required. Defaults to `"Job"`. |
| `StatisticsRoute` | Optional URL returning `SpJobStatistic` rows. When omitted, the stats tab shows an info alert. |

`JobManagement` renders tabbed `MudTabs`: Statistics, Definitions, Schedules, Runs, Workers, Workflows.

## Live dashboard (SignalR)

For real-time run/alert/definition updates, register SignalR in the host and subscribe from your page:

```javascript
// Client-side (example)
connection.on("JobEvent", (evt) => { /* refresh grids or patch rows */ });
```

See [`Lyo.Job.SignalR`](../Lyo.Job.SignalR/README.md) for event types (`run.created`, `run.finished`, `alert`, …).

## Component catalog

| Component | Role |
| --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `JobManagement` | Tabbed dashboard shell. |
| `JobStats` | Aggregated `SpJobStatistic` success rates and counts. |
| `JobDefinitionGrid` | CRUD grid for definitions. |
| `JobDefinitionView` | Editor: basic info with last/next/running activity, parameters, schedules, triggers. |
| `JobDefinitionActivity` | Basic Info activity panel: running/queued counts, last run snapshots, next scheduled slots. Timestamps use `LyoTimestamp` (browser TZ shown); next runs are relative within ±24h. |
| `JobScheduleGrid` | Standalone schedule grid with next/upcoming runs in the browser time zone (`LyoTimestamp`, relative within ±24h). |
| `JobParameterView` | Definition parameter grid (including encrypted markers, AllowedValues, and Options editor). |
| `JobScheduleView` | Inline schedule editor: add/remove, enable toggle, atomic day/month flags, timezone picker (defaults to the browser IANA id), local start/end date pickers stored as UTC midnight in that zone, blackout calendar, schedule-parameter overrides (query-root pickers inherited from definition parameters). Times stay wall-clock `TimeOnly` values. |
| `JobBlackoutCalendarEditor` | Create/unlink a schedule blackout calendar and CRUD its windows (recurring days or dated range, Skip/Defer). |
| `JobTriggerView` | Trigger relationships between definitions. |
| `JobRunGrid` | Runs with state pills, **progress bar** column, drill-down, **Resync RabbitMQ** (republish queued runs missing from the broker). |
| `JobRunDetailView` | Parameters, results, logs, **progress**, **SLA breach** chip, alert flags, re-run. |
| `JobWorkerInstanceGrid` | Live worker registry (type, machine, PID, in-flight, heartbeat). |
| `JobWorkflowView` | Workflow picker + ordered step diagram. |
| `RunJobDialog` | Ad-hoc run with parameter overrides; Options / AllowedValues render as MudSelect with live sibling binding. |

Every grid / view accepts `IApiClient` and route parameters so components can be hosted independently of the full shell.

## UI features

| Feature | Where surfaced |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Progress | `JobRunGrid` progress column; `JobRunDetailView` linear bar + message |
| SLA | `JobRunDetailView` breach indicator when `SlaBreached == true` |
| Alerting | Definition/run alert flags in detail view |
| Queue resync | `JobRunGrid` **Resync RabbitMQ** toolbar. `POST Job/Run/Resync` (scoped when a definition filter is set) |
| Worker registry | **Workers** tab (`JobWorkerInstanceGrid`) |
| Workflows | **Workflows** tab (`JobWorkflowView`) |
| Schedules / blackout calendars | **Schedules** tab. Add/remove schedules, enable toggle, timezone dropdown defaulting to the browser zone, local date pickers, inline blackout calendar/windows, misfire/cron fields |
| Dry run | `RunJobDialog` can pass `DryRun` for validate-only runs (no worker dispatch) |

## `JobColorHelper`

Static helper for consistent visual treatment of job state, result, and log level.

| Member | Returns / behavior |
| -------------------------------------------- | ---------------------------------------------------------------------- |
| `ForState(JobState)` | MudBlazor `Color` mapping. |
| `ForResult(JobRunResult?)` | Success / warning / error colors. |
| `ForLogLevel(JobLogLevel)` | Trace/Debug→Default, Info→Info, Warning→Warning, Error/Critical→Error. |
| `StateIcon` / `ResultIcon` | Material icon names. |
| `FormatDuration` / `FormatDurationFromDates` | Human-readable durations. |
| `GetEnumDescription<T>(T value)` | `[Description]` attribute or field name. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)