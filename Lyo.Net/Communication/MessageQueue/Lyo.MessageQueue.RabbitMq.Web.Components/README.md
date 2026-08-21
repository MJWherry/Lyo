# Lyo.MessageQueue.RabbitMq.Web.Components

Blazor components for RabbitMQ exchanges, bindings, and broker workbenches.

## RabbitMqWorkbench

`RabbitMqWorkbench` is a list-select dashboard for the full `IRabbitMqService` API. Queue and exchange snapshots load on first render. Auto-refresh stays on the queue toolbar.

| Tab | Operations |
| ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Queues** | Type-to-filter dropdown (name + flag chips in the list). **Info** shows durable/state/type chips plus ready/unacked/consumers. **Quick actions** peek/clear/delete/copy. Inner **Send** tab: wrap/priority/delay/send. Inner **Manage** tab: create and bind. |
| **Exchanges** | Type-to-filter dropdown (name + type/durable chips). `amq.*` and the unnamed default are hidden unless Show defaults is on. **Info** and **Quick actions** sit side by side. Inner **Send** tab: wrap/priority/delay/routing key/send. Inner **Manage** tab: create and bind. |

Queue creation supports:

- **`x-max-priority`.** Numeric max priority (same pattern as job workers use for `job.run.{workerType}` queues).
- **DLQ auto-wiring.** `CreateQueueWithDlq` with optional custom DLQ name.

Publishing supports:

- **`SendToQueueWithPriority`.** Envelope publish with priority (requires a priority-enabled queue).
- **`SendToQueueDelayed`.** Broker-side delay via TTL + dead-letter wait queues.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.MessageQueue` (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` (direct, lyo)
- `Lyo.MessageQueue.Web.Components` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `RabbitMQ.Client` `7.2.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)