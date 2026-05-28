# Lyo.Job.Scheduler

Hosted `JobScheduler` that polls the Job API for enabled definitions, evaluates each schedule with `Lyo.Scheduler.ScheduleCalculator`, creates job runs via `IApiClient`, listens to
`IJobEventPublisher` for definition updates and run completions, fires triggers, schedules retries with exponential backoff, and trips/resets per-definition circuit breakers.

Designed for multi-instance deployment: run creation uses the `(JobScheduleId, ScheduledSlotUtc)` unique constraint to keep duplicate slot creations idempotent.

## Registration

`AddJobScheduler` registers the scheduler as a `BackgroundService`, exposes it as both `JobScheduler` and `IJobScheduler`, and wires `JobSchedulerOptions`. It also requires
`IApiClient`, `IFormatterService`, and `IJobEventPublisher` to be registered (typically via `AddMqJobEventPublisher` in `Lyo.Job.Postgres`).

```csharp
services.AddJobScheduler(new JobSchedulerOptions {
    ApiBaseUrl = "https://api.example.com",
    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"),
    DefinitionRefreshIntervalSeconds = 30,
    ScheduleCheckIntervalSeconds = 10,
    CreatedBy = "Scheduler",
});

// Or bind from the "JobScheduler" configuration section:
services.AddJobScheduler();
```

Run lifecycle is managed by the hosted-service infrastructure — there is no separate `StartAsync` to call.

## Configuration (appsettings.json)

`TimeZone` is a `TimeZoneInfo?`. When null, schedule times are evaluated as UTC. The configuration value is bound by `Microsoft.Extensions.Options.ConfigurationExtensions`, which
accepts the same IANA / Windows time-zone identifiers that `TimeZoneInfo.FindSystemTimeZoneById` accepts (for example `"America/New_York"`, `"UTC"`, or `"Eastern Standard Time"`).

```json
{
  "JobScheduler": {
    "ApiBaseUrl": "https://api.example.com",
    "TimeZone": "America/New_York",
    "DefinitionRefreshIntervalSeconds": 30,
    "ScheduleCheckIntervalSeconds": 10,
    "CreatedBy": "Scheduler"
  }
}
```

All `JobSchedulerOptions` properties:

| Property                           | Type            | Default       | Notes                                                       |
|------------------------------------|-----------------|---------------|-------------------------------------------------------------|
| `ApiBaseUrl`                       | `string`        | _required_    | Base URL of the Job API.                                    |
| `TimeZone`                         | `TimeZoneInfo?` | `null` (UTC)  | Applied to each schedule via `ScheduleDefinition.TimeZone`. |
| `DefinitionRefreshIntervalSeconds` | `int`           | `30`          | Definition refresh cadence.                                 |
| `ScheduleCheckIntervalSeconds`     | `int`           | `10`          | Schedule evaluation cadence.                                |
| `CreatedBy`                        | `string`        | `"Scheduler"` | Stamped on every run created by the scheduler.              |

## `IJobScheduler` surface

```csharp
public interface IJobScheduler
{
    bool IsRunning { get; }
    Task RefreshDefinitionsAsync(CancellationToken ct = default);
    Task CheckSchedulesAsync(CancellationToken ct = default);
}
```

`JobScheduler` also implements `Lyo.Health.IHealth` (`HealthCheckName = "job-scheduler"`) and reports `is_running`, `loaded_job_count`, `last_definitions_refresh_utc`, and
`last_schedule_check_utc` in its health metadata.

## Runtime flow

1. **Startup** — `ExecuteAsync` calls `IJobEventPublisher.SetupAsync`, subscribes to `Constants.Mq.JobDefinitionChangeKey` and the run-completion queue, runs an initial definition
   refresh + schedule check, then starts two `PeriodicTimer` loops on the configured cadences.
2. **Definition refresh** — `QueryReq` against `Job/Definition/Query` with includes for parameters, schedules, triggers (and trigger parameters), and parallel restrictions.
   Disabled definitions are skipped. For each enabled definition the scheduler also fetches the last run, last successful run, and last failed run via `Job/Run/Query` to build a
   `JobInfo`. The in-memory map is swapped atomically under a `SemaphoreSlim`.
3. **Schedule check** — For each `JobInfo` and each enabled `JobScheduleRes`: skips if the last run is `Queued`/`Running`; enforces enabled parallel restrictions; converts the
   schedule with `JobScheduleExtensions.ToScheduleDefinition()` and applies `JobSchedulerOptions.TimeZone`; calls `ScheduleCalculator.GetNextRun(...)` against the last successful
   start (or 10 years ago as a seed). If `nextDue <= UtcNow`, posts a `JobRunReq` with `JobScheduleId` and `ScheduledSlotUtc` set. Definition parameters and trigger parameters with
   `String`/`Json` types are interpolated through `{{...}}` templates resolved by `IFormatterService` against a context that includes the definition, last runs, schedule, trigger
   and trigger parameters. Conflicts (`ApiErrorCodes.Conflict`) returned by the API are treated as a benign "another instance already created this slot".
4. **Run completion (`job.run.complete`)** — Loads the run, updates `LastRun`/`LastSuccessfulRun`/`LastFailedRun`. Maintains an in-memory consecutive failure counter; on success
   the counter resets, on failure it increments and, when `CircuitBreakerThreshold` is reached, the scheduler PATCHes the definition to `Enabled=false` +
   `CircuitBreakerTrippedAt = UtcNow` and drops it from the in-memory cache (`JobMaintenanceService` in `Lyo.Job.Postgres` re-enables it after the cooldown). If `MaxRetryCount > 0`
   and the failed run is below the cap, a retry run is created with `RetryAttempt + 1` and `ScheduledSlotUtc = UtcNow + RetryBackoffSeconds * nextAttempt`.
5. **Triggers** — When `AllowTriggers` is set on the completed run, each enabled `JobTriggerRes` whose `JobResultKey` matches `JobResultValue` causes the target definition to be
   loaded and a new run created with `JobTriggerId` and `TriggeredByJobRunId` populated.
6. **Definition updates (`Constants.Mq.JobDefinitionChangeKey`)** — Triggers a per-definition refresh of the in-memory map. Disabled definitions are removed.

## Dependencies

*(Synchronized from `Lyo.Job.Scheduler.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                | Version |
|--------------------------------------------------------|---------|
| `Microsoft.Extensions.Hosting.Abstractions`            | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `[10,)` |

### Project references

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Api.Models`](../../Api/Lyo.Api.Models/README.md)
- [`Lyo.Formatter`](../../../Data/Formatter/Lyo.Formatter/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md)
- [`Lyo.Scheduler`](../../../Core/Scheduler/Lyo.Scheduler/README.md)
