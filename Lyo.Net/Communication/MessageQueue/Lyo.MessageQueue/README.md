# Lyo.MessageQueue

Portable **queue + exchange** abstraction (`IMqService`) so schedulers, workers, and gateways can compile against **one contract** while swapping RabbitMQ—or future brokers—behind
**`Lyo.MessageQueue.*` implementations**.

Implements **`Lyo.Health.IHealth`** so dashboards can ping broker connectivity alongside DB/cache checks.

## Contract highlights (`IMqService`)

**Lifecycle**

- **`ConnectAsync` / `DisconnectAsync`** establish sessions.
- **`IsConnected`** — synchronous snapshot for guards.

**Queues**

- **`CreateQueue`** — durability / exclusivity / auto-delete plus broker-specific **`arguments`** bag (TTL, quorum queues, classic policies—whatever the plug-in forwards).
- **`DeleteQueue`** — optional **if-unused / if-empty** guards mirror broker semantics.
- **`ClearQueue`** purge without destroying topology.

**Consumption**

- **`PeekQueueMessages`** non-destructive reads for diagnostics/back-pressure introspection (**`QueuePeekMessage`** payloads).
- **`SubscribeToQueue(queueName, Func<byte[], Task<bool>> handler, CancellationToken)`** —
    - Handler returns **`true`** ⇒ **requeue/nack-with-requeue** semantics (broker-specific mapping).
    - **`false`** ⇒ acknowledge / remove.
    - **Cancellation** tears down the subscription loop cooperatively.

**Publishing**

- **`SendToQueue`** sends raw **`byte[]`** payloads—serialization policy lives in your worker (`System.Text.Json`, protobuf, compressed blobs, …).
- **
  `QueueMessageExtensions.SendToQueueWithEnvelopeAsync<T>(this IMqService, string queueName, T payload, JsonSerializerOptions?, string? messageId, DateTime? enqueuedAt, string? traceId)`
  **
  is a typed publish helper that wraps `payload` in a fresh `QueueMessageEnvelope<T>` (requeue count `0`,
  generated `MessageId`, `EnqueuedAt = UtcNow`) and forwards the JSON bytes to `SendToQueue`. Use it when
  publishing to queues consumed by `QueueWorkerBase` so requeue tracking starts on the first hop.

**Topics / exchanges**

- **`BindQueueToExchange`** + **`SendToExchange`** expose AMQP-shaped routing (`routingKey`). Non-Rabbit backends may approximate via topic subscriptions—the interface comment
  documents that expectation.

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

Subclass for **typed JSON consumers**:

- Implements `IHostedService` + `IDisposable` + `IHealth` — `StartAsync` connects (if needed) and calls
  `SubscribeToQueue`; `StopAsync` cancels and waits up to `DrainTimeoutMs` (default `30_000` ms) for in-flight
  messages before returning.
- Parses messages via the envelope-aware `DeserializeMessage` helper.
- Executes your abstract `DoWorkAsync(TRequest, CancellationToken) → Task<TResult>`.
- Applies requeue heuristics: an optional `Metadata["requeue"]` bool on the result overrides the default
  `!IsSuccess` requeue.
- Supports `maxRequeueCount` + optional DLQ publish (`dlqName`); when the count is exceeded, the original
  message bytes are forwarded to the DLQ if configured, otherwise the message is dropped at Error level.
- Tracks `InFlightCount`, exposes a `queue-worker:{QueueName}` health probe via `CheckHealthAsync`, and
  emits metrics via the injected `IMetrics`:
    - `queue.worker.message.processing.duration` (timer; tag `queue`)
    - `queue.worker.messages.received` / `processed` / `requeued` / `deserialization.failed` / `dropped.max_requeue` / `dlq`
    - `queue.worker.started` / `start.failed` / `stopped`
    - `queue.worker.running` (gauge; `1` while running, `0` after stop)
    - Error records on `queue.worker.message.processing.error` and `queue.worker.message.deserialization.error`

This is the **production-grade** path for long-running consumers in Lyo’s own job/email stacks.

### Health and diagnostics surface

`IMqService` itself extends `IHealth` (broker reachability). Implementations also publish broker-level
diagnostics through these neutral record types so dashboards can render the same shape across providers:

- `MqServiceHealth` — `Queues` and `Connections` collections.
- `MessageQueueInfo(Name, State?, Type?, Messages, MessagesReady, MessagesUnacknowledged, Consumers, AdditionalProperties)` — generic per-queue snapshot.
- `ConnectionInfo(User, UserProvidedName?, State, VHost)` — generic connection snapshot.
- `QueuePeekMessage(Payload, PayloadEncoding?, Exchange?, RoutingKey?, MessageCount?, Redelivered)` — what `PeekQueueMessages` returns.

## Operational guidance

- Treat **`byte[]`** as **opaque at the interface**—sign and compress at the app layer if payloads leave a trust zone.
- **Idempotency**: requeue storms happen when handlers throw—make side effects idempotent or persist processing tokens.
- **Health**: implementors should ensure **`IHealth`** surfaces broker reachability; don’t lie “healthy” when `IsConnected()` is false unless you intend lazy connect.

## Implementations & UI

| Package                                                               | Role                                                         |
|-----------------------------------------------------------------------|--------------------------------------------------------------|
| [`Lyo.MessageQueue.RabbitMq`](../Lyo.MessageQueue.RabbitMq/README.md) | Production **`RabbitMQ.Client`** driver + DI helpers.        |
| **`Lyo.MessageQueue.Web.Components`**                                 | Blazor UX for queue inspection/management in internal tools. |
| **`Lyo.MessageQueue.RabbitMq.Web.Components`**                        | Rabbit-specific components + wiring.                         |

## Related

- [`Lyo.Job.Scheduler`](../../../Integration/Job/Lyo.Job.Scheduler/README.md) — often pairs with queues for fan-out triggers.
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md) — uniform health reporting.
