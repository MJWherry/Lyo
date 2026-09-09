# Lyo.Lock

Exclusive locks by key and keyed semaphores (bounded concurrency per key), plus in-memory implementations for a single process.

## Features

- **ILockService.** Acquire/release by string key, or `ExecuteWithLockAsync` helpers that throw `TimeoutException` if the lock is not obtained.
- **LocalLockService.** `SemaphoreSlim` gives one holder per normalized key.
- **IKeyedSemaphoreService.** Up to `maxConcurrency` simultaneous permit holders per key.
- **LocalKeyedSemaphoreService.** Per-key `SemaphoreSlim` with ref-counted cleanup when idle.
- **Key normalization.** By default keys are compared case-insensitively (`ToLowerInvariant`). Set skip when keys are already normalized.
- **DI.** `AddLocalLock`, `AddLocalLockFromConfiguration`, `AddLocalKeyedSemaphore`, `AddLocalKeyedSemaphoreFromConfiguration`.
- **Metrics.** Optional timers/counters via `Lyo.Metrics` when `EnableMetrics` is true and `IMetrics` is registered.

## Examples

### First steps

```csharp
using Lyo.Lock;
using Microsoft.Extensions.DependencyInjection;

services.AddLocalLock();
services.AddLocalKeyedSemaphore();

// Optional: bind appsettings → LockOptions / KeyedSemaphoreOptions
services.AddLocalLockFromConfiguration(configuration);
services.AddLocalKeyedSemaphoreFromConfiguration(configuration);
```

### First steps (2)

```csharp
// Exclusive lock: null if timeout
var handle = await lockService.AcquireAsync("order:123", timeout: TimeSpan.FromSeconds(5));
if (handle is not null)
{
    await using (handle)
        await ProcessOrderAsync();
}

// Throws TimeoutException if not acquired
await lockService.ExecuteWithLockAsync("order:123", async ct => await ProcessOrderAsync(ct));

// Up to 3 concurrent operations for the same key (same process only)
await semaphoreService.ExecuteAsync("export:tenant-1", 3, async ct => await RunExportAsync(ct));
```

### `KeyedSemaphoreOptions` (section `KeyedSemaphoreOptions`)

```json
{
  "LockOptions": {
    "DefaultAcquireTimeout": "00:00:30",
    "DefaultLockDuration": "00:01:00",
    "KeyPrefix": "lyo:lock:",
    "EnableMetrics": false,
    "SkipKeyNormalization": false
  },
  "KeyedSemaphoreOptions": {
    "DefaultAcquireTimeout": "00:00:30",
    "SkipKeyNormalization": false,
    "EnableMetrics": false
  }
}
```

## Benchmarks

- Portfolio suite: `lock`

## Which one to pick

| Primitive | Type | Scope | Typical use |
| ------------------------------------------------------- | -------------------------- | -------------- | -------------------------------------------------------------------------------------------------- |
| `ILockService` / `LocalLockService` | Per-key mutex | Single process | Guard mutations to one aggregate, avoid duplicate work, serialize handlers per entity ID |
| `IKeyedSemaphoreService` / `LocalKeyedSemaphoreService` | Per-key counting semaphore | Single process | Cap concurrent exports/API calls/backfills *per tenant or resource key* without global rate limits |

More than one server or process needs a distributed `ILockService` (see [`Lyo.Lock.Redis`](../Lyo.Lock.Redis/README.md)). Keyed semaphores in this package stay process-local.

## First steps

Inject `IKeyedSemaphoreService` and/or `ILockService`:

## Keyed-semaphore rules

- Keep `maxConcurrency` stable for a given key while any permit is held or waiters exist. If you pass a different `maxConcurrency` for an active key, `LocalKeyedSemaphoreService` throws `InvalidOperationException` instead of undefined behavior.
- Cancellation tokens on `ExecuteAsync` / `AcquireAsync` are honored while waiting.

## `LockOptions` (`LockOptions` section)

| Property | Default | Description |
| ----------------------- | ----------- | ---------------------------------------------------------------------------- |
| `DefaultAcquireTimeout` | 30s | Ceiling wait for `ExecuteWithLockAsync` / `AcquireAsync`. |
| `DefaultLockDuration` | 60s | Used by distributed locks (Redis TTL). Ignored by `LocalLockService`. |
| `KeyPrefix` | `lyo:lock:` | Prefix for Redis keys; harmless for local-only usage. |
| `SkipKeyNormalization` | `false` | When `true`, keys are not lowercased (caller must ensure consistent casing). |
| `EnableMetrics` | `false` | Record lock timings/counters when `IMetrics` is available. |

## `KeyedSemaphoreOptions` (section `KeyedSemaphoreOptions`)

| Property | Default | Description |
| ----------------------- | ------- | --------------------------------------------------------------- |
| `DefaultAcquireTimeout` | 30s | Ceiling wait for a permit. |
| `SkipKeyNormalization` | `false` | Same semantics as lock options. |
| `EnableMetrics` | `false` | Record semaphore timings/counters when `IMetrics` is available. |

Example `appsettings.json`:

## Metrics (`Constants`)

With metrics on and `IMetrics` in the container, names follow `Lyo.Lock.Constants`:

**Locks (`Constants.Metrics`)**

| Name | Role |
| ----------------------------------------------- | ------------------------------------ |
| `lock.acquire.duration` | Time spent waiting to acquire |
| `lock.acquire.success` / `lock.acquire.failure` | Counter |
| `lock.release.duration` | Time to release |
| `lock.execute.duration` | Wall time for `ExecuteWithLockAsync` |

**Semaphores (`Constants.SemaphoreMetrics`)**

| Name | Role |
|-----------------------------------------------------------|------------------------------|
| `semaphore.acquire.duration` | Wait time for a permit |
| `semaphore.acquire.success` / `semaphore.acquire.failure` | Counter |
| `semaphore.release.duration` | Release timing |
| `semaphore.execute.duration` | Wall time for `ExecuteAsync` |

The `key` tag is the logical key string the caller passed (XML docs on `Constants` describe it).

## Methods on ILockService

- **AcquireAsync.** Returns `ILockHandle?` (`null` on timeout).
- **ExecuteWithLockAsync / ExecuteWithLockAsync<T>.** Acquire, run the delegate, release. Throw `TimeoutException` if not acquired.

## Methods on ILockHandle / IPermitHandle

- **ReleaseAsync.** Idempotent after the first release.
- **Dispose / DisposeAsync.** Release. Sync dispose may block briefly on the internal `ReleaseAsync`.

## Methods on IKeyedSemaphoreService

- **AcquireAsync.** Returns `IPermitHandle?` on timeout.
- **ExecuteAsync / ExecuteAsync<T>.** Throw `TimeoutException` if no permit.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)