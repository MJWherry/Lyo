# Lyo.Schedule.Models

DTO-only assembly that describes a schedule. Used by [`Lyo.Scheduler`](../../Scheduler/Lyo.Scheduler/README.md), `Lyo.Job.Postgres`, and any consumer that needs a
transport-friendly representation of "when does this run".

## Examples

### Quick start

```csharp
using Lyo.Schedule.Models;

var weekday = new ScheduleDefinitionBuilder()
    .SetTimes(new TimeOnly(8, 0), new TimeOnly(17, 0))
    .OnDays(DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday)
    .Build();

var cron = new ScheduleDefinitionBuilder()
    .SetCron("0 */15 * * * *") // every 15 minutes (6-field, second precision)
    .Build();

cron.Validate(); // throws ArgumentException for invalid cron expressions

var next = CronExpression
    .Parse(cron.CronExpression!, CronFormat.IncludeSeconds)
    .GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
```

## Types

| Type | Role |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **`ScheduleType`** | Enum: `SetTimes`, `Interval`, `OneShot`, `Cron`. |
| **`ScheduleDefinition`** | Immutable record bundling `Type`, `DayFlags`, `MonthFlags`, set-times, interval bounds, one-shot timestamp, and `CronExpression`. `Validate()` throws when fields don't match the chosen `ScheduleType`. |
| **`ScheduleDefinitionBuilder`** | Fluent builder — `SetTimes(...)`, `Interval(...)`, `OneShot(...)`, `SetCron(string)`, `OnDays(DayOfWeek)`, `InMonths(...)`. Calling a `Set*` mutator switches `Type` so callers can't combine inconsistent fields. |
| **`CronExpression`** / **`CronFormat`** | Standalone cron parser/evaluator. `CronExpression.Parse(expression, CronFormat.Standard \ |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.DateAndTime` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)