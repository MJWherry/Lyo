# Lyo.Schedule.Web.Components

Blazor component(s) for interactively building and previewing [`Lyo.Schedule.Models.ScheduleDefinition`](../Lyo.Schedule.Models/README.md) values.

## Components

| Component | What it does |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ScheduleWorkbench` | Interactive editor for `ScheduleDefinition`. Chooses the `ScheduleType` (`SetTimes` / `Interval` / `OneShot` / `Cron`), shows the matching inputs for that type (day/month flags, time pickers, interval bounds, one-shot timestamp, cron string with a `CronFormat` toggle), validates with `ScheduleDefinition.Validate()`, and previews the next N occurrences in the host's `TimeZoneInfo`. |

## Wiring into a host

`<ScheduleWorkbench />` is a pure component. Pass a `ScheduleDefinition?` initial value as a parameter and take the updated value from the value-changed callback. MudBlazor layout primitives come from [`Lyo.Web.Components`](../../../Apps/Web/Lyo.Web.Components/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
- `Lyo.Web.Primitives` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)