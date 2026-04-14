# Lyo.Lock

Centralized synchronization primitives with local in-memory implementations. Provides `ILockService` for exclusive locks and `IKeyedSemaphoreService` for bounded concurrency by
key.

## Features

- **Provider-agnostic** – `ILockService` abstraction with pluggable implementations
- **LocalLockService** – In-memory, per-process locking using `SemaphoreSlim` per key
- **LocalKeyedSemaphoreService** – In-memory, per-process bounded concurrency using `SemaphoreSlim` per key
- **Key-based locking** – Lock by string key (e.g. `"order:123"`, `"user:456:profile"`)
- **Dependency injection** – First-class DI support with configuration binding
- **Optional metrics** – Integrates with Lyo.Metrics for acquire/release/execute timings

## Quick Start

### Local Lock (Single Process)

```csharp
using Lyo.Lock;
using Microsoft.Extensions.DependencyInjection;

// Register local lock service
services.AddLocalLock();

// Register local keyed semaphore service
services.AddLocalKeyedSemaphore();

// Or with options
services.AddLocalLock(options =>
{
    options.DefaultAcquireTimeout = TimeSpan.FromSeconds(30);
    options.EnableMetrics = true;
});

// Or from configuration (binds to "LockOptions" section)
services.AddLocalLock(configuration);
```

### Usage

```csharp
// Acquire and release manually
var handle = await lockService.AcquireAsync("order:123", timeout: TimeSpan.FromSeconds(5));
if (handle != null)
{
    try
    {
        await ProcessOrderAsync();
    }
    finally
    {
        await handle.ReleaseAsync();
    }
}

// Or use ExecuteWithLockAsync (throws if lock cannot be acquired)
await lockService.ExecuteWithLockAsync("order:123", async ct =>
{
    await ProcessOrderAsync();
});

// With return value
var result = await lockService.ExecuteWithLockAsync("order:123", async ct =>
{
    return await FetchOrderAsync();
});

// Allow up to 3 concurrent executions for a key
await semaphoreService.ExecuteAsync("order:123", 3, async ct =>
{
    await ProcessOrderChunkAsync();
});
```

## Configuration

### LockOptions

| Property                | Default       | Description                                               |
|-------------------------|---------------|-----------------------------------------------------------|
| `DefaultAcquireTimeout` | 30s           | Maximum time to wait for a lock                           |
| `DefaultLockDuration`   | 60s           | Auto-release duration for distributed locks               |
| `KeyPrefix`             | `"lyo:lock:"` | Prefix for lock keys (distributed)                        |
| `EnableMetrics`         | `false`       | Enable metrics collection                                 |
| `SkipKeyNormalization`  | `false`       | Skip `ToLowerInvariant()` on keys when already normalized |

Example `appsettings.json`:

```json
{
  "LockOptions": {
    "DefaultAcquireTimeout": "00:00:30",
    "DefaultLockDuration": "00:01:00",
    "KeyPrefix": "lyo:lock:",
    "EnableMetrics": false,
    "SkipKeyNormalization": false
  }
}
```

### KeyedSemaphoreOptions

| Property                | Default | Description                                               |
|-------------------------|---------|-----------------------------------------------------------|
| `DefaultAcquireTimeout` | 30s     | Maximum time to wait for a permit                         |
| `SkipKeyNormalization`  | `false` | Skip `ToLowerInvariant()` on keys when already normalized |
| `EnableMetrics`         | `false` | Enable metrics collection                                 |

Example `appsettings.json`:

```json
{
  "KeyedSemaphoreOptions": {
    "DefaultAcquireTimeout": "00:00:30",
    "SkipKeyNormalization": false,
    "EnableMetrics": false
  }
}
```

## API

- `AcquireAsync(key, timeout?, lockDuration?, ct)` – Acquire lock, returns `ILockHandle?` or null on timeout
- `ExecuteWithLockAsync(key, action, ...)` – Execute action while holding lock; throws on timeout
- `ExecuteWithLockAsync<T>(key, func, ...)` – Execute function and return result
- `AcquireAsync(key, maxConcurrency, timeout?, ct)` – Acquire semaphore permit, returns `IPermitHandle?` or null on timeout
- `ExecuteAsync(key, maxConcurrency, action, ...)` – Execute action while holding a permit; throws on timeout

### ILockHandle

- `ReleaseAsync()` – Release the lock
- `Dispose()` / `DisposeAsync()` – Release on dispose

### IPermitHandle

- `ReleaseAsync()` – Release the permit
- `Dispose()` / `DisposeAsync()` – Release on dispose

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Lock.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10.0.1,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10.0.1,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*13*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `IKeyedSemaphoreService`
- `ILockHandle`
- `ILockService`
- `IPermitHandle`
- `KeyedSemaphoreOptions`
- `LocalKeyedSemaphoreService`
- `LocalLockService`
- `LockOptions`
- `LockServiceExtensions`
- `Metrics`
- `SemaphoreMetrics`
- `Tags`

<!-- LYO_README_SYNC:END -->

