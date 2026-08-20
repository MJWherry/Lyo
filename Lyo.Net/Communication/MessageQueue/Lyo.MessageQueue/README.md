# Lyo.MessageQueue

`IMqService` is the queue and exchange contract. Schedulers, workers, and gateways compile against one interface and swap RabbitMQ or later brokers behind `Lyo.MessageQueue.*` implementations.

Implements `Lyo.Health.IHealth` so dashboards can ping broker connectivity alongside DB/cache checks.

## `IMqService`

- `ConnectAsync` / `DisconnectAsync` open and close sessions.
- `IsConnected` is a synchronous snapshot for guards.

## Messaging envelopes (`QueueMessageEnvelope<T>`)

`QueueMessageEnvelope<T>` carries `Payload`, `RequeueCount`, `MessageId`, `EnqueuedAt`, `TraceId`, and
`Version` alongside the payload. The internal `QueueWorkerHelpers.DeserializeMessage<T>` detects JSON
shaped like `{ Payload, RequeueCount, … }` vs raw DTO JSON so you can:

- Attach `RequeueCount` / identifiers / timestamps without wrapping every caller manually.
- Migrate legacy producers that still emit bare JSON objects. The first requeue from a legacy message
  is automatically wrapped in an envelope by `QueueWorkerBase` so subsequent requeues count correctly.

`MessageProcessingExceptionHandling` (`IgnoreAndRemoveFromQueue`, `ThrowAndRemoveFromQueue`,
`RequeueOnException`) is the shared enum implementations expose for tuning how thrown exceptions in
message handlers map to ack/nack/requeue.

## `QueueWorkerBase`

- Implements `IHostedService` + `IDisposable` + `IHealth`. `StartAsync` connects if needed and calls `SubscribeToQueue`. `StopAsync` cancels and waits up to `DrainTimeoutMs` (default `30_000` ms) for in-flight messages before returning.
- Parses messages via the envelope-aware `DeserializeMessage` helper.
- Executes your abstract `DoWorkAsync(TRequest, CancellationToken) → Task<TResult>`.
- Applies requeue heuristics: an optional `Metadata["requeue"]` bool on the result overrides the default `!IsSuccess` requeue.
- Supports `maxRequeueCount` + optional DLQ publish (`dlqName`); when the count is exceeded, the original message bytes are forwarded to the DLQ if configured, otherwise the message is dropped at Error level.
- Optional retry backoff via the public `RequeueDelay` property: when set and the transport implements `IDelayedMqService`, each counted requeue is republished with a broker-side delay of `RequeueDelay × attempt` (linear backoff), so a failing message cannot burn through its retry budget in milliseconds. Transports without delay support republish immediately.

## Envelope retry flow

Every failure path acks the original delivery and republishes a counted copy. A bad message or a
repeatedly-throwing `DoWorkAsync` cannot loop forever on broker redelivery:

```mermaid
flowchart LR
    msg[Message delivered] --> des{Deserialize\nautocorrect ladder}
    des -->|unrecoverable| poison[Ack + forward original bytes to DLQ]
    des -->|ok| work[DoWorkAsync]
    work -->|success| ack[Ack]
    work -->|failure / exception| cap{RequeueCount < max?}
    cap -->|yes| requeue["Ack + republish with RequeueCount+1\n(delayed by RequeueDelay × attempt when supported)"]
    requeue --> msg
    cap -->|no| dlq[Ack + route to DLQ or drop]
```

## `QueueWorkerOptions`

Shared defaults resolved by DI registration paths (for example `AddJobWorker` / `AddJobWorkerFromConfiguration`,
section name `"QueueWorkerOptions"`). The `QueueWorkerBase` constructor signature stays unchanged:

| Property | Type | Default | Purpose |
| ------------------------ | ----------- | ------- | ---------------------------------------------------------------------------------------------------------------- |
| `DefaultMaxRequeueCount` | `int?` | `5` | Requeue cap applied when a worker doesn't pass an explicit `maxRequeueCount`. `null` = unlimited retries. |
| `RequeueDelay` | `TimeSpan?` | `2s` | Base retry delay (linear backoff by attempt). Requires an `IDelayedMqService` transport; `null`/zero = no delay. |

- Tracks `InFlightCount`, exposes a `queue-worker:{QueueName}` health probe via `CheckHealthAsync`, and
  emits metrics via the injected `IMetrics`:
    - `queue.worker.message.processing.duration` (timer; tag `queue`)
    - `queue.worker.messages.received` / `processed` / `requeued` / `deserialization.failed` / `dropped.max_requeue` / `dlq`
    - `queue.worker.started` / `start.failed` / `stopped`
    - `queue.worker.running` (gauge; `1` while running, `0` after stop)
    - Error records on `queue.worker.message.processing.error` and `queue.worker.message.deserialization.error`

This is the hosted-consumer path used by Lyo job and email workers.

## Health and diagnostics

- `MqServiceHealth`. `Queues` and `Connections` collections.
- `MessageQueueInfo(Name, State?, Type?, Messages, MessagesReady, MessagesUnacknowledged, Consumers, AdditionalProperties)`. Per-queue snapshot.
- `ConnectionInfo(User, UserProvidedName?, State, VHost)`. Connection snapshot.
- `QueuePeekMessage(Payload, PayloadEncoding?, Exchange?, RoutingKey?, MessageCount?, Redelivered)`. What `PeekQueueMessages` returns.

## Operations

- Treat `byte[]` as opaque at the interface. Sign and compress at the app layer if payloads leave a trust zone.
- **Idempotency.** Requeue storms happen when handlers throw. Make side effects idempotent or persist processing tokens.
- **Health.** Implementors should make `IHealth` report broker reachability. Do not report healthy when `IsConnected()` is false unless you intend a lazy connect.

## Implementations & UI

| Package | Role |
| --------------------------------------------------------------------- | ---------------------------------------------------------------- |
| [`Lyo.MessageQueue.RabbitMq`](../Lyo.MessageQueue.RabbitMq/README.md) | `RabbitMQ.Client` driver and DI helpers. |
| `Lyo.MessageQueue.Web.Components` | Blazor UI for queue inspection and management in internal tools. |
| `Lyo.MessageQueue.RabbitMq.Web.Components` | Rabbit-specific components and wiring. |

## Related

- [`Lyo.Job.Scheduler`](../../../Integration/Job/Lyo.Job.Scheduler/README.md). Often pairs with queues for fan-out triggers.
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md). Shared health reporting.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)