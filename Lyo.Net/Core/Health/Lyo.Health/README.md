# Lyo.Health

Interface for services that can report their health. Services implement `IHealth` and expose health directly—no central health service. Health returns `HealthResult` with status, timings, and optional metadata.

## Examples

### Usage

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

### Contract

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

## Contract

- **`HealthCheckName`** is a short identifier (e.g. `"filestorage"`, `"cache"`, `"rabbitmq"`, `"audit-postgres"`, `"change-tracker-postgres"`) used to label probe output in hosts.
- **`CheckHealthAsync`** is the live probe; it should be cheap, short-circuiting, and honor the supplied `CancellationToken`.

## `HealthResult`

`HealthResult` is an immutable, sealed class that captures the outcome of a probe:

| Member | Type | Notes |
| ----------- | --------------------------------------- | --------------------------------------------------------------------------------- |
| `IsHealthy` | `bool` | `true` for healthy, `false` for unhealthy. |
| `Duration` | `TimeSpan` | How long the probe took (typically measured with `Stopwatch`). |
| `CheckedAt` | `DateTime` | UTC instant the probe completed (set by the factory helpers). |
| `Message` | `string?` | Optional human-readable summary; populated with the exception message on failure. |
| `Metadata` | `IReadOnlyDictionary<string, object?>?` | Connection info, schema, version, key-id, etc. |
| `Exception` | `Exception?` | Captured exception when the probe threw. |

Use the static factories rather than constructing directly:

## Usage

Get health from the service directly: Service interfaces (`IFileStorageService`, `ICacheService`, `IMqService`) extend `IHealth`—health comes from the service, no separate registration. Hosts that need an aggregate view typically resolve `IEnumerable<IHealth>` and fan out `CheckHealthAsync` in parallel, then publish the resulting `HealthResult` collection.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)