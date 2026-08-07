# Lyo.Cache.Fusion

**`FusionCacheService`** adapts **`ZiggyCreatures.FusionCache`** to **`ICacheService`** so application code (**`Lyo.Api`**, background workers, feature modules) can swap between **purely in-memory** [`Lyo.Cache`](../../Cache/Lyo.Cache/README.md) and **Fusion + optional Redis backplane** without rewriting call sites.

## What Fusion buys you

- **Stampede protection / soft/hard timeouts** — Fusion handles cache stampede mitigation and configurable timeouts (see Fusion’s own options in `FusionCacheOptions`).
- **Optional distributed backplane** — **`ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis`** keeps multiple nodes coherent when you register Redis.
- **Tag-based removal** — **`RemoveByTagAsync`** matches how **`Lyo.Api`** invalidates cached query/GET responses (see `QueryCacheTagGranularity` in the `Lyo.Cache` README).

## Payload pipeline parity

Fusion registration **reuses the same payload stack** as local cache: - **`ICachePayloadSerializer`** (JSON bytes by default). - **`ICachePayloadCodec`** (optional compression and, on **.NET 10+**, optional encryption hooks — see `Lyo.Cache` README **`CacheOptions.Payload`**). That means **`GetOrSetPayloadAsync` / `GetOrSetPayloadAsync<T>`** behave consistently whether you pick local or Fusion.

## Registration surface (`FusionCacheServiceExtensions`)

| Method | Behavior |
| -------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`AddFusionCache(Action<CacheOptions>?, …)`** | Registers Fusion + payload services; optional Redis backplane delegate (requires **`IConnectionMultiplexer`** already in DI). |
| **`AddFusionCache(redisConnectionString, …)`** | Registers Redis via **`AddRedisConnection`**, then Fusion + backplane. |
| **`AddFusionCacheFromConfiguration(IConfiguration, …)`** | Binds **`CacheOptions`** from **`"CacheOptions"`**. If configuration contains **`Redis:ConnectionString`** (section name configurable), registers Redis + backplane; otherwise Fusion runs **local-only** with the same options binding. |

Internal wiring uses **`FusionCacheRegistration.AddFusionCacheInternal`** to avoid naming collisions between **our** `AddFusionCache` extension and Fusion’s extension methods (see
source comments in `FusionCacheRegistration.cs`).

**`ServiceLocator`** captures **`IServiceProvider`** when **`FusionCacheService`** is resolved so adapter code can reach optional Fusion features that expect service location.

## Operational checklist

- Register **payload** + **cache options** in the same order as documented in **`Lyo.Cache`** (Fusion assumes those services exist).
- For Redis: ensure **network partitions** are handled at the infrastructure layer; Fusion’s backplane only helps when Redis is reachable.
- For **tag invalidation** from **`Lyo.Api`**: prefer **`Broad`** vs **`Granular`** tag strategies as documented under **`Lyo.Cache`**—Granular adds per-PK tags and costs more CPU on writes.

## See also

- [`Lyo.Cache`](../Lyo.Cache/README.md) — authoritative discussion of **`CacheOptions`**, query tags, payload encryption/compression.
- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) — query result caching toggles and invalidation paths.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `ZiggyCreatures.FusionCache` `2.6.0` — (direct, third-party)
- `ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis` `2.6.0` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)