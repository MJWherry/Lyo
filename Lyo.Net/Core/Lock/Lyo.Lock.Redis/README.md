# Lyo.Lock.Redis

Redis-backed `ILockService` through [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis). Reach for this when several app instances must exclude each other on one logical key.

Keyed semaphores (`IKeyedSemaphoreService`) are not implemented here. They stay in-process in [`Lyo.Lock`](../Lyo.Lock/README.md).

## Features

- **Cross-process / cross-host.** String keys for mutual exclusion.
- **Acquire.** `SET key token NX PX ttl` (one unique token per holder).
- **Release.** A Lua script deletes the key only while the stored value still matches the token (so another instance's lock is not deleted after expiry or misuse).
- **Waiting.** Optional pub/sub wakeups on release (`UsePubSubForAcquireWait`) so waiters are not stuck on a fixed poll interval. Fallback polling uses `AcquirePollInterval`.
- **TTL.** `DefaultLockDuration` / per-call `lockDuration` so a crashed process cannot hold a key forever.
- **Shared multiplexer.** Reuse the same `IConnectionMultiplexer` as caching or other Redis consumers.

## Examples

### Reuse an `IConnectionMultiplexer`

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

### From a connection string

```csharp
services.AddRedisLock("localhost:6379", options =>
{
    options.AcquirePollInterval = TimeSpan.FromMilliseconds(10);
});
```

### From configuration

```csharp
services.AddRedisLockFromConfiguration(configuration);
```

### From configuration (2)

```csharp
services.AddRedisLockFromConfiguration(configuration, redisSectionName: "RedisCluster");
```

### From configuration (3)

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

## From a connection string

Registers `IConnectionMultiplexer` with `TryAddSingleton` if it is missing, then the lock service:

## From configuration

Binds `LockOptions` from the `LockOptions` section and reads Redis from the `Redis` section (`ConnectionString`, optional `Password`). A custom Redis section name throws `ConfigurationException` (from `Lyo.Exceptions`) if no connection string can be resolved. Example `appsettings.json`:

## `RedisLockOptions` (builds on `LockOptions`)

| Property | Default | What it controls |
| ------------------------- | ------- | ---------------------------------------------------------------------------------------------------------- |
| `AcquirePollInterval` | 10 ms | Retry delay when `UsePubSubForAcquireWait` is `false`. |
| `UsePubSubForAcquireWait` | `true` | While waiting, subscribe to a per-key notify channel. A successful Lua delete in `ReleaseAsync` publishes. |

From `LockOptions`: `DefaultLockDuration`, `DefaultAcquireTimeout`, `KeyPrefix`, `EnableMetrics`, `SkipKeyNormalization`.

## Internals

- **Redis key.** The normalized logical key plus `KeyPrefix` (unless normalization is skipped).
- **Acquire loop.** Attempt `SET` with `NX` and expiry. On failure, wait on pub/sub with a bounded deadline, or `Task.Delay(AcquirePollInterval)`.
- **Notify channel.** A separate Redis channel from the same prefix and key so waiters can retry promptly after a legitimate release.
- **Release.** Lua compares the holder's token to the stored token. On match, `DEL` and publish to the notify channel.

## Ops notes

- **TTL vs work duration.** Another instance can acquire if the key expires while the critical section still runs longer than `lockDuration`. Size `DefaultLockDuration` / per-call `lockDuration` above worst-case runtime, or shorten the guarded work.
- **Clocks.** The client deadline uses `DateTime.UtcNow` for acquire timeout. Redis handles key TTL independently.
- **Fairness.** These Redis locks are not strictly FIFO. Under contention, which waiter wins is nondeterministic.
- **Metrics.** Same names as `Lyo.Lock.Constants.Metrics` when `EnableMetrics` is true (see [`Lyo.Lock` README](../Lyo.Lock/README.md#metrics-constants)).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Lock` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `StackExchange.Redis` `2.12.0` (direct, third-party)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)