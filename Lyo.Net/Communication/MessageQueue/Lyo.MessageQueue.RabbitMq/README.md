# Lyo.MessageQueue.RabbitMq

Concrete `IMqService` (`RabbitMqService`) using `RabbitMQ.Client`, also surfaced as `IRabbitMqService`
when you need RabbitMQ-specific knobs (exchanges) that are not part of the shared abstraction.

## Registration

Use `SetupRabbitMqService` or `SetupRabbitMqServiceFromConfiguration` (in `Extensions`):

1. `RabbitMqOptions` singleton (registered via an explicit `Action<RabbitMqOptions>` or bound from
   configuration. Default section name: `RabbitMqOptions.SectionName = "RabbitMqOptions"`.
2. `IConnectionFactory` singleton built from those options with:
    - Host / virtual host / port / credentials from options.
    - `ClientProvidedName` set to `MachineName - ApplicationName (EnvironmentName)` so the connection is
      identifiable in the RabbitMQ management UI.
    - `ClientProperties` populated from the `connectionProperties` dictionary you pass to the extension
      (rich connection metadata — container id, build sha, etc.).
3. `RabbitMqService` registered as a singleton, exposed under all three types: itself, `IRabbitMqService`,
   and `IMqService`.

```csharp
services.SetupRabbitMqServiceFromConfiguration(
    builder.Configuration,
    connectionProperties: new Dictionary<string, object?> { ["build_sha"] = buildSha });
// or
services.SetupRabbitMqService(
    connectionProperties: [],
    options =>
    {
        options.Host = "rabbit.internal";
        options.Port = 5672;
        options.VirtualHost = "/";
        options.AdminUrl = "http://rabbit.internal:15672";
        options.Username = "...";
        options.Password = "...";
    });
```

> `SetupRabbitMqService` requires both the `connectionProperties` dictionary and the configure delegate;
> `SetupRabbitMqServiceFromConfiguration` also throws when `connectionProperties` is `null` — pass an empty
> dictionary (`[]`) when you have no extras.

Health is exposed through the shared `IMqService : IHealth` contract; `CheckHealthAsync` opens a probe
connection via the registered `IConnectionFactory` and reports `Healthy` when it opens.

## `RabbitMqOptions`

| Property                | Type                                 | Default              | Purpose                                                                                                                                                                                                                                         |
|-------------------------|--------------------------------------|----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Host`                  | `string`                             | — required           | AMQP host name.                                                                                                                                                                                                                                 |
| `Port`                  | `int`                                | `5672`               | AMQP port.                                                                                                                                                                                                                                      |
| `VirtualHost`           | `string`                             | `/`                  | RabbitMQ vhost.                                                                                                                                                                                                                                 |
| `Username` / `Password` | `string`                             | — required           | AMQP and Management API credentials.                                                                                                                                                                                                            |
| `AdminUrl`              | `string`                             | — **required**       | Base URL of the RabbitMQ Management HTTP API (e.g. `http://host:15672`). Used to construct `HttpClient.BaseAddress = "{AdminUrl}/api/"`; `ClearQueue` and `PeekQueueMessages` call into it. The service constructor will throw if this is null. |
| `EnableMetrics`         | `bool`                               | `false`              | When `false`, the injected `IMetrics` is replaced with `NullMetrics.Instance`.                                                                                                                                                                  |
| `ProcessingLimit`       | `int`                                | `0`                  | Maximum concurrent messages processed per queue. `0` means no limit; otherwise a per-queue `SemaphoreSlim` of this size gates dispatch.                                                                                                         |
| `DefinedQueues`         | `IReadOnlyList<string>?`             | `null`               | Queues to declare on `ConnectAsync`.                                                                                                                                                                                                            |
| `ExceptionHandling`     | `MessageProcessingExceptionHandling` | `RequeueOnException` | Strategy applied when a subscribed handler throws.                                                                                                                                                                                              |

## Capabilities mapping

| Abstract call                                                              | RabbitMQ behaviour                                                                                                                                                                                                                   |
|----------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CreateQueue`                                                              | Declares queues with durability / exclusivity / auto-delete flags plus a broker `arguments` dictionary.                                                                                                                              |
| `DeleteQueue`                                                              | Deletes a queue, optionally guarded by `ifUnused` / `ifEmpty`.                                                                                                                                                                       |
| `ClearQueue`                                                               | Purges first via the Management API (`DELETE /api/queues/{vhost}/{name}/contents`), falling back to `QueuePurgeAsync` on the publish channel.                                                                                        |
| `BindQueueToExchange`                                                      | Binds queue ↔ exchange with a routing key.                                                                                                                                                                                           |
| `SendToQueue` / `SendToExchange`                                           | Basic publish on the shared publish channel.                                                                                                                                                                                         |
| `SubscribeToQueue`                                                         | Opens a dedicated channel per subscriber, declares the queue, creates an `AsyncEventingBasicConsumer`, and bridges `BasicConsume`’s `ack/nack/requeue` to the `Func<byte[], Task<bool>>` contract (`true` → requeue, `false` → ack). |
| `PeekQueueMessages`                                                        | Non-destructive read via the Management API (`POST /api/queues/{vhost}/{name}/get` with `ackmode=ack_requeue_true`).                                                                                                                 |
| `CreateExchange` / `DeleteExchange` (RabbitMQ only, on `IRabbitMqService`) | Direct exchange declaration / deletion.                                                                                                                                                                                              |

Because Rabbit features evolve quickly (streams, quorum queues), anything advanced tends to funnel through
`CreateQueue`’s `arguments` bag; inspect `RabbitMqService` for the defaults you rely on before upgrading
`RabbitMQ.Client`.

## Hosted services and workers

Pair with `QueueWorkerBase` from [`Lyo.MessageQueue`](../Lyo.MessageQueue/README.md): workers deserialize JSON
payloads, reuse the envelope helpers, integrate DLQ / `maxRequeueCount`, and drain gracefully on
`IHostedService` shutdown. The publish helper `IMqService.SendToQueueWithEnvelopeAsync` is the recommended
way to publish typed payloads to a queue consumed by such a worker.

Schedulers (`Lyo.Job.Scheduler`) commonly publish triggers here while separate worker processes consume.

## Blazor tooling

[`Lyo.MessageQueue.RabbitMq.Web.Components`](../Lyo.MessageQueue.RabbitMq.Web.Components/README.md) layers a
UI on top of the same service registrations for internal dashboards.

## See also

- [`Lyo.MessageQueue`](../Lyo.MessageQueue/README.md) — the underlying contract, envelopes, and worker base.
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — worker results and the `Metadata["requeue"]` pattern.
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md) — counters and timers emitted by the service and `QueueWorkerBase`.
