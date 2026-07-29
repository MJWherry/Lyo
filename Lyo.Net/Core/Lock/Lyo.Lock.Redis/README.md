# Lyo.Lock.Redis

Distributed implementation of `ILockService` using **Redis** and [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis). Use this when multiple app instances
must exclude each other on the same logical key.

**Keyed semaphores** (`IKeyedSemaphoreService`) are not implemented here; they remain in-process in [`Lyo.Lock`](../Lyo.Lock/README.md).

## Features

- **Cross-process / cross-host** mutual exclusion on string keys.
- **Acquire** — `SET key token NX PX ttl` (unique token per holder).
- **Release** — Lua script deletes the key only if the value still matches the token (avoids deleting another instance’s lock after expiry or misuse).
- **Waiting** — optional **pub/sub** wakeups on release (`UsePubSubForAcquireWait`) to avoid sleeping on a fixed poll interval; fallback polling uses `AcquirePollInterval`.
- **TTL** — `DefaultLockDuration` / per-call `lockDuration` so crashed processes do not hold keys forever.
- **Shared multiplexer** — use the same `IConnectionMultiplexer` as caching or other Redis consumers.

## Examples

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

```csharp
services.AddRedisLock("localhost:6379", options =>
{
    options.AcquirePollInterval = TimeSpan.FromMilliseconds(10);
});
```

### Configuration

```csharp
services.AddRedisLockFromConfiguration(configuration);
```

### Configuration (2)

```csharp
services.AddRedisLockFromConfiguration(configuration, redisSectionName: "RedisCluster");
```

### Configuration (3)

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

## Connection string

Registers `IConnectionMultiplexer` with `TryAddSingleton` if missing, then the lock service:

## Configuration

Binds `LockOptions` from the `LockOptions` section and reads Redis from the `Redis` section (`ConnectionString`, optional `Password`): Custom Redis section name: Throws `ConfigurationException` (from `Lyo.Exceptions`) if no connection string can be resolved. Example `appsettings.json`:

## `RedisLockOptions` (extends `LockOptions`)

| Property | Default | Description |
| ------------------------- | ------- | --------------------------------------------------------------------------------------------------------------- |
| `AcquirePollInterval` | 10 ms | Delay between retries when `UsePubSubForAcquireWait` is `false`. |
| `UsePubSubForAcquireWait` | `true` | Subscribe to a per-key notify channel while waiting; publisher runs on successful Lua delete in `ReleaseAsync`. |

Inherited from `LockOptions`: `DefaultAcquireTimeout`, `DefaultLockDuration`, `KeyPrefix`, `EnableMetrics`, `SkipKeyNormalization`.

## How it works

- **Redis key** — `KeyPrefix` + normalized logical key (unless normalization is skipped).
- **Acquire loop** — try `SET` with `NX` and expiry; on failure, either wait on pub/sub with bounded deadline or `Task.Delay(AcquirePollInterval)`.
- **Notify channel** — separate Redis channel derived from the same prefix and key so waiters can retry promptly after a legitimate release.
- **Release** — Lua compares stored token to holder’s token; if equal, `DEL` and publish to the notify channel.

## Operational notes

- **TTL vs work duration** — if your critical section can run longer than `lockDuration`, the key may expire and another instance can acquire. Size `DefaultLockDuration` / per-call `lockDuration` above worst-case runtime, or shorten the guarded work.
- **Clocks** — acquire timeout uses `DateTime.UtcNow` on the client for deadline calculation; Redis handles key TTL independently.
- **Fairness** — Redis locks are not strictly FIFO; under contention, which waiter wins is nondeterministic.
- **Metrics** — same names as `Lyo.Lock.Constants.Metrics` when `EnableMetrics` is true (see [`Lyo.Lock` README](../Lyo.Lock/README.md#metrics-constants)).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Lock` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `StackExchange.Redis` `2.12.0` — (direct, third-party)
- `Lyo.Metrics` — (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)