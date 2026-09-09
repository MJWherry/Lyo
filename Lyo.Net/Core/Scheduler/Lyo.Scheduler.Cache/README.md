# Lyo.Scheduler.Cache

Cache-backed `ISchedulerStateStore` for [`Lyo.Scheduler`](../Lyo.Scheduler/README.md). Each schedule's `LastRunUtc` / `NextRunUtc` / state markers go through [ `Lyo.Cache`](../../Cache/Lyo.Cache/README.md), so cron/interval/one-shot schedules survive process restarts without extra infrastructure beyond whatever already backs your cache (Fusion in-memory, Postgres, Redis, …).

## Examples

### Wire into DI

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

## Types

| Type | What it does |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `CacheSchedulerStateStore` | An `ISchedulerStateStore` that persists scheduler state via `ICacheService`. Each schedule id gets its own key scope. |
| `SchedulerCacheExtensions` | DI extensions: `AddSchedulerWithCache`. |

## Dependency injection

`AddSchedulerWithCache` is a convenience over `services.AddScheduler(sp => new CacheSchedulerStateStore(sp.GetRequiredService<ICacheService>()), ...)`. Register `ICacheService` first.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Scheduler` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)