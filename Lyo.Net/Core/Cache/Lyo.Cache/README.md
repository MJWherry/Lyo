# Lyo.Cache

In-process `ICacheService` plus typed byte-payload methods. Serialize values once, store framed bytes (optional compression / encryption on .NET 10+), and round-trip without Fusion's default CLR binary formatter. Fusion-backed `ICacheService` lives in `Lyo.Cache.Fusion`.

## Features

- **`AddLocalCacheFromConfiguration` / `AddLocalCache`.** In-process cache backed by `IMemoryCache`. Wires `ICachePayloadSerializer`, `ICachePayloadCodec`, and the payload-aware `ICacheService` (singleton `LocalCacheService`). The serializer is registered with `TryAddSingleton` so hosts can pre-register their own `ICachePayloadSerializer`, for example one bound to the host's `JsonOptions`, before calling `AddLocalCache*`.
- **`AddFusionCache` (in `Lyo.Cache.Fusion`).** Same payload services. `FusionCacheService` implements the typed and byte `GetOrSetPayloadAsync<T>` / `GetOrSetPayloadAsync` overloads.

## Benchmarks

- Portfolio suite: `cache`

## Registration

- **`AddLocalCache` / `AddLocalCacheFromConfiguration`.** In-process cache backed by `IMemoryCache`. Wires `ICachePayloadCodec`, `ICachePayloadSerializer`, and the payload-aware `ICacheService` (singleton `LocalCacheService`). The serializer is registered with `TryAddSingleton` so hosts can pre-register their own `ICachePayloadSerializer`, for example one bound to the host's `JsonOptions`, before calling `AddLocalCache*`.
- **`AddFusionCache` (in `Lyo.Cache.Fusion`).** Same payload services. `FusionCacheService` implements the byte and typed `GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>` overloads.

## Expiration

Each entry has a duration and an expiration mode. Writes stamp the policy; reads honor it. Existing TimeSpan duration overloads are Absolute. Sliding is opt-in via setupAction (`SetSlidingExpiration`). Local uses IMemoryCache sliding; Fusion re-Sets on hit. Callers use only ICacheService.

| Mode | Meaning |
| -------------------- | -------------------------------------------------------------------------------------------------------- |
| `Absolute` (default) | Expire Duration after write. Successful reads do not extend lifetime. |
| `Sliding` | Expire Duration after the last successful access. TryGetValue / GetOrSet / payload hits reset the clock. |

## What bypass does

Storage is skipped by `LocalCacheService` when `CacheOptions.Enabled` is `false`. Factories run on every call. `Set` / `SetPayload` are no-ops. `TryGetValue` / `TryGetPayload` return false. Invalidation calls return immediately. Use this for tests, local diagnostics, and dynamic cache-off toggles.

## L1 item snapshot

This process's in-memory (L1) key and tag list is `ICacheService.Items`, not a Redis dump. `CacheItem.Encrypted`, `Compressed`, and `SizeBytes` come from framed payload entries this process has written or decoded on a payload hit. Object `Set` / `GetOrSet` keys report encrypted/compressed as false and leave size unset. Keys also carry `Expires` (UTC instant from the entry TTL; sliding hits push it forward). Tags leave storage flags and `Expires` null. Other processes can write L2 keys that never appear here. There is no background L2 sync.

## Reflection / metadata TTLs

Dedicated lifetimes on `CacheOptions` are used by reflection-heavy helpers in other Lyo packages so they can share a single cache instance:

| Option | Default | Purpose |
| -------------------------- | ------- | ----------------------------------------------------------------------- |
| `PropertyInfoExpiration` | 1 hour | Reflected `PropertyInfo` lookups, for example query comparison helpers. |
| `TypeMetadataExpiration` | 4 hours | Type metadata used by conversion and comparison. |
| `PropertyGetterExpiration` | 4 hours | Compiled property-getter delegates. |
| `ComparisonInfoExpiration` | 1 hour | Property-difference plan metadata. |

## Invalidation methods

`IHealth` is implemented by `ICacheService`, which also exposes these invalidation methods used by Lyo.Api and downstream CRUD plumbing.

| Method | Effect |
| ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `InvalidateCacheItem(string key)` | Removes a single entry (key is normalized lower-invariant) and drops its tag index entries. |
| `InvalidateCacheItemByTag(string tag)` | Removes every entry tagged with `tag`. |
| `InvalidateCacheByTypeAsync(string fullTypeName)` | Removes entries tagged for a CLR type name (typical tag shape `type:<full name>`). |
| `InvalidateCacheByTypeAsync(Type)` / `InvalidateCacheByTypeAsync<T>()` | Type-based overloads of the above. |
| `InvalidateQueryCacheAsync<TDb>()` | Invalidates cached queries tagged for entity type `TDb` (typically `entity:<name>`). |
| `InvalidateAllCachedQueriesAsync()` | Drops all entries tagged for general list/query caching (implementation-defined `queries` tag). |

When CRUD invalidation helpers in Lyo.Api fall back from per-PK tags to a single type-wide
`entity:<type>` tag during bulk mutations, `MaxBulkQueryInvalidationByIdCount` (default `20`) is the cutoff. It is ignored when `QueryCacheTagGranularity` is `Broad` (which is always type-wide anyway).

## Health

`HealthCheckName = "cache"` is implemented by both `ICacheService` types (`LocalCacheService` and Fusion's `FusionCacheService`). A short-lived test key tagged `lyo-health-check` is written and read back; timing lands on `HealthResult.Healthy` / `HealthResult.Unhealthy`. Any host that resolves `IEnumerable<IHealth>` picks the probe up when the cache is registered.

## Query cache tag granularity (`QueryCacheTagGranularity`)

`Lyo.Api` uses this when tagging `POST …/QueryConcrete`, `POST …/QueryProject`, and `GET` cache entries for Fusion `RemoveByTagAsync` invalidation.

| Value | Meaning |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Broad` (default) | Type-scoped tags (`entity:{typename}`, scope/shape tags). Lower CPU when attaching tags on cache write. Any mutation to that entity type clears all cached queries/GETs for the type. |
| `Granular` | Adds per-primary-key instance tags (`entity:{type}:{pk}`) so invalidation can target only affected pages. Higher tagging cost, finer busting. |

Configuration values are `"QueryCacheTagGranularity": "Broad"` or `"Granular"`. `QueryCacheTagBuilder` and `QueryCacheInvalidation` in `Lyo.Api` consume them.

## Payload pipeline (`CacheOptions.Payload`)

`ICacheService.GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>` is the call path, including `QueryOptions.CacheQueryResultsAsUtf8Payload` in Lyo.Api.

| Area | Role |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ICachePayloadSerializer` | Object to UTF-8 bytes (default: `SystemTextJsonCachePayloadSerializer`). Hosts can replace the registration so serialization matches HTTP JSON options. |
| `ICachePayloadCodec` | Frames plaintext bytes. Optional `AutoCompress` above `AutoCompressMinSizeBytes`. Optional `AutoEncrypt` (net10, requires `IEncryptionService`). `IsFramed` detects a LYO1 blob. See `CachePayloadCodec`. |
| `CachePayloadOptions` | `AutoCompress`, `AutoCompressMinSizeBytes`, net10 `AutoEncrypt` / `EncryptionKeyId`. |

Typical bindings: `CacheOptions:Payload:AutoCompress`, `CacheOptions:Payload:AutoCompressMinSizeBytes`.

## API hosts (Lyo.Api)

The host `JsonOptions` (or shared defaults when those are missing) become `ICachePayloadSerializer` via `AddLyoQueryServices`. That keeps cached query payloads on the same JSON contract as REST `QueryConcreteReq` / `ProjectionQueryReq`. The Lyo.Api README *Query result caching* section covers `QueryOptions:CacheQueryResultsAsUtf8Payload` and the fact that both `POST …/QueryConcrete` and `POST …/QueryProject` honor it.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)