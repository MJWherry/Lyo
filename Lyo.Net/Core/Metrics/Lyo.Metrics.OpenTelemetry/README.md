# Lyo.Metrics.OpenTelemetry

OpenTelemetry implementation of `IMetrics` for exporting metrics to OpenTelemetry-compatible backends.

## Features

- Full `IMetrics` interface implementation
- Automatic metric name sanitization for OpenTelemetry conventions
- Tag/attribute conversion
- Thread-safe metric recording
- Support for multiple exporters (Console, Prometheus, OTLP, etc.)

## Examples

### Basic Setup (Console Exporter)

```csharp
using Lyo.Metrics.OpenTelemetry;

services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics");
```

### With Prometheus Exporter

```csharp
services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddPrometheusExporter(options =>
    {
        options.ScrapeEndpointPath = "/metrics";
    });
});
```

### With OTLP Exporter (for Jaeger, Tempo, etc.)

```csharp
services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    });
});
```

### With Multiple Exporters

```csharp
services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics", configureMeterProvider: builder =>
{
    // Console for development
    builder.AddConsoleExporter();
    
    // Prometheus for scraping
    builder.AddPrometheusExporter();
    
    // OTLP for centralized collection
    builder.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("https://otel-collector:4317");
    });
});
```

### Example

```csharp
// Register OpenTelemetry metrics
services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics");

// Use IMetrics as normal
var metrics = serviceProvider.GetRequiredService<IMetrics>();

metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200")]);

using (metrics.StartTimer("operation.duration"))
{
    // Your operation here
}
```

## Metric Type Mapping

- **Counters**: `IncrementCounter` → OpenTelemetry `Counter<long>`
- **Gauges**: `RecordGauge` → OpenTelemetry `Histogram<double>` (push-based)
- **Histograms/Timings**: `RecordHistogram`/`RecordTiming` → OpenTelemetry `Histogram<double>`
- **Errors**: `RecordError` → OpenTelemetry `Counter<long>` with error attributes
- **Events**: `RecordEvent` → OpenTelemetry `Counter<long>`

## Metric Name Sanitization

- Dots (`.`) are replaced with underscores (`_`)
- Hyphens (`-`) are replaced with underscores (`_`)
- Names starting with digits are prefixed with `_`

## Tag/Attribute Conversion

- Tag keys are sanitized (dots/hyphens → underscores)
- Tag values are preserved as-is

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `OpenTelemetry` `1.16.0` — (direct, third-party)
- `OpenTelemetry.Exporter.Console` `1.16.0` — (direct, third-party)
- `OpenTelemetry.Extensions.Hosting` `1.16.0` — (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)