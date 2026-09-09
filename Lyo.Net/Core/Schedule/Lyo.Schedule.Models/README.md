# Lyo.Schedule.Models

Schedule-only DTOs. [`Lyo.Scheduler`](../../Scheduler/Lyo.Scheduler/README.md), `Lyo.Job.Postgres`, and any other consumer that wants a transport-friendly answer to "when does this run" take a dependency here.

## Examples

### First steps

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

| Type | What it does |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ScheduleType` | Enum whose values are `SetTimes`, `Interval`, `OneShot`, and `Cron`. |
| `ScheduleDefinition` | Immutable record that holds `Type`, `DayFlags`, `MonthFlags`, set-times, interval bounds, a one-shot timestamp, and `CronExpression`. `Validate()` throws if the fields do not match the chosen `ScheduleType`. |
| `ScheduleDefinitionBuilder` | Fluent builder exposing `SetTimes(...)`, `Interval(...)`, `OneShot(...)`, `SetCron(string)`, `OnDays(DayOfWeek)`, and `InMonths(...)`. A `Set*` mutator also switches `Type`, so inconsistent fields cannot be combined. |
| `CronExpression` / `CronFormat` | Cron parser and evaluator that stand alone. Pass the expression plus a `CronFormat` into `CronExpression.Parse`. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.DateAndTime` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)