# Lyo.Cache.Fusion

`ZiggyCreatures.FusionCache` is adapted to `ICacheService` by `FusionCacheService` so `Lyo.Api`, workers, and feature modules can swap in-memory [`Lyo.Cache`](../../Cache/Lyo.Cache/README.md) for Fusion plus an optional Redis backplane without rewriting call sites.

## What Fusion gives you

- **Stampede protection / soft/hard timeouts.** Fusion handles cache stampede mitigation and configurable timeouts. See Fusion's own options in `FusionCacheOptions`.
- **Optional distributed backplane.** `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` keeps multiple nodes coherent when you register Redis.
- **Tag-based removal.** `RemoveByTagAsync` matches how `Lyo.Api` invalidates cached query/GET responses. See `QueryCacheTagGranularity` in the `Lyo.Cache` README.

## Parity of the payload pipeline

Fusion registration reuses the same payload stack as local cache. `ICachePayloadSerializer` writes JSON bytes by default. `ICachePayloadCodec` can compress and, on .NET 10+, encrypt. See `CacheOptions.Payload` in the `Lyo.Cache` README. `GetOrSetPayloadAsync<T>` / `GetOrSetPayloadAsync` behave the same on Fusion and local cache.

## Wire format for Redis L2

When Fusion has Redis L2 (`IDistributedCache`), values are written as a LYO2 metadata envelope around codec-framed bytes — not System.Text.Json. Payload `byte[]` that is already LYO1 is stored as-is. Object `GetOrSet` values are serialized once with `ICachePayloadSerializer` then framed by `ICachePayloadCodec` (`AutoEncrypt` / `AutoCompress`). JSON L2 keys written before this format still deserialize until they expire.

## Snapshot of L1 items

`Items` is this process's Fusion L1 list (Expire/Remove/Set events), not a Redis SCAN. Framed payload writes set `CacheItem.Compressed`, `Encrypted`, and `SizeBytes`. Keys also carry `Expires` from the remembered TTL (sliding hits re-Set and refresh it). Other nodes can write L2 keys that never show up here until this process loads them.

## Registration (`FusionCacheServiceExtensions`)

| Method | Behavior |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `AddFusionCache(Action<CacheOptions>?, …)` | Registers Fusion and payload services. Optional Redis backplane delegate requires `IConnectionMultiplexer` already in DI. |
| `AddFusionCache(redisConnectionString, …)` | Registers Redis via `AddRedisConnection`, then Fusion and the backplane. |
| `AddFusionCacheFromConfiguration(IConfiguration, …)` | Binds `CacheOptions` from `"CacheOptions"`. If configuration contains `Redis:ConnectionString` (section name configurable), registers Redis and the backplane. Otherwise Fusion runs local-only with the same options binding. |

`FusionCacheRegistration.AddFusionCacheInternal` is the internal entry point so this package's `AddFusionCache` does not collide with Fusion's own extension methods. Comments in
`FusionCacheRegistration.cs` spell out the wiring.

When `FusionCacheService` is resolved, `ServiceLocator` stores `IServiceProvider` so adapters can reach optional Fusion features that expect service location.

## Ops checklist

- Register cache and payload options in the same order as documented in `Lyo.Cache`. Fusion assumes those services exist.
- Redis L2 uses Lyo's `IFusionCacheSerializer` (codec-framed bytes). A host-registered System.Text.Json Fusion serializer is ignored.
- For Redis, handle network partitions at the infrastructure layer. Fusion's backplane only helps when Redis is reachable.
- For tag invalidation from `Lyo.Api`, pick `Granular` vs `Broad` as documented under `Lyo.Cache`. Granular adds per-PK tags and costs more CPU on writes.

## Related reading

- `CacheOptions`, query tags, payload compression and encryption: [`Lyo.Cache`](../Lyo.Cache/README.md).
- Query result caching toggles and invalidation paths: [`Lyo.Api`](../../../Apps/Api/Lyo.Api/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `ZiggyCreatures.FusionCache` `2.6.0` (direct, third-party)
- `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` `2.6.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)