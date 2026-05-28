# Lyo.Job.Postgres

PostgreSQL persistence and minimal-API host for the Lyo job-management subsystem. Wraps EF Core, the Lyo CRUD/Query stack, Mapster, and `IJobEventPublisher` so a host can drop in a
complete job service: definitions, parameters, schedules, triggers, runs, run parameters, run results, run logs, and stats.

## Drop-and-play registration

`AddPostgresJobManagement` registers the `JobContext` factory, optional auto-migrations, the Lyo CRUD services, `JobService`, and a default no-op `IJobEventPublisher` (
`NullJobEventPublisher`) so resolution always succeeds. Replace the publisher with `AddMqJobEventPublisher()` once you have an `IMqService` available.

```csharp
services.AddLyoQueryServices();
services.AddFusionCache(...);            // or AddLocalCache(...)
services.AddMapster(cfg => cfg.Apply(Extensions.ConfigureJobMappings));
services.AddPostgresJobManagement(o => {
    o.ConnectionString = connectionString;
    o.EnableAutoMigrations = true;
});

// After IMqService is registered (e.g. AddRabbitMq):
services.AddMqJobEventPublisher();

// Optional dead-job watchdog + circuit-breaker auto-reset:
services.AddJobMaintenanceService();

var app = builder.Build();
app.BuildJobGroup();
```

`PostgresJobOptions` has two settings, both bindable from the `PostgresJob` configuration section (`PostgresJobOptions.SectionName`):

| Property               | Default | Notes                                                           |
|------------------------|---------|-----------------------------------------------------------------|
| `ConnectionString`     | `""`    | Npgsql connection string. Required.                             |
| `EnableAutoMigrations` | `false` | When `true`, `Lyo.Postgres` runs pending migrations on startup. |

`Schema` is fixed to `job`. EF migrations history lives in `job.__EFMigrationsHistory`.

## DI extension methods

All registrations hang off `IServiceCollection`:

| Method                                                        | Purpose                                                                              |
|---------------------------------------------------------------|--------------------------------------------------------------------------------------|
| `AddJobDbContext(connectionString)`                           | Adds a scoped `JobContext` plus the underlying factory (legacy callers).             |
| `AddJobDbContext(Action<DbContextOptionsBuilder>)`            | Adds `JobContext` with a hand-written `DbContextOptionsBuilder`.                     |
| `AddJobDbContextFactory(PostgresJobOptions)`                  | Adds `IDbContextFactory<JobContext>` and migrations support.                         |
| `AddJobDbContextFactory(Action<PostgresJobOptions>)`          | Inline-configured variant.                                                           |
| `AddJobDbContextFactoryFromConfiguration(config, section?)`   | Binds `PostgresJobOptions` from configuration (defaults to `PostgresJob`).           |
| `AddPostgresJobManagement(PostgresJobOptions)`                | Full job service: factory + CRUD + `JobService` + `NullJobEventPublisher`.           |
| `AddPostgresJobManagement(Action<PostgresJobOptions>)`        | Inline-configured variant.                                                           |
| `AddPostgresJobManagementFromConfiguration(config, section?)` | Configuration-bound variant.                                                         |
| `AddJobMaintenanceService()`                                  | Adds `JobMaintenanceService` as a hosted background service.                         |
| `AddMqJobEventPublisher()`                                    | Replaces `NullJobEventPublisher` with `MqJobEventPublisher` (requires `IMqService`). |

## `JobService` lifecycle API

`JobService` orchestrates run state changes and event publishing. It depends on the Lyo CRUD/query/patch services, `ILyoMapper`, `IJobEventPublisher`, and
`IDbContextFactory<JobContext>`.

| Method                                            | Behavior                                                                                                                                                                                                                                                                                                                         |
|---------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Log(jobRunId, JobRunLogReq)`                     | Inserts a run log entry.                                                                                                                                                                                                                                                                                                         |
| `CreateJobRun(JobRunReq, ct)`                     | Enforces `MaxConcurrentRuns`, validates parameters against the definition (`Required`, `MinLength`, `MaxLength`, `ValidationRegex`, `AllowedValues` pipe-separated), inserts a `Queued` run, swallows the `ix_job_run_schedule_slot_unique` 23505 conflict (idempotent across scheduler instances), then publishes `RunCreated`. |
| `StartedJobRun(jobRunId)`                         | Patches `State = Running` + `StartedTimestamp`, publishes `RunStarted`.                                                                                                                                                                                                                                                          |
| `CancelJobRun(jobRunId)`                          | Verifies the run is `Running`/`Queued`, patches to `Cancelling`, publishes `RunCancelled`.                                                                                                                                                                                                                                       |
| `FinishedJobRun(jobRunId, results)`               | Verifies `Running`/`Cancelling`, derives `JobRunResult` from the `Result` key, patches `State = Finished` + `FinishedTimestamp` + `Result`, inserts each `JobRunResultReq`, publishes `RunFinished`, reloads the run with related navigations.                                                                                   |
| `RerunJob(jobRunId)`                              | Clones an existing run as a new `Queued` run with `ReRanFromJobRunId` set, then publishes `RunCreated`.                                                                                                                                                                                                                          |
| `GetDefinitionStats(definitionId, days = 30, ct)` | Aggregates `JobDefinitionStatsRes` over the rolling window: total/success/failure counts, success rate, average + p95 duration (p95 requires ≥20 samples), consecutive failures from the most recent 100 results, last run / last success timestamps.                                                                            |

All event-bearing methods short-circuit with `MessageQueueConnectionIssue` when `IJobEventPublisher.IsConnected()` is `false`.

## Endpoint matrix (`BuildJobGroup`)

`BuildJobGroup(this WebApplication)` is called once after `app.Build()` and registers the full CRUD surface for the job schema. Tag `"Job"`. Every CRUD endpoint set is created with
`ApiFeatureFlag.All | UpsertInheritCreate | UpsertInheritUpdate | PatchInheritsUpdate`.

| Route prefix                               | Entity             | Endpoints                                                      | Side effects                                                                                                                                                          |
|--------------------------------------------|--------------------|----------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Job/Definition`                           | `JobDefinition`    | Full CRUD + Query                                              | `BeforeCreate` assigns a COMB GUID. `AfterUpdate` publishes a definition-updated event. `BeforeDelete` cascades runs/parameters/schedules and clears self-references. |
| `Job/Definition/Parameter`                 | `JobParameter`     | Full CRUD                                                      | New GUID on create; `AfterUpdate` republishes the owning definition.                                                                                                  |
| `Job/Schedule`                             | `JobSchedule`      | Full CRUD                                                      | New GUID on create; `AfterUpdate` republishes the owning definition.                                                                                                  |
| `Job/Triggers`                             | `JobTrigger`       | Full CRUD                                                      | New GUID on create; `AfterUpdate` republishes both source and target definitions.                                                                                     |
| `Job/Run`                                  | `JobRun`           | Query + Get + Delete + Delete-bulk                             | Delete cascades logs/parameters/results and clears `ReRanFromJobRunId` self-references.                                                                               |
| `Job/Run/Parameter`                        | `JobRunParameter`  | Query + Get + Create                                           |                                                                                                                                                                       |
| `Job/Run/Result`                           | `JobRunResult`     | Query + Get + Create                                           |                                                                                                                                                                       |
| `Job/Run/Log`                              | `JobRunLog`        | Query + Get + Create                                           |                                                                                                                                                                       |
| `GET Job/Definition/{id:guid}/Stats?days=` | (stats projection) | Maps to `JobService.GetDefinitionStats` (defaults to 30 days). |

The remaining route constants from `Lyo.Job.Models.Constants.Rest.Job` (`Job/Run/{id}/Started`, `Job/Run/{id}/Finished`, `Job/Run/{id}/Heartbeat`, `Job/Run/{id}/Log`,
`Job/Run/Files`, `Job/ScheduleParameters`, `Job/TriggerParameters`, …) are mounted by complementary host code (worker/job-api endpoints); they are referenced from this project but
not registered inside `BuildJobGroup`.

## Mapster mappings (`ConfigureJobMappings`)

Apply with `cfg.Apply(Extensions.ConfigureJobMappings)`. Bi-directional mappings cover every entity ↔ DTO pair (`JobDefinition`, `JobParameter`, `JobSchedule`,
`JobScheduleParameter`, `JobTrigger`, `JobTriggerParameter`, `JobParallelRestriction`, `JobRun`, `JobRunParameter`, `JobRunResult`, `JobRunLog`). Entity → response mappings use
`MapWith()` to side-step a Mapster issue with positional records during eager `Compile()`. Schedules store `DayFlags`/`MonthFlags`/`Type` as strings and round-trip through
`Enum.Parse`; `JobScheduleDatabaseExtensions.ToScheduleDefinition` does the same for direct entity callers.

## Event publishers (`Events/`)

- `NullJobEventPublisher` — registered by default, reports `IsConnected() == false`, returns `Task.CompletedTask` for all operations so callers fail fast with
  `MessageQueueConnectionIssue`.
- `MqJobEventPublisher` — registered by `AddMqJobEventPublisher`. Calls `IMqService` to create the `job.run.complete` queue, the per-worker-type `job.run.{workerType}` queue, the
  `job.events` exchange, and (for `SubscribeToRunCancellationsAsync`) the per-worker-type `job.run.{workerType}.cancel` queue. Routing keys match `Lyo.Job.Models.Constants.Mq`.

## Maintenance service (`JobMaintenanceService`)

`BackgroundService` that ticks every 30 seconds and:

- **Fails dead jobs** — moves `Running`/`Cancelling` runs whose `LastHeartbeatUtc + JobDefinition.TimeoutMinutes` has elapsed into `Finished` with `JobRunResult.Timeout`.
- **Resets circuit breakers** — re-enables definitions where `Enabled == false`, `CircuitBreakerResetMinutes > 0`, and the `CircuitBreakerTrippedAt + CircuitBreakerResetMinutes`
  cooldown has expired.

## Design-time migrations

The package ships with `JobContextFactory` so EF tooling can discover the context. Set `JOB_CONNECTION_STRING` and run:

```bash
export JOB_CONNECTION_STRING="Host=localhost;Database=postgres;Username=postgres;Password=password"
dotnet ef migrations add YourMigrationName --project Lyo.Job.Postgres
```

## Dependencies

*(Synchronized from `Lyo.Job.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Mapster`                                         | `[10,)` |
| `Microsoft.EntityFrameworkCore.Design`            | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- [`Lyo.Api`](../../Api/Lyo.Api/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
