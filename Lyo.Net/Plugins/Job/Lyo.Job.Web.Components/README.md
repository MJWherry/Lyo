# Lyo.Job.Web.Components

Blazor / MudBlazor dashboard for the Lyo job stack. Drop `JobManagement` on a host page for Statistics, Definitions, Schedules, Runs (progress and SLA breach indicators), worker registry, and workflow views. Uses an injected `IApiClient` and a configurable base route prefix.

The Workers datagrid uses REST heartbeats plus optional AutoRefresh (on by default) for live registry updates.

Targets server-side or interactive Blazor on `net10.0` and pulls in MudBlazor `>= 9.3`.

This package is a Razor component library. It has no `AddXxx` DI registration. The host must already register `IApiClient`.

## Examples

### Architecture

```mermaid
flowchart LR
    UI[JobManagement tabs] --> API[IApiClient → Job API]
    API --> MQ[job.events exchange]
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

`MudTabs` on `JobManagement` expose Statistics, Definitions, Schedules, Runs, Workers, and Workflows.

## Component catalog

| Component | Role |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `JobManagement` | Tabbed dashboard shell. |
| `JobStats` | Aggregated `SpJobStatistic` success rates and counts. |
| `JobDefinitionGrid` | CRUD grid for definitions. |
| `JobDefinitionView` | Editor: basic info with last/next/running activity, parameters, schedules, triggers. |
| `JobDefinitionActivity` | Basic Info activity panel: running/queued counts, last run snapshots, next scheduled slots. Timestamps use `LyoTimestamp` (browser TZ shown); next runs are relative within ±24h. |
| `JobScheduleGrid` | Standalone schedule grid with next/upcoming runs in the browser time zone (`LyoTimestamp`, relative within ±24h). **Run now** creates a job run with that schedule's stored parameters. |
| `JobParameterView` | Definition parameter editor via `LyoParameterEditor` (`LyoTypeSelect` type+shape in one row; Required/Encrypt flags; description, optional default, and Options kind in the expanded body). Loads the definition's latest runs once so formatter-typed parameters preview against real values. |
| `JobScheduleView` | Inline schedule editor: add/remove, enable toggle, atomic day/month flags with selected-state preset chips (Every day / Weekdays / Weekends, Every month / quarter), timezone picker (defaults to the browser IANA id), local start/end date pickers stored as UTC midnight in that zone, blackout calendar, `LyoParameterEditor` schedule-parameter overrides (query-root pickers inherited from definition parameters). **Run now** creates a job run with the selected schedule's stored parameters. Times stay wall-clock `TimeOnly` values. |
| `JobBlackoutCalendarEditor` | Create/unlink a schedule blackout calendar and CRUD its windows (weekdays, calendar month/day-of-month with optional year bounds, dated range, or calculated holiday; Skip/Defer). |
| `JobTriggerView` | Trigger relationships between definitions. |
| `JobRunGrid` | Runs with state pills, **worker** column (machine:pid + instance id), drill-down, **Resync RabbitMQ** (republish queued runs missing from the broker). |
| `JobRunDetailView` | Overview (identity/timing/context), parameters, results, logs ordered by time, **progress**, **SLA** chip, re-run. |
| `JobWorkerInstanceGrid` | Worker datagrid (type, machine, PID, state chips, in-flight, heartbeat, metadata). AutoRefresh on by default. |
| `JobWorkerInstanceView` | Worker detail popup: identity, CPU/memory, queue subscriptions, plus System and Worker metadata tabs. |
| `JobWorkflowView` | Workflow picker + ordered step diagram. |
| `RunJobDialog` | Ad-hoc run with parameter overrides via `LyoParameterValueField` (Options / AllowedValues select or typed JSON, optional encrypt). |

Host a grid or view on its own: each one takes `IApiClient` plus route parameters and does not require the full shell.

## `JobFormatterContext`

A formatter-typed job parameter is a template, so authors need the keys that will actually resolve. `JobFormatterContext.Build(definition, latestRuns, schedule)` builds that key list and matches both runtime stages:

- **Scheduler** (`JobScheduler.BuildTemplateData`): `Definition`, `LastRun`, `LastSuccessfulRun`, `LastFailedRun`, `Trigger`, `TriggeredByRun`, `Schedule`, plus flattened `{prefix}_Result_{key}` / `{prefix}_Parameter_{key}`, `Schedule_Parameter_{key}`, and `Trigger_Parameter_{key}` entries.
- **Worker** (`JobWorkerParameterFormatter.SeedJobRun`): the `jobrun` bag (run properties and a `Parameters` map). `{jobrun.parameters.emailTo}` reads from that map.

Views receive the list as `FormatterContext`. Host-supplied `AddContext` keys arrive on a separate path through `AddLyoFormatterValueEditor`. Definitions that have never run still autocomplete on the run paths because `LyoSampleShape` substitutes for the missing `JobRunRes`. `JobFormatterContextTests` checks the key list against the scheduler so the editor cannot drift from runtime resolution.

## UI features

| Feature | Where surfaced |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Progress | `JobRunGrid` progress column; `JobRunDetailView` linear bar + message |
| SLA | `JobRunDetailView` breach indicator when `SlaBreached == true` |
| Alerting | Definition/run alert flags in detail view |
| Queue resync | `JobRunGrid` **Resync RabbitMQ** toolbar. `POST Job/Run/Resync` (scoped when a definition filter is set) |
| Worker registry | **Workers** tab (`JobWorkerInstanceGrid`) |
| Workflows | **Workflows** tab (`JobWorkflowView`) |
| Schedules / blackout calendars | **Schedules** tab. Add/remove schedules, enable toggle, timezone dropdown defaulting to the browser zone, local date pickers, inline blackout calendar/windows (weekdays, calendar days, date range, holiday), misfire/cron fields |
| Dry run | `RunJobDialog` can pass `DryRun` for validate-only runs (no worker dispatch) |

## `JobColorHelper`

One static helper applies the same colors and icons to job state, result, and log level.

| Member | Returns / behavior |
| -------------------------------------------- | ---------------------------------------------------------------------- |
| `ForState(JobState)` | MudBlazor `Color` mapping. |
| `ForWorkerState(JobWorkerInstanceState)` | Running→Success, Draining→Warning, Stopped→Default. |
| `ForResult(JobRunResult?)` | Success / warning / error colors. |
| `ForLogLevel(JobLogLevel)` | Trace/Debug→Default, Info→Info, Warning→Warning, Error/Critical→Error. |
| `StateIcon` / `ResultIcon` | Material icon names. |
| `FormatDuration` / `FormatDurationFromDates` | Human-readable durations. |
| `GetEnumDescription<T>(T value)` | `[Description]` attribute or field name. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)