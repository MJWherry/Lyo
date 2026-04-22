# Lyo.Cache

Local and Fusion-backed **`ICacheService`** implementations with optional **typed byte payload** APIs for serializing values once, storing framed bytes (optional compression /
encryption on .NET 10+), and round-tripping without Fusion’s default CLR binary formatter for cached objects.

## Registration

- **`AddLocalCache`** / **`AddLocalCacheFromConfiguration`** — in-process cache; wires **`ICachePayloadCodec`**, **`ICachePayloadSerializer`**, and payload-aware **`ICacheService`
  **.
- **`AddFusionCache`** (in **`Lyo.Cache.Fusion`**) — same payload services; **`FusionCacheService`** implements **`GetOrSetPayloadAsync`** and typed **`GetOrSetPayloadAsync<T>`**.

Configure via **`CacheOptions`** (`"CacheOptions"` section): **`DefaultExpiration`**, **`EnableMetrics`**, type expiration maps, **`QueryCacheTagGranularity`**, and **`Payload`** (
see below).

### Query cache tag granularity (`QueryCacheTagGranularity`)

Used by **`Lyo.Api`** when tagging **`POST …/Query`**, **`POST …/QueryProject`**, and **`GET`** cache entries for Fusion **`RemoveByTagAsync`** invalidation.

| Value                 | Meaning                                                                                                                                                                                   |
|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`Broad`** (default) | Type-scoped tags (**`entity:{typename}`**, scope/shape tags). Lower CPU when attaching tags on cache write; any mutation to that entity type clears all cached queries/GETs for the type. |
| **`Granular`**        | Adds per-primary-key instance tags (**`entity:{type}:{pk}`**) so invalidation can target only affected pages — higher tagging cost, finer busting.                                        |

Set in configuration as **`"QueryCacheTagGranularity": "Broad"`** or **`"Granular"`**. See **`QueryCacheTagBuilder`** and **`QueryCacheInvalidation`** in **`Lyo.Api`**.

## Payload pipeline (`CacheOptions.Payload`)

Used when callers use **`ICacheService.GetOrSetPayloadAsync`** / **`GetOrSetPayloadAsync<T>`** (for example **`QueryOptions.CacheQueryResultsAsUtf8Payload`** in Lyo.Api).

| Area                          | Role                                                                                                                                                                                           |
|-------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`ICachePayloadSerializer`** | Object ↔ UTF-8 bytes (default: **`SystemTextJsonCachePayloadSerializer`**). Hosts can replace the registration so serialization matches HTTP JSON options.                                     |
| **`ICachePayloadCodec`**      | Frames plaintext bytes; optional **`AutoCompress`** above **`AutoCompressMinSizeBytes`**; optional **`AutoEncrypt`** (net10, requires **`IEncryptionService`**) — see **`CachePayloadCodec`**. |
| **`CachePayloadOptions`**     | **`AutoCompress`**, **`AutoCompressMinSizeBytes`**, net10 **`AutoEncrypt`** / **`EncryptionKeyId`**.                                                                                           |

Binding examples: **`CacheOptions:Payload:AutoCompress`**, **`CacheOptions:Payload:AutoCompressMinSizeBytes`**, etc.

## API hosts (Lyo.Api)

**`AddLyoQueryServices`** registers **`ICachePayloadSerializer`** to use the host’s **`JsonOptions`** (falling back to shared defaults). That keeps cached query payloads aligned
with REST JSON for **`QueryReq`** / **`ProjectionQueryReq`** shapes.

See the Lyo.Api README *Query result caching* section for **`QueryOptions:CacheQueryResultsAsUtf8Payload`** and how **`POST …/Query`** and **`POST …/QueryProject`** both honor it.
