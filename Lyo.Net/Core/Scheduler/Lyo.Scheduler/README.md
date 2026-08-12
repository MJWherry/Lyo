# Lyo.Scheduler

In-process scheduler service for executing actions at scheduled times. Supports **SetTimes**, **Interval**, **OneShot**, and **Cron** schedules (5- or 6-field expressions) with
logging, metrics, and optional state persistence via `ISchedulerStateStore`.

## Features

- **Schedule types** – `SetTimes` (specific daily times), `Interval` (periodic within a window), `OneShot` (single run), `Cron` (full cron expressions via [
  `Lyo.Schedule.Models.CronExpression`](../../Schedule/Lyo.Schedule.Models/README.md))
- **State persistence** – In-memory by default; pluggable `ISchedulerStateStore` (e.g. cache-backed) for cross-restart persistence
- **Logging and metrics** – Built-in `IMetrics` and `ILogger` integration
- **Background execution** – Actions run in background by default; optional action timeout

## Examples

### Usage

```csharp
using Lyo.Scheduler;
using Lyo.Schedule.Models;

// Add to DI (in-memory state store)
services.AddScheduler();

// Or with custom options
services.AddScheduler(options =>
{
    options.CheckIntervalMs = 5_000;
    options.ActionTimeout = TimeSpan.FromMinutes(10);
    options.RunInBackground = true;
});

// Or with a persistent state store (e.g. cache-backed)
services.AddScheduler(myStateStore);

// Add schedules and start
var scheduler = app.Services.GetRequiredService<ISchedulerService>();

scheduler.AddSchedule(
    "daily-report",
    "Daily Report",
    new ScheduleDefinition
    {
        Type = ScheduleType.SetTimes,
        Times = ["09:00", "17:00"],
        Timezone = "America/New_York"
    },
    async ct => await SendDailyReportAsync(ct));

await scheduler.StartAsync();
```

## `ISchedulerService` API

| Member                                                                                             | Purpose                                                                                                           |
|----------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| `AddSchedule(string id, string name, ScheduleDefinition def, Func<CancellationToken,Task> action)` | Register a scheduled action. Returns `true` when newly registered; `false` when an entry already exists for `id`. |
| `RemoveSchedule(string id)`                                                                        | Remove a schedule.                                                                                                |
| `TryGetSchedule(string id, out ScheduledAction action)` / `GetSchedules()`                         | Inspection.                                                                                                       |
| `StartAsync(CancellationToken)` / `StopAsync(CancellationToken)`                                   | Lifecycle.                                                                                                        |
| `TriggerNowAsync(string id, CancellationToken)`                                                    | Force-run an existing schedule out of cadence.                                                                    |
| `GetNextRun(string id, DateTimeOffset? from = null)`                                               | Next occurrence using `ScheduleCalculator` for all four schedule types.                                           |

`ScheduleCalculator` is the underlying evaluator: it dispatches to `GetNextRunSetTimes` / `GetNextRunInterval` / `GetNextRunOneShot` / `GetNextRunCron`. Cron evaluation first tries
`CronFormat.IncludeSeconds` (6 fields) and falls back to the standard 5-field format.

## Schedule types (Lyo.Schedule.Models)

- **SetTimes** – Run at specific times each day (e.g. 09:00, 17:00)
- **Interval** – Run every N minutes/hours within a daily window
- **OneShot** – Run once at a specific time
- **Cron** – Standard 5-field or 6-field cron (`"0 8 * * MON-FRI"`, `"*/30 * * * * *"`)

## Configuration

| Option          | Default | Description                                     |
|-----------------|---------|-------------------------------------------------|
| CheckIntervalMs | 10000   | Interval (ms) between checks for due schedules  |
| EnableMetrics   | true    | Enable metrics (when IMetrics registered)       |
| RunInBackground | true    | Run actions fire-and-forget vs await            |
| ActionTimeout   | 120 min | Max duration for each action; null = no timeout |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.DateAndTime` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Schedule.Models` — (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)