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

**Topics / exchanges**

- **`BindQueueToExchange`** + **`SendToExchange`** expose AMQP-shaped routing (`routingKey`). Non-Rabbit backends may approximate via topic subscriptions—the interface comment
  documents that expectation.

## Messaging envelopes (`QueueMessageEnvelope<T>`)

Higher-level helpers (see `QueueWorkerHelpers`) detect JSON shaped like **`{ Payload, RequeueCount, … }`** vs raw DTO JSON so you can:

- Attach **`RequeueCount`** / identifiers / timestamps without wrapping every caller manually.
- Migrate legacy producers that still emit bare JSON objects.

Workers can **`WrapInEnvelope`** when publishing retries.

## Hosted worker pattern (`QueueWorkerBase<TRequest,TResult>`)

Subclass for **typed JSON consumers**:

- Implements **`IHostedService`** — starts **`SubscribeToQueue`** during host startup.
- Parses messages via **`DeserializeMessage`** (envelope-aware).
- Executes your abstract **`Process`** returning **`TResult : ResultBase` (`Lyo.Result`)**.
- Applies **requeue heuristics**: optional **`Metadata["requeue"]` bool** overrides automatic **`!isSuccess` requeue**.
- Supports **`maxRequeueCount`** + optional **DLQ publish** when poison messages exceed thresholds.
- Tracks **`InFlightCount`**, respects **`DrainTimeoutMs`** on shutdown, emits metrics via **`IMetrics`**.

This is the **production-grade** path for long-running consumers in Lyo’s own job/email stacks.

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
