# Lyo.Scheduler

In-process scheduler service for executing actions at scheduled times. Supports SetTimes, Interval, and OneShot schedules with logging, metrics, and optional state persistence via
`ISchedulerStateStore`.

## Features

- **Schedule types** – SetTimes (specific daily times), Interval (periodic), OneShot (single run)
- **State persistence** – In-memory by default; pluggable `ISchedulerStateStore` (e.g. cache-backed) for cross-restart persistence
- **Logging and metrics** – Built-in `IMetrics` and `ILogger` integration
- **Background execution** – Actions run in background by default; optional action timeout

## Usage

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

## Schedule types (Lyo.Schedule.Models)

- **SetTimes** – Run at specific times each day (e.g. 09:00, 17:00)
- **Interval** – Run every N minutes/hours
- **OneShot** – Run once at a specific time

## Configuration

| Option          | Default | Description                                     |
|-----------------|---------|-------------------------------------------------|
| CheckIntervalMs | 10000   | Interval (ms) between checks for due schedules  |
| EnableMetrics   | true    | Enable metrics (when IMetrics registered)       |
| RunInBackground | true    | Run actions fire-and-forget vs await            |
| ActionTimeout   | 120 min | Max duration for each action; null = no timeout |

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Scheduler.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.DateAndTime`
- `Lyo.Exceptions`
- `Lyo.Metrics`
- `Lyo.Schedule.Models`

## Public API (generated)

Top-level `public` types in `*.cs` (*6*). Nested types and file-scoped namespaces may omit some entries.

- `InMemorySchedulerStateStore`
- `ISchedulerService`
- `ISchedulerStateStore`
- `SchedulerExtensions`
- `SchedulerOptions`
- `SchedulerService`

<!-- LYO_README_SYNC:END -->

