# Lyo.Lock

Key-based **exclusive locks** and **keyed semaphores** (bounded concurrency per key) with a small abstraction layer and in-memory implementations for a single process.

## When to use what

| Primitive | Type | Scope | Typical use |
|-----------|------|--------|-------------|
| `ILockService` / `LocalLockService` | Mutex per key | One process | Guard mutations to one aggregate, avoid duplicate work, serialize handlers per entity ID |
| `IKeyedSemaphoreService` / `LocalKeyedSemaphoreService` | Counting semaphore per key | One process | Cap concurrent exports/API calls/backfills *per tenant or resource key* without global rate limits |

For **multiple servers or processes**, register a distributed `ILockService` (see [`Lyo.Lock.Redis`](../Lyo.Lock.Redis/README.md)). Keyed semaphores in this package remain **local only**.

## Features

- **`ILockService`** — acquire/release by string key, or `ExecuteWithLockAsync` helpers that throw `TimeoutException` if the lock is not obtained.
- **`LocalLockService`** — one holder per normalized key using `SemaphoreSlim`.
- **`IKeyedSemaphoreService`** — up to `maxConcurrency` simultaneous permit holders **per key**.
- **`LocalKeyedSemaphoreService`** — per-key `SemaphoreSlim` with ref-counted cleanup when idle.
- **Key normalization** — by default keys are compared case-insensitively (`ToLowerInvariant`); optional skip when keys are already normalized.
- **DI** — `AddLocalLock`, `AddLocalLockFromConfiguration`, `AddLocalKeyedSemaphore`, `AddLocalKeyedSemaphoreFromConfiguration`.
- **Metrics** — optional timers/counters via `Lyo.Metrics` when `EnableMetrics` is true and `IMetrics` is registered.

## Quick start

```csharp
using Lyo.Lock;
using Microsoft.Extensions.DependencyInjection;

services.AddLocalLock();
services.AddLocalKeyedSemaphore();

// Optional: bind appsettings → LockOptions / KeyedSemaphoreOptions
services.AddLocalLockFromConfiguration(configuration);
services.AddLocalKeyedSemaphoreFromConfiguration(configuration);
```

Inject `ILockService` and/or `IKeyedSemaphoreService`:

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

### Rules for keyed semaphores

- Use a **stable `maxConcurrency`** for a given key while any permit is held or waiters exist. If you pass a different `maxConcurrency` for an active key, `LocalKeyedSemaphoreService` throws (`InvalidOperationException`) instead of undefined behavior.
- Cancellation tokens on `AcquireAsync` / `ExecuteAsync` are honored while waiting.

## Configuration

### `LockOptions` (`LockOptions` section)

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultAcquireTimeout` | 30s | Max wait for `AcquireAsync` / `ExecuteWithLockAsync`. |
| `DefaultLockDuration` | 60s | Used by **distributed** locks (Redis TTL). Ignored by `LocalLockService`. |
| `KeyPrefix` | `lyo:lock:` | Prefix for Redis keys; harmless for local-only usage. |
| `SkipKeyNormalization` | `false` | When `true`, keys are not lowercased (caller must ensure consistent casing). |
| `EnableMetrics` | `false` | Record lock timings/counters when `IMetrics` is available. |

### `KeyedSemaphoreOptions` (`KeyedSemaphoreOptions` section)

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultAcquireTimeout` | 30s | Max wait for a permit. |
| `SkipKeyNormalization` | `false` | Same semantics as lock options. |
| `EnableMetrics` | `false` | Record semaphore timings/counters when `IMetrics` is available. |

Example `appsettings.json`:

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

## Metrics (`Constants`)

When metrics are enabled and `IMetrics` is registered, names match `Lyo.Lock.Constants`:

**Locks (`Constants.Metrics`)**

| Name | Role |
|------|------|
| `lock.acquire.duration` | Wait time for acquisition |
| `lock.acquire.success` / `lock.acquire.failure` | Counter |
| `lock.release.duration` | Release timing |
| `lock.execute.duration` | Wall time for `ExecuteWithLockAsync` |

**Semaphores (`Constants.SemaphoreMetrics`)**

| Name | Role |
|------|------|
| `semaphore.acquire.duration` | Wait time for a permit |
| `semaphore.acquire.success` / `semaphore.acquire.failure` | Counter |
| `semaphore.release.duration` | Release timing |
| `semaphore.execute.duration` | Wall time for `ExecuteAsync` |

Tag dimension: `key` — logical key string as passed by the caller (see XML docs on `Constants`).

## API summary

### `ILockService`

- `AcquireAsync` — returns `ILockHandle?` (`null` on timeout).
- `ExecuteWithLockAsync` / `ExecuteWithLockAsync<T>` — acquire, run delegate, release; throw `TimeoutException` if not acquired.

### `ILockHandle` / `IPermitHandle`

- `ReleaseAsync` — idempotent after first release.
- `Dispose` / `DisposeAsync` — release (sync dispose may block briefly on internal `ReleaseAsync`).

### `IKeyedSemaphoreService`

- `AcquireAsync` — returns `IPermitHandle?` on timeout.
- `ExecuteAsync` / `ExecuteAsync<T>` — throw `TimeoutException` if no permit.

## Dependencies

*(Synchronized from `Lyo.Lock.csproj`.)*

**Target frameworks:** `netstandard2.0`, `net10.0`

### NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10.0.1,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10.0.1,)` |

### Project references

- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../Metrics/Lyo.Metrics/README.md)
