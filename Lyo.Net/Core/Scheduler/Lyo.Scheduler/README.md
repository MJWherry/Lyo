# Lyo.Scheduler

In-process scheduler that runs actions at scheduled times. Supports **SetTimes**, **Interval**, **OneShot**, and **Cron** schedules (5- or 6-field expressions) with logging, metrics, and optional state persistence via `ISchedulerStateStore`.

## Features

- **Schedule types.** `Interval` (periodic inside a window), `SetTimes` (explicit daily times), `OneShot` (single run), `Cron` (5- or 6-field expressions via [`Lyo.Schedule.Models.CronExpression`](../../Schedule/Lyo.Schedule.Models/README.md)).
- **State persistence.** In-memory by default. Pluggable `ISchedulerStateStore` (e.g. cache-backed) so state survives a restart.
- **Logging and metrics.** `ILogger` and `IMetrics`.
- **Background execution.** Actions run in the background by default. Optional action timeout.

## Examples

### How to use it

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

## `ISchedulerService` methods

| Member | What it does |
| -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `AddSchedule(string id, string name, ScheduleDefinition def, Func<CancellationToken,Task> action)` | Add a scheduled action. `true` when the id is new; `false` when that `id` is already registered. |
| `RemoveSchedule(string id)` | Drop a schedule. |
| `TryGetSchedule(string id, out ScheduledAction action)` / `GetSchedules()` | Inspection. |
| `StartAsync(CancellationToken)` / `StopAsync(CancellationToken)` | Lifecycle. |
| `TriggerNowAsync(string id, CancellationToken)` | Run an existing schedule immediately, ignoring cadence. |
| `GetNextRun(string id, DateTimeOffset? from = null)` | `ScheduleCalculator` yields the next occurrence for all four schedule types. |

`ScheduleCalculator` is the evaluator underneath: it dispatches to `GetNextRunSetTimes` / `GetNextRunInterval` / `GetNextRunOneShot` / `GetNextRunCron`. Cron evaluation first tries
`CronFormat.IncludeSeconds` (6 fields) and falls back to the standard 5-field format.

## Schedule kinds (Lyo.Schedule.Models)

- **SetTimes.** Fire at specific times each day (e.g. 09:00, 17:00).
- **Interval.** Fire every N minutes/hours inside a daily window.
- **OneShot.** Fire once at a specific time.
- **Cron.** 5-field or 6-field cron (`"0 8 * * MON-FRI"`, `"*/30 * * * * *"`).

## Options

| Option | Default | What it controls |
| --------------- | ------- | ---------------------------------------------- |
| CheckIntervalMs | 10000 | How often (ms) to look for due schedules |
| EnableMetrics | true | Turn metrics on (when IMetrics is registered) |
| RunInBackground | true | Fire-and-forget actions vs await them |
| ActionTimeout | 120 min | Longest each action may run; null = no timeout |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.DateAndTime` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Schedule.Models` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)