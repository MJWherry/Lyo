# Lyo.MessageQueue.RabbitMq.Web.Components

Reusable Blazor components for RabbitMQ-specific exchanges, bindings, and broker workbenches.

## RabbitMqWorkbench

`RabbitMqWorkbench.razor` is an internal dashboard for exercising the full `IRabbitMqService` surface:

| Tab | Operations |
| ------------ | ---------------------------------------------------------------------------------------------------------- |
| **Exchange** | Create/delete exchanges, bind queues |
| **Queue** | Create (optionally with DLQ + `x-max-priority`), peek, clear, delete; single-queue info via management API |
| **Publish** | Send to queue/exchange (envelope wrap, priority, delayed delivery) |
| **Stats** | All-queue statistics with optional name filter, manual refresh, and auto-refresh polling |

Queue creation supports:

- **`x-max-priority`** — numeric max priority (same pattern as job workers use for `job.run.{workerType}` queues).
- **DLQ auto-wiring** — `CreateQueueWithDlq` with optional custom DLQ name.

Publishing supports:

- **`SendToQueueWithPriority`** / envelope publish with priority (requires a priority-enabled queue).
- **`SendToQueueDelayed`** — broker-side delay via TTL + dead-letter wait queues.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.MessageQueue` — (direct, lyo)
- `Lyo.MessageQueue.RabbitMq` — (direct, lyo)
- `Lyo.MessageQueue.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `RabbitMQ.Client` `7.2.1` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)