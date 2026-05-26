# Lyo.Schedule.Models

DTO-only assembly that describes a schedule. Used by [`Lyo.Scheduler`](../../Scheduler/Lyo.Scheduler/README.md), `Lyo.Job.Postgres`, and any consumer that needs a
transport-friendly representation of "when does this run".

## Types

| Type                                    | Role                                                                                                                                                                                                                                                                                                                    |
|-----------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`ScheduleType`**                      | Enum: `SetTimes`, `Interval`, `OneShot`, `Cron`.                                                                                                                                                                                                                                                                        |
| **`ScheduleDefinition`**                | Immutable record bundling `Type`, `DayFlags`, `MonthFlags`, set-times, interval bounds, one-shot timestamp, and `CronExpression`. `Validate()` throws when fields don't match the chosen `ScheduleType`.                                                                                                                |
| **`ScheduleDefinitionBuilder`**         | Fluent builder — `SetTimes(...)`, `Interval(...)`, `OneShot(...)`, `SetCron(string)`, `OnDays(DayOfWeek)`, `InMonths(...)`. Calling a `Set*` mutator switches `Type` so callers can't combine inconsistent fields.                                                                                                      |
| **`CronExpression`** / **`CronFormat`** | Standalone cron parser/evaluator. `CronExpression.Parse(expression, CronFormat.Standard \| CronFormat.IncludeSeconds)` for 5-field (minute precision) or 6-field (second precision). `GetNextOccurrence(DateTimeOffset, TimeZoneInfo)` is the single evaluation entry point used by `Lyo.Scheduler.ScheduleCalculator`. |

## Quick start

```csharp
using Lyo.Schedule.Models;

var weekday = new ScheduleDefinitionBuilder()
    .SetTimes(new TimeOnly(8, 0), new TimeOnly(17, 0))
    .OnDays(DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday)
    .Build();

var cron = new ScheduleDefinitionBuilder()
    .SetCron("0 */15 * * * *")   // every 15 minutes (6-field, second precision)
    .Build();

cron.Validate(); // throws ArgumentException for invalid cron expressions

var next = CronExpression
    .Parse(cron.CronExpression!, CronFormat.IncludeSeconds)
    .GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
```

## Related projects

- [`Lyo.Common`](../../Common/Lyo.Common/README.md)
- [`Lyo.DateAndTime`](../../DateAndTime/Lyo.DateAndTime/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Scheduler`](../../Scheduler/Lyo.Scheduler/README.md) — consumer that evaluates these definitions.
