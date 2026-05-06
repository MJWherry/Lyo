# Lyo.Lock.Redis

Distributed implementation of `ILockService` using **Redis** and [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis). Use this when multiple app instances must exclude each other on the same logical key.

**Keyed semaphores** (`IKeyedSemaphoreService`) are not implemented here; they remain in-process in [`Lyo.Lock`](../Lyo.Lock/README.md).

## Features

- **Cross-process / cross-host** mutual exclusion on string keys.
- **Acquire** — `SET key token NX PX ttl` (unique token per holder).
- **Release** — Lua script deletes the key only if the value still matches the token (avoids deleting another instance’s lock after expiry or misuse).
- **Waiting** — optional **pub/sub** wakeups on release (`UsePubSubForAcquireWait`) to avoid sleeping on a fixed poll interval; fallback polling uses `AcquirePollInterval`.
- **TTL** — `DefaultLockDuration` / per-call `lockDuration` so crashed processes do not hold keys forever.
- **Shared multiplexer** — use the same `IConnectionMultiplexer` as caching or other Redis consumers.

## Quick start

### Existing `IConnectionMultiplexer`

```csharp
using Lyo.Lock.Redis;
using Microsoft.Extensions.DependencyInjection;

// IConnectionMultiplexer must already be registered (e.g. shared cache setup)
services.AddRedisLock(options =>
{
    options.DefaultAcquireTimeout = TimeSpan.FromSeconds(30);
    options.DefaultLockDuration = TimeSpan.FromSeconds(60);
    options.UsePubSubForAcquireWait = true;
});
```

### Connection string

Registers `IConnectionMultiplexer` with `TryAddSingleton` if missing, then the lock service:

```csharp
services.AddRedisLock("localhost:6379", options =>
{
    options.AcquirePollInterval = TimeSpan.FromMilliseconds(10);
});
```

### Configuration

Binds `LockOptions` from the `LockOptions` section and reads Redis from the `Redis` section (`ConnectionString`, optional `Password`):

```csharp
services.AddRedisLockFromConfiguration(configuration);
```

Custom Redis section name:

```csharp
services.AddRedisLockFromConfiguration(configuration, redisSectionName: "RedisCluster");
```

Throws `InvalidOperationException` if no connection string can be resolved.

Example `appsettings.json`:

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

### `RedisLockOptions` (extends `LockOptions`)

| Property | Default | Description |
|----------|---------|-------------|
| `AcquirePollInterval` | 10 ms | Delay between retries when `UsePubSubForAcquireWait` is `false`. |
| `UsePubSubForAcquireWait` | `true` | Subscribe to a per-key notify channel while waiting; publisher runs on successful Lua delete in `ReleaseAsync`. |

Inherited from `LockOptions`: `DefaultAcquireTimeout`, `DefaultLockDuration`, `KeyPrefix`, `EnableMetrics`, `SkipKeyNormalization`.

## How it works

1. **Redis key** — `KeyPrefix` + normalized logical key (unless normalization is skipped).
2. **Acquire loop** — try `SET` with `NX` and expiry; on failure, either wait on pub/sub with bounded deadline or `Task.Delay(AcquirePollInterval)`.
3. **Notify channel** — separate Redis channel derived from the same prefix and key so waiters can retry promptly after a legitimate release.
4. **Release** — Lua compares stored token to holder’s token; if equal, `DEL` and publish to the notify channel.

## Operational notes

- **TTL vs work duration** — if your critical section can run longer than `lockDuration`, the key may expire and another instance can acquire. Size `DefaultLockDuration` / per-call `lockDuration` above worst-case runtime, or shorten the guarded work.
- **Clocks** — acquire timeout uses `DateTime.UtcNow` on the client for deadline calculation; Redis handles key TTL independently.
- **Fairness** — Redis locks are not strictly FIFO; under contention, which waiter wins is nondeterministic.
- **Metrics** — same names as `Lyo.Lock.Constants.Metrics` when `EnableMetrics` is true (see [`Lyo.Lock` README](../Lyo.Lock/README.md#metrics-constants)).

## Dependencies

*(Synchronized from `Lyo.Lock.Redis.csproj`.)*

**Target frameworks:** `netstandard2.0`, `net10.0`

### NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10.0.1,)` |
| `StackExchange.Redis` | `[2.12,)` |

### Project references

- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Lock`](../Lyo.Lock/README.md)
