# Lyo.MessageQueue.RabbitMq.Web.Components

Reusable Blazor components for RabbitMQ-specific exchanges, bindings, and broker workbenches.

## RabbitMqWorkbench

`RabbitMqWorkbench.razor` is an internal dashboard for exercising the full `IRabbitMqService` surface:

| Tab          | Operations                                                                                                 |
|--------------|------------------------------------------------------------------------------------------------------------|
| **Exchange** | Create/delete exchanges, bind queues                                                                       |
| **Queue**    | Create (optionally with DLQ + `x-max-priority`), peek, clear, delete; single-queue info via management API |
| **Publish**  | Send to queue/exchange (envelope wrap, priority, delayed delivery)                                         |
| **Stats**    | All-queue statistics with optional name filter, manual refresh, and auto-refresh polling                   |

Queue creation supports:

- **`x-max-priority`** — numeric max priority (same pattern as job workers use for `job.run.{workerType}` queues).
- **DLQ auto-wiring** — `CreateQueueWithDlq` with optional custom DLQ name.

Publishing supports:

- **`SendToQueueWithPriority`** / envelope publish with priority (requires a priority-enabled queue).
- **`SendToQueueDelayed`** — broker-side delay via TTL + dead-letter wait queues.

## Related projects

- [`Lyo.MessageQueue.RabbitMq`](../Lyo.MessageQueue.RabbitMq/README.md)
- [`Lyo.MessageQueue`](../Lyo.MessageQueue/README.md)
- [`Lyo.MessageQueue.Web.Components`](../Lyo.MessageQueue.Web.Components/README.md)
