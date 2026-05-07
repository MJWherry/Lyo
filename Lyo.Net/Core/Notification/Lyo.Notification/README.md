# Lyo.Notification

In-process **publish/subscribe** for small domain events. It is **not** durable, **not** distributed, and **not** ordered across machines—only useful when every publisher and
handler lives in the **same DI container** (typical ASP.NET Core host or worker). For cross-service messaging use **`Lyo.MessageQueue`** (RabbitMQ, etc.).

## Why this exists

Sometimes you only need: *“when X happens, run these side effects in-process without the feature knowing about each handler.”* Full MediatR-style pipelines (behaviors, open
generics, pipeline ordering) are intentionally **out of scope**. You get a **marker type**, **one or more handlers per notification**, and a **publisher** that resolves handlers
from DI and awaits them **sequentially**.

## Core types

| Type                                      | Role                                                                                                                                |
|-------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| **`INotification`**                       | Marker interface. Your event DTO implements it (can be a `record` with whatever payload you need).                                  |
| **`INotificationHandler<TNotification>`** | `HandleAsync(TNotification, CancellationToken)`. **Many** handlers can be registered for the same `TNotification`; all are invoked. |
| **`INotificationPublisher`**              | `PublishAsync<T>(T, CancellationToken)` dispatches to every registered handler of that `T`.                                         |
| **`NotificationPublisher`**               | Default implementation: `GetServices<INotificationHandler<T>>()`, then **`await` each handler in registration order**.              |

There is **no** built-in “stop on first handler” or “only one handler” rule—every matching handler runs every time unless you unregister it.

## Registration

```csharp
using Lyo.Notification;

builder.Services.AddLyoNotification();
builder.Services.AddSingleton<INotificationHandler<OrderPlacedNotification>, SendEmail>();
builder.Services.AddSingleton<INotificationHandler<OrderPlacedNotification>, UpdateInventoryProjection>();
```

- **`AddLyoNotification`** registers **`INotificationPublisher`** as **`NotificationPublisher`** (**singleton**). The publisher captures **`IServiceProvider`** so each publish can
  resolve the current handler set (supports scoped handlers **only if** you resolve **`INotificationPublisher`** from the same scope that contains scoped handlers—which is fragile;
  prefer **singleton/transient** handlers or resolve handlers explicitly in tests).

Handlers are ordinary DI services—lifetime is entirely up to you (`Singleton`, `Scoped`, `Transient`).

## Publishing

```csharp
public class CheckoutService(INotificationPublisher bus)
{
    public async Task CompleteAsync(Guid orderId, CancellationToken ct)
    {
        // ... persistence ...
        await bus.PublishAsync(new OrderPlacedNotification(orderId), ct);
    }
}
```

Publish is **`async void`-free**: `PublishAsync` **awaits each handler sequentially**. There is **no** parallel fan-out built in.

## Error behavior (important)

Inside **`NotificationPublisher`**, each handler is wrapped in **try/catch**:

- On exception: the error is **logged** at **Error** level (handler type + notification type in the structured log payload).
- The **remaining handlers still run**—failures do **not** abort the publisher or rethrow.

So notifications are **best-effort** side effects: logging + continue. If you need **transactional** semantics or **fail closed** behavior, call handlers explicitly or wrap
`PublishAsync` yourself.

Cancellation: **`CancellationToken`** is passed through to **`HandleAsync`**. If cancelled mid-loop, handlers that observe the token stop; handlers already running complete unless
they cancel internally.

## When **not** to use this

- Cross-process or cross-pod events → message bus.
- Guaranteed delivery / retries / dead-letter → queue + outbox patterns.
- Pipelines that must run middleware in order across all handlers → mediator library.
- You need mediator request/response (query objects with return values) → this is publish-only (`Task`, no aggregate return value from `PublishAsync`).

## See also

- [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md) — broker-backed messaging.
- [`Lyo.Discord.Bot`](../../../Integration/Discord/Lyo.Discord.Bot/README.md) — example integration host that pulls in diff and other utilities.
