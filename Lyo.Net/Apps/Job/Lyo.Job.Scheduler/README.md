# Lyo.Job.Scheduler

A hosted `JobScheduler` polls the Job API for enabled definitions, evaluates schedules (with blackout calendars, misfire catch-up, and per-schedule time zones), creates job runs via `IApiClient`, listens to `IJobEventPublisher` for definition updates and run completions, fires triggers, schedules retries with linear or **exponential backoff**, aggregates batch parent progress, trips/resets per-definition circuit breakers, and publishes failure/circuit-breaker alerts.

Optional `JobWorkflowEngine` advances multi-step workflow runs when constituent job runs finish. Step runs are created with dispatch suppressed and only published after the run is linked to its workflow run step, so a fast worker cannot finish a step run before the engine knows which step it belongs to. `JobScheduler` completion-message failures on `job.run.complete` use a bounded counted requeue (max 3, like `QueueWorkerBase`) instead of requeueing forever, so a poison message cannot loop indefinitely. `JobWorkflowEngine` uses the same pattern (max 5).

Safe for multi-instance hosts: duplicate slot creates return the existing run. The due-slot cursor uses the later of last success and this schedule's last attempted slot so a failed scheduled run is not re-POSTed every check.

## Examples

### Register in DI

```csharp
// IMqService (RabbitMQ) + Job.Client publisher — not Lyo.Job.Postgres
services.AddJobClient(sp => sp.GetRequiredService<IApiClient>());
services.AddMqJobEventPublisherFromConfiguration(configuration);
// or: services.AddMqJobEventPublisher();

services.AddJobScheduler(new JobSchedulerOptions {
    ApiBaseUrl = "https://api.example.com",
    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"),
    DefinitionRefreshIntervalSeconds = 30,
    ScheduleCheckIntervalSeconds = 10,
    EnableMisfireCatchUp = true,
    MisfireLookbackMinutes = 1440,
    CreatedBy = "Scheduler",
});

// Or bind from the "JobScheduler" configuration section:
services.AddJobScheduler();

// Optional workflow orchestration (section JobWorkflowEngine):
services.AddJobWorkflowEngine(new JobWorkflowEngineOptions {
    ApiBaseUrl = "https://api.example.com",
    CreatedBy = "WorkflowEngine",
});
// services.AddJobWorkflowEngine();
```

### `IJobScheduler` methods

```csharp
public interface IJobScheduler
{
    bool IsRunning { get; }
    Task RefreshDefinitionsAsync(CancellationToken ct = default);
    Task CheckSchedulesAsync(CancellationToken ct = default);
}
```

## Registration

Needs `IApiClient`, `IFormatterService`, and `IJobEventPublisher`. For scheduler/worker hosts register the non-EF publisher via `Lyo.Job.Client.AddMqJobEventPublisher()` / `AddMqJobEventPublisherFromConfiguration()` (`IMqService` + optional `IJobClient`). Do **not** use `Lyo.Job.Postgres.AddMqJobEventPublisher*`. That path pulls EF. `IMqService` and `IMetrics` are optional for the scheduler itself (MQ is required for the publisher).

## `JobSchedulerOptions` (`JobSchedulerOptions.SectionName` = `"JobScheduler"`)

| Property | Type | Default | Notes |
| ---------------------------------- | --------------- | ------------- | ---------------------------------------------------------- |
| `ApiBaseUrl` | `string` | _required_ | Base URL of the Job API. |
| `TimeZone` | `TimeZoneInfo?` | `null` (UTC) | Fallback zone when a schedule has no `TimeZoneId`. |
| `DefinitionRefreshIntervalSeconds` | `int` | `30` | How often definitions + calendars refresh. |
| `ScheduleCheckIntervalSeconds` | `int` | `10` | How often schedules are evaluated. |
| `CreatedBy` | `string` | `"Scheduler"` | Written onto runs the scheduler creates. |
| `EnableMisfireCatchUp` | `bool` | `true` | Global default; a schedule's `MisfirePolicy` can override. |
| `MisfireLookbackMinutes` | `int` | `1440` | Oldest missed slots still eligible for `RunOnce` catch-up. |

`TimeZone` binds from configuration using Windows / IANA identifiers (e.g. `"UTC"`, `"America/New_York"`).

## `JobWorkflowEngineOptions` (`JobWorkflowEngineOptions.SectionName` = `"JobWorkflowEngine"`)

| Property | Default | Notes |
| ------------ | ------------------ | -------------------------------- |
| `ApiBaseUrl` | _required_ | Job API base URL. |
| `CreatedBy` | `"WorkflowEngine"` | Written onto workflow-run steps. |

## Metrics (`job.scheduler.*`)

| Metric | Description |
| --------------------------------------- | ------------------------------- |
| `job.scheduler.definitions.loaded` | Count after a refresh |
| `job.scheduler.refresh.duration` | Timer for refresh |
| `job.scheduler.refresh.error` | Failed refreshes |
| `job.scheduler.check.duration` | Timer for schedule checks |
| `job.scheduler.check.error` | Failed checks |
| `job.scheduler.runs.created` | Created runs |
| `job.scheduler.runs.create.failed` | Failed API creates |
| `job.scheduler.slot.conflicts` | Benign duplicate-slot conflicts |
| `job.scheduler.triggers.fired` | Runs driven by triggers |
| `job.scheduler.retries.scheduled` | Retry runs after failure |
| `job.scheduler.circuit_breaker.tripped` | Disabled definitions |
| `job.scheduler.misfires.caught_up` | `RunOnce` misfire runs |
| `job.scheduler.misfires.skipped` | Skipped missed slots |

## What happens at runtime

```mermaid
flowchart TB
    start[Startup] --> setup[EventPublisher.SetupAsync]
    setup --> refresh[Refresh definitions + calendars]
    refresh --> loops[Periodic refresh + schedule check]
    loops --> eval[Evaluate next slot]
    eval --> cal{Blackout calendar?}
    cal -->|Skip| skip[Skip slot]
    cal -->|Defer| defer[Shift to window end]
    cal -->|OK| create[POST JobRunReq]
    create --> mq[PublishRunCreated priority]
    finish[job.run.complete] --> retry{Failure + retries?}
    retry -->|yes| backoff[JobRetryBackoff delay]
    retry -->|no| trig[Fire triggers]
    finish --> batch[Update parent batch progress]
    finish --> alert[PublishAlert on failure/CB]
```

1. **Startup**. `SetupAsync`, subscribe to definition-change and run-completion queues, initial refresh + misfire pass, start periodic timers.
2. **Definition refresh**. Enabled definitions load with schedules, triggers, parallel restrictions; last-run snapshots come in one batch via
 `POST Job/Definition/LatestRuns` (falls back to per-definition queries when the endpoint is unavailable); caches them as `JobInfo`.
3. **Calendar refresh**. `JobBlackoutCalendar` + windows load for blackout evaluation (`Lyo.Job.Models.JobBlackoutCalendarEvaluator`: weekday, dated range, calendar day-of-month, or holiday slug; `Skip` or `Defer`).
4. **Schedule check**. Skips when a run is already `Running`/`Queued`; respects parallel restrictions; applies misfire policy; creates runs with `ScheduledSlotUtc` for idempotency; skips a slot this process already created or that last-run/last-failed already owns, using the later of last success and this schedule's last attempted slot as the cursor.
5. **Misfire catch-up**. When `EnableMisfireCatchUp` and schedule `MisfirePolicy == RunOnce`, creates one run for the most recent missed slot within lookback.
6. **Run completion**. Updates cached last-run pointers; circuit breaker on consecutive failures; retries via `JobRetryBackoff.ComputeBackoffSeconds` (`Exponential` or `Linear`);
 batch parent progress; failure alerts when `AlertOnFailure` and `AlertAfterConsecutiveFailures` threshold is met. Timeouts (`JobRunResult.Timeout`, published by the maintenance
 dead-job scan) count as failures for retry and circuit-breaker purposes.
7. **Triggers**. When `AllowTriggers`, matching trigger definitions spawn new runs.
8. **Definition updates**. Per-definition cache refresh; disabled definitions removed; a 404 evicts the definition from the cache (deleted definitions stop producing doomed runs).

## Templates on string parameters

When creating a run, Formatter and String parameter values (schedule, definition, and trigger) are formatted with `IFormatterService` against `Schedule`, `Definition`, last-run snapshots, and related objects. Pick type `formatter` in the catalog for Guid/DateTime templates (`{-}`, `{DateTime.UtcNow}`). Write `{Definition.Name}` — unresolved tokens such as `{client.contact.emailAddress}` stay in the value until the worker `AddContext`s them. Xml and Json values are not formatted. Legacy `{{Definition.Name}}` is unwrapped to `{Definition.Name}` in the scheduler only so existing schedules keep working; `{{` is not the placeholder standard (SmartFormat uses it as a literal brace).

## Backoff on retry

A failed run with retries remaining produces exactly **one** dispatch after the computed delay. The retry run is always created immediately (as `Queued`), and one of three dispatch
paths applies:

- **Delayed MQ available** (`IMqService is IDelayedMqService`): the create sets `SuppressDispatch = true` and the scheduler publishes a delayed `QueueMessageEnvelope` to the worker
 queue; the envelope is the sole dispatch.
- **No delayed MQ**: the create sets `ScheduledSlotUtc` to the due time, which suppresses the immediate publish; the maintenance service's stuck-queued recovery dispatches the run
 once the slot comes due.
- **No backoff** (`backoffSeconds == 0`): the API's immediate publish dispatches as usual.

If a delayed envelope is lost (scheduler crash, broker restart), the stuck-queued recovery also re-publishes it once the run has sat untouched past the threshold. The run is never
silently stranded. Retry creation carries an idempotency key (`retry:{failedRunId}:{attempt}`), so duplicate completion deliveries or two scheduler instances processing the same
completion cannot create duplicate retries. Trigger firing is deduplicated the same way (`trigger:{triggerId}:{completedRunId}`).

## Completion across instances

`job.run.complete` is a competing-consumer queue: each completion is processed by exactly one scheduler instance. Correctness across instances relies on idempotency keys (triggers, retries) and the `(JobScheduleId, ScheduledSlotUtc)` unique constraint (scheduled slots), not on instance affinity. Completion processing takes the definition lock before mutating the in-memory `JobInfo` cache, so concurrent refreshes cannot interleave with circuit-breaker counter updates. A 404 from `GET Job/Run/{id}` is acked (run not found). Other processing failures (including HTTP 500) are retried with a counted envelope up to 3 times, then dropped. Never nack-requeued forever.

## Restrictions on parallelism

When a definition has `JobParallelRestriction` rows, `CheckSchedulesAsync` skips creating a run if any restricted definition (cross-definition or same link) already has a `Running` or `Queued` run. This complements per-definition `MaxConcurrentRuns` enforced in `JobService`.

## `IJobScheduler` methods

`JobScheduler` implements `Lyo.Health.IHealth` (`HealthCheckName = "job-scheduler"`) with `loaded_job_count`, `is_running`, `last_schedule_check_utc`, and
`last_definitions_refresh_utc`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.MessageQueue` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)