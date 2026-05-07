# Lyo.MessageQueue.RabbitMq

Concrete **`IMqService`** (**`RabbitMqService`**) using **`RabbitMQ.Client`**, also surfaced as **`IRabbitMqService`** when you need Rabbit-specific knobs not yet elevated to the
shared interface.

## Registration

Use **`SetupRabbitMqService`** or **`SetupRabbitMqServiceFromConfiguration`** (**`Extensions`**):

1. **`RabbitMqOptions`** singleton (explicit `Action<RabbitMqOptions>` or **bound** from configuration section default **`RabbitMqOptions.SectionName`**).
2. **`IConnectionFactory`** singleton with:
    - Host / vhost / port / credentials from options.
    - **`ClientProvidedName`** set to **`MachineName - ApplicationName (EnvironmentName)`** for traceability in Rabbit management UI.
    - Optional **`ClientProperties`** dictionary (pass rich connection metadata—container id, build sha, etc.).
3. **`RabbitMqService`** registered as **singleton** implementing both **`IRabbitMqService`** and **`IMqService`**.

> **Note:** The helper **throws** if `connectionProperties` null in the configuration overload—pass an empty dictionary (`[]`) rather than omitting when you have no extras.

Health checks reuse the **`IMqService : IHealth`** contract from the abstraction package.

## Capabilities mapping

Translates **`IMqService`** verbs to RabbitMQ primitives:

| Abstract call         | Rabbit behavior (high level)                                                                                                           |
|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| `CreateQueue`         | Declares queues with durability/exclusivity/auto-delete flags + broker arguments dictionary.                                           |
| `BindQueueToExchange` | Queue ↔ exchange routing key binding (`topic`, `direct`, etc.—depends how you declared the exchange externally).                       |
| `SendToExchange`      | Basic publish with routing key payload bytes.                                                                                          |
| `SubscribeToQueue`    | Consumer with manual ACK/NACK equivalents; **`Task<bool>`** return maps to retry/requeue semantics expected by **`Lyo.MessageQueue`**. |

Peek/clear/delete mirror broker capabilities guarded by Rabbit permissions.

Because Rabbit features evolve quickly (streams, quorum queues), **anything advanced** tends to funnel through **`CreateQueue`**’s **`arguments`** bag—inspect `RabbitMqService`
sources for defaults you rely on before upgrading `RabbitMQ.Client`.

## Hosted services & workers

Pair with **`QueueWorkerBase`** from [`Lyo.MessageQueue`](../Lyo.MessageQueue/README.md): workers deserialize JSON payloads, reuse envelope helpers, integrate DLQ/
`maxRequeueCount`, drain gracefully on **`IHostedService`** shutdown.

Schedulers (**`Lyo.Job.Scheduler`**) commonly publish triggers here while separate worker processes consume.

## Blazor tooling

[`Lyo.MessageQueue.RabbitMq.Web.Components`](../Lyo.MessageQueue.RabbitMq.Web.Components/README.md) layers UI atop the same service registrations for internal dashboards.

## See also

- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — worker results + requeue metadata patterns.
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md) — queue depth / processing timers in **`QueueWorkerBase`**.
