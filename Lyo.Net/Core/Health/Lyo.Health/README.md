# Lyo.Health

Contract for services that report their own health. Implement `IHealth`. There is no central health service. A check returns `HealthResult` with status, timings, and optional metadata.

## Examples

### How to use it

```csharp
// File storage
var fileStorage = app.Services.GetRequiredService<IFileStorageService>();
var result = await fileStorage.CheckHealthAsync();
// result.IsHealthy, result.Duration, result.Metadata, result.Message

// Cache
var cache = app.Services.GetRequiredService<ICacheService>();
var result = await cache.CheckHealthAsync();

// RabbitMQ
var mq = app.Services.GetRequiredService<IMqService>();
var result = await mq.CheckHealthAsync();
```

### The contract

```csharp
public interface IHealth
{
    string HealthCheckName { get; }
    Task<HealthResult> CheckHealthAsync(CancellationToken ct = default);
}
```

### `HealthResult`

```csharp
HealthResult.Healthy(sw.Elapsed, message: null, metadata: new Dictionary<string, object?> { ["database"] = "audit" });
HealthResult.Unhealthy(sw.Elapsed, "Database connection failed", metadata: null, exception: ex);
```

## The contract

- Hosts label probe output with `HealthCheckName`, a short identifier, for example `"filestorage"`, `"cache"`, `"rabbitmq"`, `"audit-postgres"`, `"change-tracker-postgres"`.
- The live probe is `CheckHealthAsync`. Keep it cheap and short-circuiting, and honor the supplied `CancellationToken`.

## `HealthResult`

A probe outcome is an immutable, sealed `HealthResult`:

| Member | Type | Notes |
| ----------- | --------------------------------------- | ------------------------------------------------------------------------------ |
| `IsHealthy` | `bool` | `true` when healthy, `false` when not. |
| `Duration` | `TimeSpan` | Elapsed probe time (usually from `Stopwatch`). |
| `CheckedAt` | `DateTime` | UTC instant the probe finished (set by the factory helpers). |
| `Message` | `string?` | Optional human-readable summary; filled with the exception message on failure. |
| `Metadata` | `IReadOnlyDictionary<string, object?>?` | Schema, version, connection info, key-id, and similar. |
| `Exception` | `Exception?` | Exception captured when the probe threw. |

Prefer the static factories over constructing the type yourself:

## How to use it

Ask the service for health directly. Service interfaces (`IFileStorageService`, `ICacheService`, `IMqService`) extend `IHealth`. Health comes from the service. No separate registration. Hosts that want an aggregate view typically resolve `IEnumerable<IHealth>` and fan out `CheckHealthAsync` in parallel, then publish the resulting `HealthResult` collection.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)