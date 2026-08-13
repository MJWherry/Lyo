# Lyo.MessageQueue

Portable **queue + exchange** abstraction (`IMqService`) so schedulers, workers, and gateways can compile against **one contract** while swapping RabbitMQ—or future brokers—behind **`Lyo.MessageQueue.*` implementations**.

Implements **`Lyo.Health.IHealth`** so dashboards can ping broker connectivity alongside DB/cache checks.

## Contract highlights (`IMqService`)

- **`ConnectAsync` / `DisconnectAsync`** establish sessions.
- **`IsConnected`** — synchronous snapshot for guards.

## Messaging envelopes (`QueueMessageEnvelope<T>`)

`QueueMessageEnvelope<T>` carries `Payload`, `RequeueCount`, `MessageId`, `EnqueuedAt`, `TraceId`, and
`Version` alongside the payload. The internal `QueueWorkerHelpers.DeserializeMessage<T>` detects JSON
shaped like `{ Payload, RequeueCount, … }` vs raw DTO JSON so you can:

- Attach `RequeueCount` / identifiers / timestamps without wrapping every caller manually.
- Migrate legacy producers that still emit bare JSON objects — the first requeue from a legacy message
  is automatically wrapped in an envelope by `QueueWorkerBase` so subsequent requeues count correctly.

`MessageProcessingExceptionHandling` (`IgnoreAndRemoveFromQueue`, `ThrowAndRemoveFromQueue`,
`RequeueOnException`) is the shared enum implementations expose for tuning how thrown exceptions in
message handlers are mapped to ack/nack/requeue semantics.

## Hosted worker pattern (`QueueWorkerBase<TRequest, TResult>` where `TResult : ResultBase`)

- Implements `IHostedService` + `IDisposable` + `IHealth` — `StartAsync` connects (if needed) and calls `SubscribeToQueue`; `StopAsync` cancels and waits up to `DrainTimeoutMs` (default `30_000` ms) for in-flight messages before returning.
- Parses messages via the envelope-aware `DeserializeMessage` helper.
- Executes your abstract `DoWorkAsync(TRequest, CancellationToken) → Task<TResult>`.
- Applies requeue heuristics: an optional `Metadata["requeue"]` bool on the result overrides the default `!IsSuccess` requeue.
- Supports `maxRequeueCount` + optional DLQ publish (`dlqName`); when the count is exceeded, the original message bytes are forwarded to the DLQ if configured, otherwise the message is dropped at Error level.
- Optional retry backoff via the public `RequeueDelay` property: when set and the transport implements `IDelayedMqService`, each counted requeue is republished with a broker-side delay of `RequeueDelay × attempt` (linear backoff), so a failing message cannot burn through its retry budget in milliseconds. Transports without delay support republish immediately.

## Hosted worker pattern (`QueueWorkerBase<TRequest, TResult>` where `TResult : ResultBase`) — Envelope retry flow

Every failure path acks the original delivery and republishes a counted copy — a bad message or a
repeatedly-throwing `DoWorkAsync` can never spin in an infinite broker redelivery loop:

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

## Hosted worker pattern (`QueueWorkerBase<TRequest, TResult>` where `TResult : ResultBase`) — `QueueWorkerOptions`

Shared defaults resolved by DI registration paths (e.g. `AddJobWorker` / `AddJobWorkerFromConfiguration`,
section name `"QueueWorkerOptions"`) — the `QueueWorkerBase` constructor signature stays unchanged:

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

This is the **production-grade** path for long-running consumers in Lyo’s own job/email stacks.

## Hosted worker pattern (`QueueWorkerBase<TRequest, TResult>` where `TResult : ResultBase`) — Health and diagnostics surface

- `MqServiceHealth` — `Queues` and `Connections` collections.
- `MessageQueueInfo(Name, State?, Type?, Messages, MessagesReady, MessagesUnacknowledged, Consumers, AdditionalProperties)` — generic per-queue snapshot.
- `ConnectionInfo(User, UserProvidedName?, State, VHost)` — generic connection snapshot.
- `QueuePeekMessage(Payload, PayloadEncoding?, Exchange?, RoutingKey?, MessageCount?, Redelivered)` — what `PeekQueueMessages` returns.

## Operational guidance

- Treat **`byte[]`** as **opaque at the interface**—sign and compress at the app layer if payloads leave a trust zone.
- **Idempotency**: requeue storms happen when handlers throw—make side effects idempotent or persist processing tokens.
- **Health**: implementors should ensure **`IHealth`** surfaces broker reachability; don’t lie “healthy” when `IsConnected()` is false unless you intend lazy connect.

## Implementations & UI

| Package | Role |
| --------------------------------------------------------------------- | ------------------------------------------------------------ |
| [`Lyo.MessageQueue.RabbitMq`](../Lyo.MessageQueue.RabbitMq/README.md) | Production **`RabbitMQ.Client`** driver + DI helpers. |
| **`Lyo.MessageQueue.Web.Components`** | Blazor UX for queue inspection/management in internal tools. |
| **`Lyo.MessageQueue.RabbitMq.Web.Components`** | Rabbit-specific components + wiring. |

## Related

- [`Lyo.Job.Scheduler`](../../../Integration/Job/Lyo.Job.Scheduler/README.md) — often pairs with queues for fan-out triggers.
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md) — uniform health reporting.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)