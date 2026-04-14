# Lyo.Health

Interface for services that can report their health. Services implement `IHealth` and expose health directly—no central health service. Health returns `HealthResult` with status,
timings, and optional metadata.

## Usage

Get health from the service directly:

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

Service interfaces (`IFileStorageService`, `ICacheService`, `IMqService`) extend `IHealth`—health comes from the service, no separate registration.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Health.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*2*). Nested types and file-scoped namespaces may omit some entries.

- `HealthResult`
- `IHealth`

<!-- LYO_README_SYNC:END -->

