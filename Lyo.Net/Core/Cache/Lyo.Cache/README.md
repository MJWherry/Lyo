# Lyo.Cache

Local `ICacheService` plus typed byte payload methods. Serialize values once, store framed bytes (optional compression / encryption on .NET 10+), and round-trip without Fusion's default CLR binary formatter. Fusion-backed `ICacheService` lives in `Lyo.Cache.Fusion`.

## Features

- **`AddLocalCache` / `AddLocalCacheFromConfiguration`.** In-process cache backed by `IMemoryCache`. Wires `ICachePayloadCodec`, `ICachePayloadSerializer`, and the payload-aware `ICacheService` (singleton `LocalCacheService`). The serializer is registered with `TryAddSingleton` so hosts can pre-register their own `ICachePayloadSerializer`, for example one bound to the host's `JsonOptions`, before calling `AddLocalCache*`.
- **`AddFusionCache` (in `Lyo.Cache.Fusion`).** Same payload services. `FusionCacheService` implements the byte and typed `GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>` overloads.

## Benchmarks

- Portfolio suite: `cache`

## Registration

- **`AddLocalCache` / `AddLocalCacheFromConfiguration`.** In-process cache backed by `IMemoryCache`. Wires `ICachePayloadCodec`, `ICachePayloadSerializer`, and the payload-aware `ICacheService` (singleton `LocalCacheService`). The serializer is registered with `TryAddSingleton` so hosts can pre-register their own `ICachePayloadSerializer`, for example one bound to the host's `JsonOptions`, before calling `AddLocalCache*`.
- **`AddFusionCache` (in `Lyo.Cache.Fusion`).** Same payload services. `FusionCacheService` implements the byte and typed `GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>` overloads.

## Expiration

Entries have a duration and an expiration mode. Writes stamp the policy; reads honor it. Existing TimeSpan duration overloads are Absolute. Sliding is opt-in via setupAction (`SetSlidingExpiration`). Local uses IMemoryCache sliding; Fusion re-Sets on hit. Callers use only ICacheService.

| Mode | Meaning |
| -------------------- | -------------------------------------------------------------------------------------------------------- |
| `Absolute` (default) | Expire Duration after write. Successful reads do not extend lifetime. |
| `Sliding` | Expire Duration after the last successful access. TryGetValue / GetOrSet / payload hits reset the clock. |

## Bypass behavior

When `CacheOptions.Enabled` is `false`, `LocalCacheService` skips storage. Factories run on every call. `Set` / `SetPayload` are no-ops. `TryGetValue` / `TryGetPayload` return false. Invalidation calls return immediately. Use this for tests, local diagnostics, and dynamic cache-off toggles.

## L1 item snapshot

`ICacheService.Items` is this process's in-memory (L1) key and tag list, not a Redis dump. `CacheItem.Encrypted`, `Compressed`, and `SizeBytes` come from framed payload entries this process has written or decoded on a payload hit. Object `Set` / `GetOrSet` keys report encrypted/compressed as false and leave size unset. Keys also carry `Expires` (UTC instant from the entry TTL; sliding hits push it forward). Tags leave storage flags and `Expires` null. Other processes can write L2 keys that never appear here. There is no background L2 sync.

## Reflection / metadata TTLs

`CacheOptions` exposes dedicated lifetimes used by reflection-heavy helpers in other Lyo packages so they can share a single cache instance:

| Option | Default | Purpose |
| -------------------------- | ------- | ----------------------------------------------------------------------- |
| `PropertyInfoExpiration` | 1 hour | Reflected `PropertyInfo` lookups, for example query comparison helpers. |
| `TypeMetadataExpiration` | 4 hours | Type metadata used by conversion and comparison. |
| `PropertyGetterExpiration` | 4 hours | Compiled property-getter delegates. |
| `ComparisonInfoExpiration` | 1 hour | Property-difference plan metadata. |

## Invalidation methods

`ICacheService` extends `IHealth` and exposes these invalidation methods used by Lyo.Api and downstream CRUD plumbing.

| Method | Effect |
| ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `InvalidateCacheItem(string key)` | Removes a single entry (key is normalized lower-invariant) and drops its tag index entries. |
| `InvalidateCacheItemByTag(string tag)` | Removes every entry tagged with `tag`. |
| `InvalidateCacheByTypeAsync(string fullTypeName)` | Removes entries tagged for a CLR type name (typical tag shape `type:<full name>`). |
| `InvalidateCacheByTypeAsync(Type)` / `InvalidateCacheByTypeAsync<T>()` | Type-based overloads of the above. |
| `InvalidateQueryCacheAsync<TDb>()` | Invalidates cached queries tagged for entity type `TDb` (typically `entity:<name>`). |
| `InvalidateAllCachedQueriesAsync()` | Drops all entries tagged for general list/query caching (implementation-defined `queries` tag). |

`MaxBulkQueryInvalidationByIdCount` (default `20`) controls when CRUD invalidation helpers in Lyo.Api fall back from per-PK tags to a single type-wide
`entity:<type>` tag during bulk mutations. It is ignored when `QueryCacheTagGranularity` is `Broad` (which is always type-wide anyway).

## Health

Both `ICacheService` implementations (`LocalCacheService` and Fusion's `FusionCacheService`) implement `IHealth` with `HealthCheckName = "cache"`. The probe round-trips a short-lived test key tagged `lyo-health-check` and reports timing through `HealthResult.Healthy` / `HealthResult.Unhealthy`. Registering the cache adds this probe to hosts that resolve `IEnumerable<IHealth>`.

## Query cache tag granularity (`QueryCacheTagGranularity`)

Used by `Lyo.Api` when tagging `POST …/QueryConcrete`, `POST …/QueryProject`, and `GET` cache entries for Fusion `RemoveByTagAsync` invalidation.

| Value | Meaning |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Broad` (default) | Type-scoped tags (`entity:{typename}`, scope/shape tags). Lower CPU when attaching tags on cache write. Any mutation to that entity type clears all cached queries/GETs for the type. |
| `Granular` | Adds per-primary-key instance tags (`entity:{type}:{pk}`) so invalidation can target only affected pages. Higher tagging cost, finer busting. |

Set in configuration as `"QueryCacheTagGranularity": "Broad"` or `"Granular"`. See `QueryCacheTagBuilder` and `QueryCacheInvalidation` in `Lyo.Api`.

## Payload pipeline (`CacheOptions.Payload`)

Used when callers use `ICacheService.GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>`, for example `QueryOptions.CacheQueryResultsAsUtf8Payload` in Lyo.Api.

| Area | Role |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ICachePayloadSerializer` | Object to UTF-8 bytes (default: `SystemTextJsonCachePayloadSerializer`). Hosts can replace the registration so serialization matches HTTP JSON options. |
| `ICachePayloadCodec` | Frames plaintext bytes. Optional `AutoCompress` above `AutoCompressMinSizeBytes`. Optional `AutoEncrypt` (net10, requires `IEncryptionService`). `IsFramed` detects a LYO1 blob. See `CachePayloadCodec`. |
| `CachePayloadOptions` | `AutoCompress`, `AutoCompressMinSizeBytes`, net10 `AutoEncrypt` / `EncryptionKeyId`. |

Binding examples: `CacheOptions:Payload:AutoCompress`, `CacheOptions:Payload:AutoCompressMinSizeBytes`.

## API hosts (Lyo.Api)

`AddLyoQueryServices` registers `ICachePayloadSerializer` to use the host's `JsonOptions`, falling back to shared defaults. Cached query payloads stay aligned with REST JSON for `QueryConcreteReq` / `ProjectionQueryReq` shapes. See the Lyo.Api README *Query result caching* section for `QueryOptions:CacheQueryResultsAsUtf8Payload` and how `POST …/QueryConcrete` and `POST …/QueryProject` both honor it.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common` (transitive, lyo)
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