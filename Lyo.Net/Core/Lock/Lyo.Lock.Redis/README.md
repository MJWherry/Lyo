# Lyo.Lock.Redis

Redis-based distributed lock implementation for Lyo.Lock. Uses StackExchange.Redis for multi-instance coordination across processes and servers.

## Features

- **Distributed locking** – Coordinate across multiple app instances via Redis
- **SET NX + Lua release** – Standard Redis lock pattern with token-based release
- **Pub/sub optimization** – Uses Redis pub/sub to wake waiters immediately when a lock is released (reduces latency vs polling)
- **Auto-expiry** – Locks auto-release if the process crashes (configurable duration)
- **Shares Redis connection** – Can use the same `IConnectionMultiplexer` as Lyo.Cache.Fusion

## Quick Start

### With Existing Redis Connection

```csharp
using Lyo.Lock.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

// If IConnectionMultiplexer is already registered (e.g. via Lyo.Cache.Fusion)
services.AddRedisLock();

// Or with options
services.AddRedisLock(options =>
{
    options.DefaultAcquireTimeout = TimeSpan.FromSeconds(30);
    options.DefaultLockDuration = TimeSpan.FromSeconds(60);
    options.UsePubSubForAcquireWait = true;
});
```

### With Connection String

```csharp
services.AddRedisLock("localhost:6379");

// Or with options
services.AddRedisLock("localhost:6379", options =>
{
    options.AcquirePollInterval = TimeSpan.FromMilliseconds(10);
});
```

### From Configuration

```csharp
services.AddRedisLock(configuration);
```

Expects `Redis` section with `ConnectionString` and optional `LockOptions` section:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Password": "optional-password"
  },
  "LockOptions": {
    "DefaultAcquireTimeout": "00:00:30",
    "DefaultLockDuration": "00:01:00",
    "KeyPrefix": "lyo:lock:",
    "AcquirePollInterval": "00:00:00.010",
    "UsePubSubForAcquireWait": true,
    "EnableMetrics": false,
    "SkipKeyNormalization": false
  }
}
```

## Configuration

### RedisLockOptions (extends LockOptions)

| Property                  | Default | Description                                                                          |
|---------------------------|---------|--------------------------------------------------------------------------------------|
| `AcquirePollInterval`     | 10ms    | Interval between retries when polling (only when `UsePubSubForAcquireWait` is false) |
| `UsePubSubForAcquireWait` | `true`  | Use Redis pub/sub to wake waiters on release instead of polling                      |
| `SkipKeyNormalization`    | `false` | Skip `ToLowerInvariant()` on keys                                                    |

Inherited from `LockOptions`: `DefaultAcquireTimeout`, `DefaultLockDuration`, `KeyPrefix`, `EnableMetrics`.

## How It Works

1. **Acquire**: `SET key token NX PX duration` – sets the lock with a unique token and expiry
2. **Release**: Lua script verifies token and deletes only if it matches (prevents releasing another holder's lock)
3. **Waiters**: When `UsePubSubForAcquireWait` is true, waiters subscribe to a per-key channel and are notified immediately when the lock is released

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Lock.Redis.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10.0.1,)` |
| `StackExchange.Redis` | `[2.12,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Lock`

## Public API (generated)

Top-level `public` types in `*.cs` (*3*). Nested types and file-scoped namespaces may omit some entries.

- `RedisLockOptions`
- `RedisLockService`
- `RedisLockServiceExtensions`

<!-- LYO_README_SYNC:END -->

