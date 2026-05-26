# Lyo.Schedule.Web.Components

Blazor component(s) for building and previewing [`Lyo.Schedule.Models.ScheduleDefinition`](../Lyo.Schedule.Models/README.md) values interactively.

## Components

| Component               | Purpose                                                                                                                                                                                                                                                                                                                                                                                           |
|-------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`ScheduleWorkbench`** | Interactive editor for `ScheduleDefinition`. Picks the `ScheduleType` (`SetTimes` / `Interval` / `OneShot` / `Cron`), exposes the right input set for each type (day/month flags, time pickers, interval bounds, one-shot timestamp, cron string with `CronFormat` toggle), validates via `ScheduleDefinition.Validate()`, and previews the next N occurrences against the host's `TimeZoneInfo`. |

## Host integration

`<ScheduleWorkbench />` is a pure component — pass a `ScheduleDefinition?` initial value via parameter and receive the updated value through the value-changed callback. MudBlazor
layout primitives are inherited from [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md).

## Related projects

- [`Lyo.Schedule.Models`](../Lyo.Schedule.Models/README.md)
- [`Lyo.Scheduler`](../../Scheduler/Lyo.Scheduler/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
