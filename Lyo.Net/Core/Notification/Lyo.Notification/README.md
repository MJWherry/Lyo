# Lyo.Notification

Small domain events via in-process publish/subscribe. Not distributed, not durable, and not ordered across machines. Only useful when every publisher and handler lives in the same DI container (typical worker or ASP.NET Core host). For cross-service messaging use `Lyo.MessageQueue` (RabbitMQ, and similar brokers).

## Features

- `AddLyoNotification` registers `INotificationPublisher` as `NotificationPublisher` (singleton). The publisher captures `IServiceProvider` so each publish can resolve the current handler set. Scoped handlers work only if you resolve `INotificationPublisher` from the same scope that contains those handlers, which is fragile. Prefer transient or singleton handlers, or resolve handlers explicitly in tests.

## Examples

### Register in DI

```csharp
using Lyo.Notification;

builder.Services.AddLyoNotification();
builder.Services.AddSingleton<INotificationHandler<OrderPlacedNotification>, SendEmail>();
builder.Services.AddSingleton<INotificationHandler<OrderPlacedNotification>, UpdateInventoryProjection>();
```

### Publishing

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

## Why the package exists

Use this when a feature should fire in-process side effects without knowing each handler. MediatR-style pipelines (open generics, behaviors, pipeline ordering) are out of scope. You get a marker type, one or more handlers per notification, and a publisher that resolves handlers from DI and awaits them sequentially.

## Types at the core

| Type | Role |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `INotification` | Marker interface. Your event DTO implements it (can be a `record` with whatever payload you need). |
| `INotificationHandler<TNotification>` | `HandleAsync(TNotification, CancellationToken)`. Many handlers can be registered for the same `TNotification`. All are invoked. |
| `INotificationPublisher` | `PublishAsync<T>(T, CancellationToken)` dispatches to every registered handler of that `T`. |
| `NotificationPublisher` | Default implementation: `GetServices<INotificationHandler<T>>()`, then await each handler in registration order. |

There is no built-in only-one-handler or stop-on-first-handler rule. Every matching handler runs every time unless you unregister it.

## Registration

- `AddLyoNotification` registers `INotificationPublisher` as `NotificationPublisher` (singleton). The publisher captures `IServiceProvider` so each publish can resolve the current handler set. Scoped handlers work only if you resolve `INotificationPublisher` from the same scope that contains those handlers, which is fragile. Prefer transient or singleton handlers, or resolve handlers explicitly in tests.

## Publishing

Handlers run one after another; `PublishAsync` awaits each in sequence. There is no parallel fan-out.

## What happens on errors

`NotificationPublisher` wraps each handler in try/catch. On exception, the error is logged at Error level (notification type and handler type in the structured log payload). Remaining handlers still run. Failures do not abort the publisher or rethrow. Notifications are best-effort side effects: log and continue. If you need fail-closed behavior or transactional semantics, call handlers explicitly or wrap `PublishAsync` yourself. `CancellationToken` is passed through to `HandleAsync`. If cancelled mid-loop, handlers that observe the token stop. Handlers already running complete unless they cancel internally.

## When to skip this package

- Cross-pod or cross-process events → message bus.
- Guaranteed delivery / dead-letter / retries → queue + outbox patterns.
- Pipelines that must run middleware in order across all handlers → mediator library.
- You need mediator request/response (query objects with return values) → this is publish-only (`Task`, no aggregate return value from `PublishAsync`).

## Related reading

- Broker-backed messaging: [`Lyo.MessageQueue`](../../../Communication/MessageQueue/Lyo.MessageQueue/README.md).
- Example integration host that pulls in diff and other utilities: [`Lyo.Discord.Bot`](../../../Integration/Discord/Lyo.Discord.Bot/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)