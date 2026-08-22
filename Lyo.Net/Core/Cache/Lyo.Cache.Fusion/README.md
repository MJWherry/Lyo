# Lyo.Cache.Fusion

`FusionCacheService` adapts `ZiggyCreatures.FusionCache` to `ICacheService` so `Lyo.Api`, workers, and feature modules can swap in-memory [`Lyo.Cache`](../../Cache/Lyo.Cache/README.md) for Fusion plus an optional Redis backplane without rewriting call sites.

## What Fusion buys you

- **Stampede protection / soft/hard timeouts.** Fusion handles cache stampede mitigation and configurable timeouts. See Fusion's own options in `FusionCacheOptions`.
- **Optional distributed backplane.** `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` keeps multiple nodes coherent when you register Redis.
- **Tag-based removal.** `RemoveByTagAsync` matches how `Lyo.Api` invalidates cached query/GET responses. See `QueryCacheTagGranularity` in the `Lyo.Cache` README.

## Payload pipeline parity

Fusion registration reuses the same payload stack as local cache. `ICachePayloadSerializer` writes JSON bytes by default. `ICachePayloadCodec` can compress and, on .NET 10+, encrypt. See `CacheOptions.Payload` in the `Lyo.Cache` README. `GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>` behave the same on local cache and Fusion.

## Redis L2 wire format

When Fusion has Redis L2 (`IDistributedCache`), values are written as a LYO2 metadata envelope around codec-framed bytes — not System.Text.Json. Payload `byte[]` that is already LYO1 is stored as-is. Object `GetOrSet` values are serialized once with `ICachePayloadSerializer` then framed by `ICachePayloadCodec` (`AutoCompress` / `AutoEncrypt`). JSON L2 keys written before this format still deserialize until they expire.

## L1 item snapshot

`Items` is this process's Fusion L1 list (Set/Remove/Expire events), not a Redis SCAN. Framed payload writes set `CacheItem.Encrypted`, `Compressed`, and `SizeBytes`. Keys also carry `Expires` from the remembered TTL (sliding hits re-Set and refresh it). Other nodes can write L2 keys that never show up here until this process loads them.

## Registration (`FusionCacheServiceExtensions`)

| Method | Behavior |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `AddFusionCache(Action<CacheOptions>?, …)` | Registers Fusion and payload services. Optional Redis backplane delegate requires `IConnectionMultiplexer` already in DI. |
| `AddFusionCache(redisConnectionString, …)` | Registers Redis via `AddRedisConnection`, then Fusion and the backplane. |
| `AddFusionCacheFromConfiguration(IConfiguration, …)` | Binds `CacheOptions` from `"CacheOptions"`. If configuration contains `Redis:ConnectionString` (section name configurable), registers Redis and the backplane. Otherwise Fusion runs local-only with the same options binding. |

Internal wiring uses `FusionCacheRegistration.AddFusionCacheInternal` to avoid naming collisions between this package's `AddFusionCache` extension and Fusion's extension methods. See
source comments in `FusionCacheRegistration.cs`.

`ServiceLocator` captures `IServiceProvider` when `FusionCacheService` is resolved so adapter code can reach optional Fusion features that expect service location.

## Operational checklist

- Register payload and cache options in the same order as documented in `Lyo.Cache`. Fusion assumes those services exist.
- Redis L2 uses Lyo's `IFusionCacheSerializer` (codec-framed bytes). A host-registered System.Text.Json Fusion serializer is ignored.
- For Redis, handle network partitions at the infrastructure layer. Fusion's backplane only helps when Redis is reachable.
- For tag invalidation from `Lyo.Api`, pick `Broad` vs `Granular` as documented under `Lyo.Cache`. Granular adds per-PK tags and costs more CPU on writes.

## See also

- [`Lyo.Cache`](../Lyo.Cache/README.md). `CacheOptions`, query tags, payload encryption and compression.
- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md). Query result caching toggles and invalidation paths.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `ZiggyCreatures.FusionCache` `2.6.0` (direct, third-party)
- `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` `2.6.0` (direct, third-party)
- `Lyo.Common` (transitive, lyo)
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