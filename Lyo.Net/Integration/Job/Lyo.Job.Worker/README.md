# Lyo.Job.Worker

Worker SDK for the Lyo job system. Subclass `JobWorkerBase` and implement a single `ExecuteAsync(IJobWorkerContext)` method — the base class consumes the worker-type queue (`job.run.{workerType}`), drives the full run lifecycle (fetch, start, heartbeat, finish), subscribes to cancellation messages, and reports results back to the Job API.

`JobWorkerBase` extends `Lyo.MessageQueue.QueueWorkerBase<Guid, Result<Unit>>`, so the underlying ack / requeue / DLQ semantics come from the MQ worker base.

## Registration

```csharp
services.AddJobWorker<MyImportWorker>(
    workerType: "csharp",
    apiBaseUrl: "https://api.example.com",
    maxRequeueCount: 5,
    dlqName: "job.run.csharp.dlq");
```

Requires `IMqService`, `IApiClient`, and `IJobEventPublisher` to be registered (typically via `AddMqJobEventPublisher` from `Lyo.Job.Postgres`). `ILogger<TWorker>` and `Lyo.Metrics.IMetrics` are resolved when available.

`AddJobWorker<TWorker>` registers the worker as a singleton and as an `IHostedService`. It uses `Activator.CreateInstance` to construct the worker with the exact `JobWorkerBase` constructor signature, so subclasses just need to forward those parameters:

```csharp
public sealed class MyImportWorker : JobWorkerBase
{
    public MyImportWorker(
        IMqService mq,
        IApiClient api,
        IJobEventPublisher events,
        string workerType,
        string apiBaseUrl,
        ILogger<MyImportWorker>? logger = null,
        IMetrics? metrics = null,
        int? maxRequeueCount = null,
        string? dlqName = null)
        : base(mq, api, events, workerType, apiBaseUrl, logger, metrics, maxRequeueCount, dlqName) { }

    protected override async Task ExecuteAsync(IJobWorkerContext ctx)
    {
        var batch = ctx.Run.JobRunParameters.GetInt("BatchSize") ?? 100;
        ctx.Logger.LogInformation("Importing batch of {Count}", batch);
        ctx.CancellationToken.ThrowIfCancellationRequested();
        ctx.Results.AddCreateCount(batch);
    }
}
```

## `JobWorkerBase` lifecycle

Each message received from `Lyo.Job.Models.Constants.Mq.QueueGetJobRunCreated(workerType)` is processed by `DoWorkAsync(Guid runId, ct)`:

1. **Fetch** — `GET {apiBaseUrl}/Job/Run/{id}?include=…` with includes for parameters, results, schedule, trigger, definition, and definition parameters. Missing runs short-circuit with `ResultVoid.Failure("Job run not found", "NotFound")`.
2. **Start** — `POST {apiBaseUrl}/Job/Run/{id}/Started?include=…` to transition the run to `Running` and return the fully-loaded `JobRunRes`.
3. **Cancellation wiring** — Creates a linked `CancellationTokenSource`, stores it in a per-run dictionary, and constructs the `IJobWorkerContext`.
4. **Heartbeat loop** — Spawns `RunHeartbeatAsync` which PATCHes `LastHeartbeatUtc = DateTime.UtcNow` to `Job/Run/{id}` every `HeartbeatInterval` (default 30 s, overridable).
5. **`ExecuteAsync(ctx)`** — Subclass work. `OperationCanceledException` is caught and translated into `JobWorkerResultBuilder.Cancel()`. Any other exception is logged and recorded via `JobWorkerResultBuilder.AddError` (which also sets the outcome to `Failure`).
6. **Finish** — `POST {apiBaseUrl}/Job/Run/{id}/Finished` with the built `IReadOnlyList<JobRunResultReq>` (`JobWorkerResultBuilder.Build()` appends the `Result` key automatically).

`StartAsync` also calls `IJobEventPublisher.SubscribeToRunCancellationsAsync(WorkerType, OnCancelAsync, ct)` — incoming cancellation messages look up the per-run CTS and cancel it.

Overridable members:

- `HeartbeatInterval` — heartbeat PATCH cadence (default `TimeSpan.FromSeconds(30)`).
- `ExecuteAsync(IJobWorkerContext)` — _required_, the actual work.
- The protected `WorkerType` property is also available to subclasses.

## `IJobWorkerContext`

```csharp
public interface IJobWorkerContext
{
    JobRunRes Run { get; }
    ILogger Logger { get; }
    CancellationToken CancellationToken { get; }
    JobWorkerResultBuilder Results { get; }
}
```

`Run` is fully populated by the start call (parameters, definition, schedule, trigger). `Logger` is scoped with `JobRunId` and `WorkerType` for structured logs. `CancellationToken` reacts to both host shutdown and cancellation messages.

## `JobWorkerResultBuilder`

Fluent builder for the `IReadOnlyList<JobRunResultReq>` reported on finish. `Build()` always appends a `Result` entry with the current outcome so the server can read it back.

| Method                                       | Purpose |
|----------------------------------------------|---------|
| `SetOutcome(JobRunResult)`                   | Override the outcome explicitly. |
| `Fail()`                                     | Outcome `Failure`. |
| `Cancel()`                                   | Outcome `Cancelled`. |
| `SucceedWithWarnings()`                      | Outcome `SuccessWithWarnings`. |
| `AddResult(key, value, type)`                | Arbitrary entry. |
| `AddCount(key, count)`                       | Integer counter entry. |
| `AddCreateCount`, `AddUpdateCount`, `AddDeleteCount`, `AddFailedCount`, `AddNoChangeCount` | Well-known counters (`Constants.Data.JobRunResultKey.*`). |
| `AddError(reason, index = -1)`               | Adds a `FailureReason_{n}` entry and flips the outcome to `Failure`. |
| `AddFailedItem(index, item, reason?)`        | Records a `FailedItem_{n}` and optional `FailureReason_{n}`, flips to `Failure`. |
| `AddApiCallTime(apiName, milliseconds)`      | Records `ApiCallTime_{name}` (long). |
| `CurrentOutcome`                             | Read the current outcome. |
| `Build()`                                    | Materialises the list with the `Result` key appended. |

## Dependencies

*(Synchronized from `Lyo.Job.Worker.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                  | Version |
|------------------------------------------|---------|
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Job.Models`](../Lyo.Job.Models/README.md)
- [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md)
