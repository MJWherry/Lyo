# Lyo.Metrics.DependencyInjection

Service-collection registration for Lyo.Metrics, split out so the metrics library itself has no NuGet dependencies. Ships `AddLyoMetrics` in four option shapes, `AddLyoMetricsFromConfiguration`, and `AddNullMetrics`.

## Features

- **Same namespace.** Extensions remain in `namespace Lyo.Metrics`, so consumers add a `ProjectReference` and skip `using` edits.
- **Option shapes.** `Action<MetricsOptions>`, parameterless, `Action<IServiceProvider, MetricsOptions>`, and `Func<IServiceProvider, MetricsOptions>`.
- **Configuration binding.** On start, `AddLyoMetricsFromConfiguration(IConfiguration, string configSectionName = "MetricsOptions")` binds and validates.
- **No-op registration.** `AddNullMetrics()` binds `NullMetrics` to `IMetrics` for tests or when recording is off.
- **Why it is separate.** `Microsoft.Extensions.Configuration.Binder` comes in with `Microsoft.Extensions.Options.ConfigurationExtensions`. Leaving that out of `Lyo.Metrics` lets a package instrument itself for one dependency-free reference.

## Examples

### Register the in-memory implementation

```csharp
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;

// Defaults
services.AddLyoMetrics();

// With options
services.AddLyoMetrics(options =>
{
    options.MaxEventQueueSize = 50000;
    options.SamplingRate = 0.1; // Sample 10% of metrics
    options.ValidateTags = true;
});

// With the service provider available
services.AddLyoMetrics((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    options.MaxEventQueueSize = config.GetValue<int>("Metrics:MaxEventQueueSize");
    options.SamplingRate = config.GetValue<double>("Metrics:SamplingRate");
});
```

### Bind from config

```csharp
// Binds the "MetricsOptions" section and validates on start
services.AddLyoMetricsFromConfiguration(builder.Configuration);

// Or a custom section
services.AddLyoMetricsFromConfiguration(builder.Configuration, configSectionName: "MyMetrics");
```

### Disable recording

```csharp
// Registers NullMetrics for IMetrics. Call sites keep working; nothing is recorded
// and StartTimer allocates nothing.
services.AddNullMetrics();
```

### Use the registration

```csharp
public sealed class MyService(IMetrics metrics)
{
    public async Task ProcessAsync()
    {
        using (metrics.StartTimer("my_service.process"))
        {
            metrics.IncrementCounter("my_service.calls");
            await DoWorkAsync();
        }
    }
}
```

## When this package is needed

Host entry points that register `IMetrics` in a container should reference this package. Libraries that only *record* metrics should reference `Lyo.Metrics` alone: take `IMetrics` (or `IMetrics?` defaulting to `NullMetrics.Instance`) as a constructor parameter and let the host choose the bound implementation.

## How it relates to Lyo.Metrics.OpenTelemetry

[Lyo.Metrics.OpenTelemetry](../Lyo.Metrics.OpenTelemetry/README.md) owns `AddLyoMetricsWithOpenTelemetry` and registers its own `IMetrics` implementation. It does not depend on this package; choose one registration path or the other.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)