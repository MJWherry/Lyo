# Lyo.Job.Web.Components

Blazor / MudBlazor dashboard for the Lyo job-management stack. Drop `JobManagement` into a host page and it renders the full Statistics / Definitions / Runs experience using only
an injected `IApiClient` and the base route prefix.

All components target server-side or interactive Blazor in `net10.0` and pull in MudBlazor `>= 9.3` for layout and inputs.

## Top-level entry point

```razor
@using Lyo.Job.Web.Components

<JobManagement BaseRoute="Job" StatisticsRoute="api/job-stats/recent" />
```

| Parameter         | Notes                                                                                                                                                       |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `BaseRoute`       | Route prefix for job endpoints. Required. `JobDefinitionGrid` uses `{BaseRoute}/Definition`, `JobRunGrid` uses `{BaseRoute}/Run`, etc. Defaults to `"Job"`. |
| `StatisticsRoute` | Optional URL that returns `SpJobStatistic` rows for the stats tab. When omitted, the stats tab is empty.                                                    |

The component pulls `IApiClient` from DI and renders a tabbed `MudTabs` shell with `JobStats`, `JobDefinitionGrid`, and `JobRunGrid`.

## Component catalog

| Component           | Role                                                                                               |
|---------------------|----------------------------------------------------------------------------------------------------|
| `JobManagement`     | Tabbed dashboard shell (Statistics + Definitions + Runs).                                          |
| `JobStats`          | Renders aggregated `SpJobStatistic` data for the stats tab.                                        |
| `JobDefinitionGrid` | `LyoDataGrid` of `JobDefinitionRes` with create / edit / detail navigation.                        |
| `JobDefinitionView` | Editor for a single definition; hosts `JobParameterView`, `JobScheduleView`, and `JobTriggerView`. |
| `JobParameterView`  | CRUD grid for a definition's parameters.                                                           |
| `JobScheduleView`   | CRUD grid for a definition's schedules.                                                            |
| `JobTriggerView`    | CRUD grid for trigger relationships between definitions.                                           |
| `JobRunGrid`        | `LyoDataGrid` of `JobRunRes` with status pills and drill-down to `JobRunDetailView`.               |
| `JobRunDetailView`  | Single run view: parameters, results, logs, and re-run actions.                                    |
| `RunJobDialog`      | Dialog for kicking off an ad-hoc run from a `JobDefinitionRes`, including parameter overrides.     |

Every grid / view component accepts at minimum the `IApiClient` and the relevant route (`BaseRoute`, `DefinitionRoute`, `ScheduleRoute`, `TriggerRoute`, `RunRoute`, …), so they can
be hosted independently if you don't want the full `JobManagement` shell.

## `JobColorHelper`

Static helper for consistent visual treatment of job state, result, and log level. Mostly used internally but also useful when extending the components.

| Member                                       | Returns / behavior                                                                      |
|----------------------------------------------|-----------------------------------------------------------------------------------------|
| `ForState(JobState)`                         | `Color` — `Info`/`Warning`/`Success`/`Default`.                                         |
| `ForResult(JobRunResult?)`                   | `Color` — success / warning / error / default mapping.                                  |
| `ForLogLevel(JobLogLevel)`                   | `Color` — Trace/Debug→Default, Info→Info, Warning→Warning, Error/Critical→Error.        |
| `StateIcon(JobState)`                        | MudBlazor material icon name.                                                           |
| `ResultIcon(JobRunResult?)`                  | MudBlazor material icon name.                                                           |
| `FormatDuration(double? ms)`                 | Human-readable ms / s / min. `"—"` for null.                                            |
| `FormatDurationFromDates(started, finished)` | Same, computed from two timestamps.                                                     |
| `GetEnumDescription<T>(T value)`             | Returns the `[Description]` attribute on an enum field, falling back to the field name. |

## Dependencies

*(Synchronized from `Lyo.Job.Web.Components.csproj`.)*

**Target framework:** `net10.0` (Razor SDK)

### Framework references

- `Microsoft.AspNetCore.App`

### NuGet packages

| Package     | Version  |
|-------------|----------|
| `MudBlazor` | `[9.3,)` |

### Project references

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.Web.Components`](../../Web/Lyo.Web.Components/README.md)
