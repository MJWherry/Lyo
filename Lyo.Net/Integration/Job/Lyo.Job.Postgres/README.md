# Lyo.Job.Postgres

PostgreSQL persistence and minimal-API host for the Lyo job-management subsystem. Wraps EF Core, the Lyo CRUD/QueryConcrete stack, hand-rolled `JobLyoMapper` (`ILyoMapper`),
optional parameter encryption, audit recording, and `IJobEventPublisher` so a host can drop in a complete job service: definitions, parameters, schedules, triggers, calendars,
workflows, worker registry, runs, batch children, run parameters, run results, run logs, and stats.

## Drop-and-play registration

`AddPostgresJobManagement` registers the `JobContext` factory, optional auto-migrations, the Lyo CRUD services, `JobService`, `JobLyoMapper` as `ILyoMapper`, and a default no-op
`IJobEventPublisher` (`NullJobEventPublisher`). Hosts that also use Mapster for non-job types should replace `ILyoMapper` with `CompositeLyoMapper(job, mapsterFallback)`. Replace
the publisher with `AddMqJobEventPublisher()` once you have an `IMqService` available (API hosts with a job database only). **Scheduler and worker hosts must not use this package's
publisher** — use `Lyo.Job.Client.AddMqJobEventPublisher*` (`IMqService` + Job.Client) instead.

```csharp
services.AddLyoQueryServices();
services.AddFusionCache(...);            // or AddLocalCache(...)

services.AddPostgresJobManagement(o => {
    o.ConnectionString = connectionString;
    o.EnableAutoMigrations = true;
});

// Optional: encrypt sensitive parameter values at rest
services.AddJobParameterEncryption(keyName: "job-parameters");

// After IMqService is registered (e.g. AddRabbitMq):
services.AddMqJobEventPublisher();

// Dead-job watchdog, circuit-breaker reset, retention purge, SLA checks, worker pruning:
services.AddJobMaintenanceService(o => {
    o.DefaultRetentionDays = 90;
    o.PurgeBatchSize = 500;
    o.WorkerInstanceStaleMinutes = 5;
});

// Or bind maintenance from configuration (section JobMaintenance):
services.AddJobMaintenanceServiceFromConfiguration(configuration);

var app = builder.Build();
app.BuildJobGroup();
```

When `IAuditRecorder` is registered, CRUD hooks record `JobDefinition.*`, `JobRun.*`, and related entity events via `JobAuditHelper`.

## Configuration

### `PostgresJobOptions` (`PostgresJobOptions.SectionName` = `"PostgresJob"`)

| Property               | Default | Notes                                                           |
|------------------------|---------|-----------------------------------------------------------------|
| `ConnectionString`     | `""`    | Npgsql connection string. Required.                             |
| `EnableAutoMigrations` | `false` | When `true`, `Lyo.Postgres` runs pending migrations on startup. |

`Schema` is fixed to `job`. EF migrations history lives in `job.__EFMigrationsHistory`.

### `JobMaintenanceOptions` (`JobMaintenanceOptions.SectionName` = `"JobMaintenance"`)

| Property                       | Default | Notes                                                                                    |
|--------------------------------|---------|------------------------------------------------------------------------------------------|
| `CheckIntervalSeconds`         | `30`    | Tick cadence for all maintenance tasks.                                                  |
| `DefaultRetentionDays`         | `0`     | Global retention for finished runs; `0` = keep forever.                                  |
| `PurgeBatchSize`               | `500`   | Max runs deleted per tick.                                                               |
| `WorkerInstanceStaleMinutes`   | `5`     | Prune worker registry rows without recent heartbeat.                                     |
| `QueuedRunRedispatchMinutes`   | `10`    | Re-publish dispatch for due `Queued` runs untouched this long (`0` disables). See below. |
| `QueuedRunRedispatchBatchSize` | `200`   | Max stuck queued runs re-published per tick.                                             |

Per-definition `RetentionDays` overrides the global default when &gt; 0.

## DI extension methods

| Method                                                     | Purpose                                                                                       |
|------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| `AddJobDbContext(connectionString)`                        | Scoped `JobContext` (legacy).                                                                 |
| `AddJobDbContextFactory(...)` / `FromConfiguration(...)`   | `IDbContextFactory<JobContext>` + migrations.                                                 |
| `AddPostgresJobManagement(...)` / `FromConfiguration(...)` | Full job service: factory + CRUD + `JobService` + `NullJobEventPublisher`.                    |
| `AddJobMaintenanceService(...)` / `FromConfiguration(...)` | `JobMaintenanceService` hosted background service.                                            |
| `AddMqJobEventPublisher()`                                 | `MqJobEventPublisher` + `JobEventPublisherStartupService` (calls `SetupAsync` on host start). |
| `AddJobParameterEncryption(keyName)`                       | `JobParameterEncryptionService` + `IJobParameterEncryptionService`.                           |

## Run state machine

All lifecycle transitions are compare-and-swap updates (`ExecuteUpdateAsync` with the expected state in the `WHERE` clause), so duplicate MQ deliveries and races between workers,
the API, and maintenance resolve deterministically. Future endpoints that mutate `JobRun.State` must keep this CAS discipline.

| Transition                                    | Performed by                                                           | Guard                                                                                                                                                                   |
|-----------------------------------------------|------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| *(create)* → `Queued`                         | `CreateJobRun` (API, scheduler, workflow engine, rerun, child fan-out) | Advisory lock per definition; idempotency key; rate/concurrency limits.                                                                                                 |
| `Queued` → `Running`                          | `StartedJobRun` (worker picked up the dispatch message)                | CAS on `State == Queued` — a redelivered dispatch, a second worker, or a start after a queued-run cancel is rejected with 400 (no-requeue), never double-executed.      |
| `Queued` → `Finished`/`Cancelled`             | `CancelJobRun` (user cancels before a worker starts)                   | CAS on `State == Queued`; publishes both `RunCancelled` and `RunFinished`. If a worker won the race, falls through to the `Cancelling` path.                            |
| `Running` → `Cancelling`                      | `CancelJobRun` (user cancels an active run)                            | Patch; the worker confirms via `FinishedJobRun`.                                                                                                                        |
| `Running` → `Queued`                          | `RequeueJobRun` (worker host shutdown hand-back)                       | CAS on `State == Running`; `Cancelling` is intentionally rejected so a pending user cancel is not forgotten by a restart. Clears `StartedTimestamp`/`LastHeartbeatUtc`. |
| `Running`/`Cancelling` → `Finished`           | `FinishedJobRun` (worker reports outcome)                              | State check; stamps result, duration SLA.                                                                                                                               |
| `Running`/`Cancelling` → `Finished`/`Timeout` | `JobMaintenanceService` dead-job scan                                  | Heartbeat older than `TimeoutMinutes`; publishes `RunFinished` so retries/triggers/circuit breaker still fire.                                                          |

### Dispatch suppression

`CreateJobRun` skips the immediate `RunCreated` publish when `JobRunReq.SuppressDispatch` is set **or** `ScheduledSlotUtc` is in the future (metric
`job.service.run.dispatch.deferred`). The caller then owns dispatch: the scheduler's delayed-MQ envelope delivers backoff retries, and the workflow engine publishes step runs only
after linking them to their run step. The maintenance service's stuck-queued recovery (below) is the safety net if the owner crashes before publishing. With suppression requested,
creation succeeds even while MQ is disconnected.

### Encryption flow

Parameter values matching an encrypted definition parameter are encrypted at rest (`EncryptedValue`) and masked (`***`) by `JobLyoMapper` on every API response. `StartedJobRun` is
the single worker-trusted exception: it decrypts parameter values server-side in the response so the executing worker receives real values. Rerun and child-run creation build their
requests from the stored entity (not the masked API response), so ciphertext survives cloning and masked `***` strings are never persisted as real values.

## Production hardening in `JobService`

| Feature                   | Behavior                                                                                                                                                                                                    |
|---------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Priority**              | Run creation inherits `JobDefinition.Priority`; MQ publish passes priority to `x-max-priority` queues.                                                                                                      |
| **Idempotency**           | When `JobRunReq.IdempotencyKey` is set, returns the existing run instead of inserting a duplicate (`ix_job_run_idempotency_key_unique`).                                                                    |
| **Rate limiting**         | Rejects create when hourly run count ≥ `MaxRunsPerHour` (metric `job.service.run.create.rejected`).                                                                                                         |
| **Concurrency**           | Enforces `MaxConcurrentRuns` (Queued + Running).                                                                                                                                                            |
| **SLA**                   | On start: breaches `MustStartByMinutes` → `SlaBreached=true` + alert. On finish: breaches `ExpectedDurationMinutes` → same.                                                                                 |
| **Audit**                 | Stamps `DefinitionAuditVersion` from `JobDefinition.DefinitionVersion` on each new run; definition updates bump version.                                                                                    |
| **Tracing**               | `JobTracing.StartCreateRun` / `StartRun` / `FinishRun` spans around lifecycle transitions.                                                                                                                  |
| **Alerting**              | Publishes `PublishAlertAsync` for SLA breaches; scheduler/maintenance publish failure/dead-job alerts.                                                                                                      |
| **Batch jobs**            | `POST Job/Run/{parentId}/Children` creates fan-out child runs; scheduler aggregates parent progress when children finish.                                                                                   |
| **Encryption**            | Parameter create/update encrypts via `IJobParameterEncryptionService`; API masks encrypted values; `StartedJobRun` decrypts server-side for the executing worker (see [Encryption flow](#encryption-flow)). |
| **Dry run**               | When `JobRunReq.DryRun == true`, validates parameters and returns a synthetic `JobRunRes` without DB insert or MQ publish.                                                                                  |
| **Parameter validation**  | `ValidateRunParametersAsync` enforces definition schema: required, regex, min/max length, pipe-delimited `AllowedValues`.                                                                                   |
| **Race-safe concurrency** | `pg_advisory_xact_lock` per definition serializes create + `MaxConcurrentRuns` / rate-limit checks inside a transaction.                                                                                    |

Slot idempotency (`JobScheduleId` + `ScheduledSlotUtc` unique index) remains the multi-scheduler guard for scheduled runs.

## Metrics (`job.service.*`, `job.maintenance.*`, `job.sla.*`)

### `job.service.*`

| Metric                              | When recorded                                                                                                                                                                     |
|-------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `job.service.run.created`           | Successful insert                                                                                                                                                                 |
| `job.service.run.create.rejected`   | Validation, concurrency, rate limit (tag `reason`: `invalid_parameters`, `mq_disconnected`, `definition_not_found`, `max_runs_per_hour`, `max_concurrent_runs`, `duplicate_slot`) |
| `job.service.run.dispatch.deferred` | Create with suppressed/deferred dispatch (no immediate `RunCreated` publish)                                                                                                      |
| `job.service.run.started`           | `StartedJobRun`                                                                                                                                                                   |
| `job.service.run.start.rejected`    | Started CAS guard rejected a non-`Queued` run (duplicate delivery, cancelled, finished)                                                                                           |
| `job.service.run.requeued`          | `RequeueJobRun` (worker shutdown hand-back)                                                                                                                                       |
| `job.service.run.finished`          | `FinishedJobRun`                                                                                                                                                                  |
| `job.service.run.cancelled`         | `CancelJobRun`                                                                                                                                                                    |
| `job.service.run.rerun`             | `RerunJob`                                                                                                                                                                        |
| `job.service.run.duration`          | Finished run wall time                                                                                                                                                            |
| `job.service.run.queue_latency`     | Created → started                                                                                                                                                                 |

### `job.maintenance.*`

| Metric                                    | When recorded                          |
|-------------------------------------------|----------------------------------------|
| `job.maintenance.tick.duration`           | Each maintenance loop                  |
| `job.maintenance.tick.error`              | Tick exception                         |
| `job.maintenance.dead_jobs.failed`        | Heartbeat timeout → Finished/Timeout   |
| `job.maintenance.circuit_breakers.reset`  | Cooldown elapsed → re-enabled          |
| `job.maintenance.runs.purged`             | Retention batch delete                 |
| `job.maintenance.runs.redispatched`       | Stuck queued run dispatch re-published |
| `job.maintenance.worker_instances.pruned` | Stale registry cleanup                 |

### `job.sla.*`

| Metric           | When recorded                                                              |
|------------------|----------------------------------------------------------------------------|
| `job.sla.breach` | Queued past `MustStartByMinutes` or running past `ExpectedDurationMinutes` |

## `JobMaintenanceService`

`BackgroundService` (`IHealth`: `job-maintenance`) ticking every `CheckIntervalSeconds`:

1. **Dead jobs** — `Running`/`Cancelling` runs past `TimeoutMinutes` → `Finished`/`Timeout` + optional `DeadJob` alert. After commit, publishes `RunFinished` for each timed-out run
   so the scheduler's retry/trigger/circuit-breaker accounting fires (a late worker finish for the same run is rejected by the state check and dropped by the worker as terminal).
2. **Circuit breaker reset** — re-enables definitions after `CircuitBreakerResetMinutes`; publishes `DefinitionUpdated` so scheduler caches refresh promptly.
3. **Stuck queued redispatch** — re-publishes `RunCreated` for due `Queued` runs untouched for `QueuedRunRedispatchMinutes` (lost publishes, delayed retries whose slot came due,
   crashed suppressed-dispatch owners). Bumps `UpdatedTimestamp` so a stuck run retries once per threshold window; duplicate deliveries are harmless because `StartedJobRun` only
   transitions `Queued -> Running` once.
4. **Retention purge** — deletes finished runs older than effective retention in `PurgeBatchSize` batches. Detaches FK references from surviving rows first: `ReRanFromJobRunId`/
   `TriggeredByJobRunId`/`ParentJobRunId` on related runs and `JobWorkflowRunStep.JobRunId` (workflow history is preserved with the run reference nulled), so purging
   workflow-created runs or parents with surviving children cannot violate FKs and wedge the purge.
5. **Worker pruning** — removes stale `JobWorkerInstance` rows.
6. **SLA breach scan** — marks `SlaBreached` and increments `job.sla.breach` for overdue queued/running jobs. Alerts for SLA breaches are published by `JobService` on start/finish
   transitions, not by this background scan.

```mermaid
flowchart LR
    tick[Maintenance tick] --> dead[Fail dead jobs]
    tick --> cb[Reset circuit breakers]
    tick --> redisp[Redispatch stuck queued runs]
    tick --> ret[Purge by retention]
    tick --> wrk[Prune worker instances]
    tick --> sla[Detect SLA breaches]
    dead --> alert[PublishAlertAsync]
    dead --> fin[PublishRunFinishedAsync]
    cb --> defupd[PublishDefinitionUpdatedAsync]
```

## Endpoint matrix (`BuildJobGroup`)

Tag `"Job"`. CRUD + export on definitions; runs expose Query/Get/Delete/DeleteBulk + export.

| Route prefix                                               | Entity             | Notes                                                                                |
|------------------------------------------------------------|--------------------|--------------------------------------------------------------------------------------|
| `Job/Definition`                                           | `JobDefinition`    | Version bump + audit on update                                                       |
| `Job/Definition/Parameter`                                 | `JobParameter`     | Encryption on write                                                                  |
| `Job/Schedule`                                             | `JobSchedule`      | Misfire policy, calendar link, cron                                                  |
| `Job/Triggers`                                             | `JobTrigger`       |                                                                                      |
| `Job/BlackoutCalendar`, `Job/BlackoutCalendar/Window`      | Blackout calendars |                                                                                      |
| `Job/Workflow`, `Job/Workflow/Step`, `Job/Workflow/Run`, … | Workflows          |                                                                                      |
| `Job/WorkerInstance`                                       | Worker registry    | Created by workers                                                                   |
| `Job/Run`                                                  | `JobRun`           | Progress, SLA, idempotency fields                                                    |
| `Job/Run/{id}/Children`                                    | Batch fan-out      | `JobCreateChildRunsReq`                                                              |
| `POST Job/Definition/LatestRuns`                           | Batch latest-runs  | Latest / latest-successful / latest-failed run per definition id (scheduler refresh) |
| `GET Job/Definition/{id}/Stats`                            | Stats projection   | Rolling window aggregates                                                            |

Lifecycle routes (`RunStarted`, `RunFinished`, `RunRequeue`, `RunHeartbeat`, `RunLog`, …) are mapped alongside CRUD for scheduler/worker hosts.

## Event publishers (`Events/`)

- **`NullJobEventPublisher`** — default; `IsConnected() == false`.
- **`MqJobEventPublisher`** — creates queues/exchange bindings; publishes run events with optional priority; routes alerts to `job.notifications.alert`. Resolves worker types from
  EF when `JobContext` is registered. For scheduler/worker hosts use `Lyo.Job.Client.MqJobEventPublisher` instead.

## Design-time migrations

Set `JOB_CONNECTION_STRING` and run:

```bash
export JOB_CONNECTION_STRING="Host=localhost;Database=postgres;Username=postgres;Password=password"
dotnet ef migrations add YourMigrationName --project Lyo.Job.Postgres
```

Recent migrations of note:

- `WidenJobDefinitionWorkerType` — widens `job_definition.worker_type` from 7 to 50 characters, matching `job_worker_instance.worker_type` (worker type names longer than 7
  characters were previously truncated/rejected on the definition side only).

## Dependencies

*(Synchronized from `Lyo.Job.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Mapster`                                   | `[10,)` |
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.Api`](../../Api/Lyo.Api/README.md)
- [`Lyo.Api.Export`](../../Api/Lyo.Api.Export/README.md)
- [`Lyo.Audit`](../../../Core/Audit/Lyo.Audit/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.Scheduler`](../../../Core/Scheduler/Lyo.Scheduler/README.md)

### Related / optional packages

- [`Lyo.Job.Alerts`](../Lyo.Job.Alerts/README.md) — consume alert routing key
- [`Lyo.Job.SignalR`](../Lyo.Job.SignalR/README.md) — live dashboard
- [`Lyo.Api.Export.Csv`](../../Api/Lyo.Api.Export.Csv/README.md), [`Lyo.Api.Export.Xlsx`](../../Api/Lyo.Api.Export.Xlsx/README.md) — export add-ons
