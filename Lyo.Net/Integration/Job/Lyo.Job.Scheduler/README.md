# Lyo.Job.Scheduler

Polls job definitions via the Job API, evaluates schedules using `Lyo.DateAndTime`, creates job runs via `IApiClient`, and processes completed runs (triggers) from RabbitMQ.

## Usage

```csharp
services.AddJobScheduler(new JobSchedulerOptions
{
    ApiBaseUrl = "https://api.example.com",
    TimezoneState = USState.PA,
    DefinitionRefreshIntervalSeconds = 30,
    ScheduleCheckIntervalSeconds = 10,
});

// Or from configuration
services.AddJobScheduler(); // binds "JobScheduler" section

// Start when hosting
var scheduler = app.Services.GetRequiredService<JobScheduler>();
await scheduler.StartAsync();
```

## Configuration (appsettings.json)

```json
{
  "JobScheduler": {
    "ApiBaseUrl": "https://api.example.com",
    "TimezoneState": "PA",
    "DefinitionRefreshIntervalSeconds": 30,
    "ScheduleCheckIntervalSeconds": 10,
    "CreatedBy": "Scheduler"
  }
}
```

## Flow

1. **Definition refresh** – Periodically loads enabled job definitions (with parameters, schedules, triggers) from the API.
2. **Schedule check** – Evaluates each definition’s schedules via `DateAndTime.IsPastDue` (with MonthFlags) and creates job runs when due.
3. **Job run completion** – Subscribes to `job.run.complete`; on message, fetches the run, updates last-run state, and processes triggers when criteria match.

## Dependencies

*(Synchronized from `Lyo.Job.Scheduler.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions`  | `[10,)` |

### Project references

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Api.Models`](../../Api/Lyo.Api.Models/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.DateAndTime`](../../../Core/DateAndTime/Lyo.DateAndTime/README.md)
- [`Lyo.Formatter`](../../../Data/Formatter/Lyo.Formatter/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
- [`Lyo.MessageQueue.RabbitMq`](../../../Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/README.md)
- [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md)