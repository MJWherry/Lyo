# Lyo.Scheduler.Cache

Cache-backed `ISchedulerStateStore` for [`Lyo.Scheduler`](../Lyo.Scheduler/README.md). Persists each schedule's `LastRunUtc` / `NextRunUtc` / state markers through [
`Lyo.Cache`](../../Cache/Lyo.Cache/README.md) so cron/interval/one-shot schedules survive process restarts without external infrastructure beyond whatever backs your cache (Fusion
in-memory, Postgres, Redis, …).

## Types

| Type                           | Role                                                                                                                                  |
|--------------------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| **`CacheSchedulerStateStore`** | `ISchedulerStateStore` implementation that reads and writes scheduler state through `ICacheService`. Keys are scoped per schedule id. |
| **`SchedulerCacheExtensions`** | DI extensions.                                                                                                                        |

## DI

```csharp
using Lyo.Scheduler.Cache;

// Register cache first (e.g. via Lyo.Cache.Fusion):
services.AddFusionCache(); // or whichever ICacheService registration suits the host

// Then register the scheduler with cache persistence:
services.AddSchedulerWithCache(o =>
{
    o.CheckIntervalMs = 5_000;
});
```

`AddSchedulerWithCache` is a convenience over `services.AddScheduler(sp => new CacheSchedulerStateStore(sp.GetRequiredService<ICacheService>()), ...)`. It requires `ICacheService`
to be registered beforehand.

## Related projects

- [`Lyo.Cache`](../../Cache/Lyo.Cache/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Scheduler`](../Lyo.Scheduler/README.md)
