# Lyo.Cache

Local and Fusion-backed **`ICacheService`** implementations with optional **typed byte payload** APIs for serializing values once, storing framed bytes (optional compression / encryption on .NET 10+), and round-tripping without Fusion’s default CLR binary formatter for cached objects.

## Features

- **`AddLocalCache`** / **`AddLocalCacheFromConfiguration`** — in-process cache backed by `IMemoryCache`; wires **`ICachePayloadCodec`**, **`ICachePayloadSerializer`**, and the payload-aware **`ICacheService`** (singleton **`LocalCacheService`**). The serializer is registered with **`TryAddSingleton`** so hosts can pre-register their own **`ICachePayloadSerializer`** (e.g. one bound to the host's `JsonOptions`) before calling `AddLocalCache*`.
- **`AddFusionCache`** (in **`Lyo.Cache.Fusion`**) — same payload services; **`FusionCacheService`** implements the byte and typed **`GetOrSetPayloadAsync`** / * *`GetOrSetPayloadAsync<T>`** overloads.

## Benchmarks

- Portfolio suite: `cache`

## Registration

- **`AddLocalCache`** / **`AddLocalCacheFromConfiguration`** — in-process cache backed by `IMemoryCache`; wires **`ICachePayloadCodec`**, **`ICachePayloadSerializer`**, and the payload-aware **`ICacheService`** (singleton **`LocalCacheService`**). The serializer is registered with **`TryAddSingleton`** so hosts can pre-register their own **`ICachePayloadSerializer`** (e.g. one bound to the host's `JsonOptions`) before calling `AddLocalCache*`.
- **`AddFusionCache`** (in **`Lyo.Cache.Fusion`**) — same payload services; **`FusionCacheService`** implements the byte and typed **`GetOrSetPayloadAsync`** / * *`GetOrSetPayloadAsync<T>`** overloads.

## Expiration

Entries have a **duration** and an **expiration mode**. Writes stamp the policy; reads honor it.

| Mode | Meaning |
| --- | --- |
| **`Absolute`** (default) | Expire `Duration` after write. Successful reads do not extend lifetime. |
| **`Sliding`** | Expire `Duration` after the last successful access. `TryGetValue` / `GetOrSet` / payload hits reset the clock. |

- Existing `TimeSpan duration` overloads on `GetOrSet` / `GetOrSetPayload` / `Set(..., duration)` are **Absolute**.
- `Set(key, value, tags)` remains **Absolute** + `CacheOptions.DefaultExpiration`.
- Sliding is opt-in via `setupAction`: `o.SetSlidingExpiration(TimeSpan.FromHours(8))` (also `SetAbsoluteExpiration`).
- `SetDuration` only sets the timespan; it does not change mode.
- **Local** uses `IMemoryCache` sliding expiration. **Fusion** re-Sets on hit from a sidecar policy. Callers use only `ICacheService`.

## Bypass behavior

When **`CacheOptions.Enabled`** is **`false`**, **`LocalCacheService`** skips storage entirely: factories run on every call, **`Set`** / **`SetPayload`** are no-ops, and **`TryGetValue`** / **`TryGetPayload`** return false. Invalidation calls return immediately. This is intended for tests, local diagnostics, and dynamic cache-off toggles.

## Reflection / metadata TTLs

`CacheOptions` exposes dedicated lifetimes used by reflection-heavy helpers in other Lyo packages so they can share a single cache instance:

| Option | Default | Purpose |
| ------------------------------ | ------- | ----------------------------------------------------------------- |
| **`PropertyInfoExpiration`** | 1 hour | Reflected `PropertyInfo` lookups (e.g. query comparison helpers). |
| **`TypeMetadataExpiration`** | 4 hours | Type metadata used by conversion and comparison. |
| **`PropertyGetterExpiration`** | 4 hours | Compiled property-getter delegates. |
| **`ComparisonInfoExpiration`** | 1 hour | Property-difference plan metadata. |

## Invalidation surface

`ICacheService` (which extends **`IHealth`**) exposes the following invalidation entry points used by Lyo.Api and downstream CRUD plumbing:

| Method | Effect |
| ------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| **`InvalidateCacheItem(string key)`** | Removes a single entry (key is normalized lower-invariant) and drops its tag index entries. |
| **`InvalidateCacheItemByTag(string tag)`** | Removes every entry tagged with `tag`. |
| **`InvalidateCacheByTypeAsync(string fullTypeName)`** | Removes entries tagged for a CLR type name (typical tag shape `type:<full name>`). |
| **`InvalidateCacheByTypeAsync(Type)`** / **`InvalidateCacheByTypeAsync<T>()`** | Type-based overloads of the above. |
| **`InvalidateQueryCacheAsync<TDb>()`** | Invalidates cached queries tagged for entity type `TDb` (typically `entity:<name>`). |
| **`InvalidateAllCachedQueriesAsync()`** | Drops all entries tagged for general list/query caching (implementation-defined `queries` tag). |

`MaxBulkQueryInvalidationByIdCount` (default `20`) controls when CRUD invalidation helpers in Lyo.Api fall back from per-PK tags to a single type-wide
`entity:<type>` tag during bulk mutations. It is ignored when `QueryCacheTagGranularity` is `Broad` (which is always type-wide anyway).

## Health

Both `ICacheService` implementations (**`LocalCacheService`** and Fusion's `FusionCacheService`) implement **`IHealth`** with **`HealthCheckName = "cache"`**. The probe round-trips a short-lived test key tagged `lyo-health-check` and reports timing through **`HealthResult.Healthy`** / **`HealthResult.Unhealthy`** — so registering the cache automatically contributes to host health endpoints that resolve **`IEnumerable<IHealth>`**.

## Query cache tag granularity (`QueryCacheTagGranularity`)

Used by **`Lyo.Api`** when tagging **`POST …/QueryConcrete`**, **`POST …/QueryProject`**, and **`GET`** cache entries for Fusion **`RemoveByTagAsync`** invalidation.

| Value | Meaning |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`Broad`** (default) | Type-scoped tags (**`entity:{typename}`**, scope/shape tags). Lower CPU when attaching tags on cache write; any mutation to that entity type clears all cached queries/GETs for the type. |
| **`Granular`** | Adds per-primary-key instance tags (**`entity:{type}:{pk}`**) so invalidation can target only affected pages — higher tagging cost, finer busting. |

Set in configuration as **`"QueryCacheTagGranularity": "Broad"`** or **`"Granular"`**. See **`QueryCacheTagBuilder`** and **`QueryCacheInvalidation`** in **`Lyo.Api`**.

## Payload pipeline (`CacheOptions.Payload`)

Used when callers use **`ICacheService.GetOrSetPayloadAsync`** / **`GetOrSetPayloadAsync<T>`** (for example **`QueryOptions.CacheQueryResultsAsUtf8Payload`** in Lyo.Api).

| Area | Role |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`ICachePayloadSerializer`** | Object ↔ UTF-8 bytes (default: **`SystemTextJsonCachePayloadSerializer`**). Hosts can replace the registration so serialization matches HTTP JSON options. |
| **`ICachePayloadCodec`** | Frames plaintext bytes; optional **`AutoCompress`** above **`AutoCompressMinSizeBytes`**; optional **`AutoEncrypt`** (net10, requires **`IEncryptionService`**) — see **`CachePayloadCodec`**. |
| **`CachePayloadOptions`** | **`AutoCompress`**, **`AutoCompressMinSizeBytes`**, net10 **`AutoEncrypt`** / **`EncryptionKeyId`**. |

Binding examples: **`CacheOptions:Payload:AutoCompress`**, **`CacheOptions:Payload:AutoCompressMinSizeBytes`**, etc.

## API hosts (Lyo.Api)

**`AddLyoQueryServices`** registers **`ICachePayloadSerializer`** to use the host’s **`JsonOptions`** (falling back to shared defaults). That keeps cached query payloads aligned with REST JSON for **`QueryConcreteReq`** / **`ProjectionQueryReq`** shapes. See the Lyo.Api README *Query result caching* section for **`QueryOptions:CacheQueryResultsAsUtf8Payload`** and how **`POST …/QueryConcrete`** and **`POST …/QueryProject`** both honor it.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Compression` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)