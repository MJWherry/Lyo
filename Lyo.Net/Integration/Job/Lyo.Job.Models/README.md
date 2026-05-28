# Lyo.Job.Models

Shared DTOs, builders, enums and message-queue contracts for the Lyo job-management subsystem. Consumed by `Lyo.Job.Postgres` (the API host), `Lyo.Job.Scheduler`, `Lyo.Job.Worker`,
and any Blazor / client code that talks to the job service.

Multi-targets `netstandard2.0` and `net10.0` so the same DTOs flow through legacy callers and modern .NET hosts.

## Requests and responses

Located under `Request/` and `Response/`. Each lifecycle entity has a request DTO for create/update and a response DTO for reads:

| Entity               | Request                     | Response                                  |
|----------------------|-----------------------------|-------------------------------------------|
| Job definition       | `JobDefinitionReq`          | `JobDefinitionRes`                        |
| Parameter            | `JobParameterReq`           | `JobParameterRes`                         |
| Schedule             | `JobScheduleReq`            | `JobScheduleRes`                          |
| Schedule parameter   | `JobScheduleParameterReq`   | `JobScheduleParameterRes`                 |
| Trigger              | `JobTriggerReq`             | `JobTriggerRes`                           |
| Trigger parameter    | `JobTriggerParameterReq`    | `JobTriggerParameterRes`                  |
| Parallel restriction | `JobParallelRestrictionReq` | `JobParallelRestrictionRes`               |
| Run                  | `JobRunReq`                 | `JobRunRes`                               |
| Run parameter        | `JobRunParameterReq`        | `JobRunParameterRes`                      |
| Run result           | `JobRunResultReq`           | `JobRunResultRes`                         |
| Run log              | `JobRunLogReq`              | `JobRunLogRes`                            |
| File upload          | _(N/A)_                     | `JobFileUploadRes`                        |
| Definition stats     | _(N/A)_                     | `JobDefinitionStatsRes`, `SpJobStatistic` |

`JobInfo` (top-level) bundles a `JobDefinitionRes` with its last / last successful / last failed runs for dashboards.

`JobDefinitionReq` carries retry, timeout, concurrency and circuit-breaker knobs (`MaxRetryCount`, `RetryBackoffSeconds`, `TimeoutMinutes`, `MaxConcurrentRuns`,
`CircuitBreakerThreshold`, `CircuitBreakerResetMinutes`) plus nested `CreateParameters`, `CreateSchedules`, `CreateTriggers`, `CreateParallelRestrictions`.

`JobRunRes` exposes `GetParameterValueAs<T>`, `GetResultValueAs<T>`, `GetParameterDictionary`, and `GetResultDictionary` for typed access to parameter and result bags.

## Builders (`Builders/`)

Fluent factories for assembling request DTOs without dropping into raw initializers.

- **`JobDefinitionBuilder`** — `New(name)`, `SetDescription`, `SetType`, `ForCSharpWorker` / `ForPythonWorker`, `AsImportInCSharp`, `AddSchedule(...)` (overloads accept a
  `JobScheduleBuilder` action, `MonthFlags`/`DayFlags` + times, or `MonthFlags`/`DayFlags` + start/end/interval), `AddDailySchedule`, `AddWorkDaySchedule`, `AddJobParameter`,
  `AddEncryptedJobParameter`, `AddJobTrigger`, `AddJobParallelRestriction`, `AddPaginationAmount`, and the email-parameter helpers (`AddEmailTo`, `AddEmailCc`, `AddEmailBcc`,
  `AddEmailAttachment`). `Build()` returns the underlying `JobDefinitionReq`.
- **`JobScheduleBuilder`** — `EveryDay`, `Weekdays`, `SetMonths`, `SetDays`, `SetTimes`, `SetInterval(start, end, intervalMinutes)`, `WithDescription`, `Enabled`. `Build()` returns
  a `JobScheduleReq`; `BuildScheduleDefinition()` converts to `Lyo.Schedule.Models.ScheduleDefinition` for use with `Lyo.Scheduler`.
- **`JobTriggerBuilder`** — `SetCondition(key, ComparisonOperatorEnum, value)`, `SetDescription`, `SetEnabled`, `AddTriggerParameter`, plus email-parameter helpers (
  `AddEmailToParameter`, `AddEmailCcParameter`, `AddEmailBccParameter`, `AddEmailAttachmentParameter`).
- **`JobRunBuilder`** — Constructs ad-hoc `JobRunReq` instances from a definition id and `createdBy`. `AddParameter` overloads cover `string`, `int?`, and any `JobParameterType`.
  `AddEncryptedParameter` attaches a pre-encrypted blob.
- **`JobRunResultBuilder`** — Dictionary-keyed builder for `List<JobRunResultReq>`. Typed `AddString`, `AddInt`, `AddLong`, `AddBool`, `AddDateTime`, `AddEnum<T>`, `AddAsJson<T>`,
  plus mutators `IncrementInt`, `IncrementLong`, `AppendString`, `AddIntIfGreaterThan`, `AddIf`, `Remove`, `Clear`, `Contains`, `Get`.

```csharp
var definition = JobDefinitionBuilder
    .New("Nightly Sync", "Pulls everything from the upstream API")
    .ForCSharpWorker()
    .SetType("Import")
    .AddJobParameter("BatchSize", JobParameterType.Int, 500)
    .AddSchedule(s => s
        .EveryDay()
        .SetInterval("00:00", "23:59", intervalMinutes: 60)
        .WithDescription("Hourly"))
    .Build();

var run = JobRunBuilder
    .New(definition.Id, "scheduler")
    .AddParameter("BatchSize", 1000)
    .Build();

var results = new JobRunResultBuilder()
    .AddInt(Constants.Data.JobRunResultKey.CreateCount, 42)
    .AddString(Constants.Data.JobRunResultKey.Result, "ok")
    .Build();
```

## Enums

- `JobState` — `Unknown`, `Queued`, `Running`, `Finished`, `Cancelled`, `Cancelling`.
- `JobRunResult` — `Unknown`, `Success`, `SuccessWithWarnings`, `PartialSuccess`, `Failure`, `Cancelled`, `Skipped`, `Timeout`.
- `JobParameterType` — `String`, `Bool`, `Enum`, `DateTime`, `DateOnly`, `TimeOnly`, `Int`, `Long`, `Decimal`, `Guid`, `Regex`, `Json`, `Xml`, plus `Unknown`.
- `JobLogLevel` — log severity for `JobRunLogReq`.

## Event publisher (`Events/IJobEventPublisher`)

Transport-agnostic abstraction for job lifecycle messaging. Implementations wire up to RabbitMQ, Azure Service Bus, AWS SQS, etc. (the default `MqJobEventPublisher` in
`Lyo.Job.Postgres` wraps `IMqService`).

Members:

- `IsConnected()` and `SetupAsync(ct)` — connection / topology bootstrap.
- Publishers: `PublishRunCreatedAsync(runId, workerType, ct)`, `PublishRunStartedAsync(runId, ct)`, `PublishRunFinishedAsync(runId, ct)`, `PublishRunCancelledAsync(runId, ct)`,
  `PublishDefinitionUpdatedAsync(definitionId, ct)`.
- Subscribers: `SubscribeToDefinitionUpdatesAsync(queueName, handler, ct)`, `SubscribeToRunCompletionsAsync(handler, ct)`,
  `SubscribeToRunCancellationsAsync(workerType, handler, ct)`.

## Constants

`Constants.Mq` declares the MQ topology:

| Constant                            | Value                                  |
|-------------------------------------|----------------------------------------|
| `QueueJobRunFinish`                 | `job.run.complete`                     |
| `JobEventExchange`                  | `job.events`                           |
| `JobDefinitionChangeKey`            | `job.notifications.definition.updated` |
| `JobRunCreatedRoutingKey`           | `job.notifications.run.created`        |
| `JobRunStartedRoutingKey`           | `job.notifications.run.started`        |
| `JobRunCancelledRoutingKey`         | `job.notifications.run.cancelled`      |
| `JobRunFinishedRoutingKey`          | `job.notifications.run.finished`       |
| `QueueGetJobRunCreated(workerType)` | `job.run.{workerType}`                 |

`Constants.Rest.Job` exposes the route prefixes (`Job`, `Job/Definition`, `Job/Run`, `Job/Run/Files`, …) and per-run lifecycle routes (`RunStarted(runId)`, `RunFinished(runId)`,
`RunHeartbeat(runId)`, `RunLog(runId)`, `DefinitionStats(definitionId)`).

`Constants.Data.JobRunResultKey` and `Constants.Data.JobRunParameterKey` provide well-known keys (`Result`, `ExecutionTime`, `CreateCount`, …, plus pagination/parallelism/chunking
keys). The email/file/business-domain keys are marked `[Obsolete]` so they migrate into the consuming application.

## Extensions

- `JobScheduleExtensions.ToScheduleDefinition(...)` — converts a `JobScheduleReq` or `JobScheduleRes` to `Lyo.Schedule.Models.ScheduleDefinition`.
- `JobRunParameterExtensions` (in `Extensions/`) — typed `GetString`, `GetInt`, `GetLong`, `GetDecimal`, `GetBool`, `GetGuid`, `GetDateTime`, `GetEnum<T>`, `GetAs<T>` on
  `IReadOnlyList<JobRunParameterRes>?` and `IReadOnlyList<JobRunResultRes>?`. Useful outside a full `JobRunRes` context.

## Related projects

- [`Lyo.Api.Models`](../../Api/Lyo.Api.Models/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Schedule.Models`](../../../Core/Schedule/Lyo.Schedule.Models/README.md)
