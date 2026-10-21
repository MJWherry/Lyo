# Lyo.Schedule.Models

Schedule-only DTOs. [`Lyo.Scheduler`](../../Scheduler/Lyo.Scheduler/README.md), `Lyo.Job.Postgres`, and any other consumer that wants a transport-friendly answer to "when does this
run" take a dependency here.

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

| Type                                         | What it does                                                                                                                                                                                                                                                                                       |
|----------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ScheduleType`                               | Enum whose values are `SetTimes`, `Interval`, `OneShot`, and `Cron`.                                                                                                                                                                                                                               |
| `ScheduleDefinition`                         | Immutable record that holds `Type`, `DayFlags`, `MonthFlags`, set-times, interval bounds, a one-shot timestamp, and `CronExpression`. `Validate()` throws if the fields do not match the chosen `ScheduleType`.                                                                                    |
| `ScheduleDefinitionBuilder`                  | Fluent builder exposing `SetTimes(...)`, `Interval(...)`, `OneShot(...)`, `SetCron(string)`, `OnDays(DayOfWeek)`, and `InMonths(...)`. A `Set*` mutator also switches `Type`, so inconsistent fields cannot be combined.                                                                           |
| `CronExpression` / `CronFormat`              | Cron parser and evaluator that stand alone. Pass the expression plus a `CronFormat` into `CronExpression.Parse`.                                                                                                                                                                                   |
| `TimeFrame`                                  | One concrete UTC span (or a point, when start equals end) with a key, title, optional description, hue (0–359), category, and `EntityRef` list. The calendar stores refs and does not load the entities.                                                                                           |
| `Calendar` / `CalendarDay` / `CalendarEntry` | A placed civil range. `ForMonth` pads to full weeks (Sunday start). `FromTimeFrames` drops spans outside the range and lists a midnight-crossing span on every day it touches. `WithTimeZone` keeps the UTC instants and rebuckets civil days. `Merge` unions entries that share a range and zone. |
| `CalendarClock`                              | Formats a UTC instant in a zone as `22:00` or `10:00 PM`, and the zone offset as `UTC-04:00`. The hour cycle changes the label only.                                                                                                                                                               |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.DateAndTime` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)