# Lyo.Metrics.OpenTelemetry

OpenTelemetry implementation of `IMetrics` for exporting metrics to OpenTelemetry-compatible backends.

## Features

- `IMetrics` implementation backed by OpenTelemetry instruments
- Metric name sanitization for OpenTelemetry conventions
- Tag to attribute conversion
- Thread-safe metric recording
- Console, Prometheus, and OTLP exporters

## Examples

### Console exporter

```csharp
using Lyo.Metrics.OpenTelemetry;

services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics");
```

### Prometheus exporter

```csharp
services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddPrometheusExporter(options =>
    {
        options.ScrapeEndpointPath = "/metrics";
    });
});
```

### OTLP exporter

```csharp
services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    });
});
```

### Multiple exporters

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

## Metric type mapping

- **Counters.** `IncrementCounter` maps to OpenTelemetry `Counter<long>`.
- **Gauges.** `RecordGauge` maps to OpenTelemetry `Histogram<double>` (push-based).
- **Histograms / timings.** `RecordHistogram` / `RecordTiming` map to OpenTelemetry `Histogram<double>`.
- **Errors.** `RecordError` maps to OpenTelemetry `Counter<long>` with error attributes.
- **Events.** `RecordEvent` maps to OpenTelemetry `Counter<long>`.

## Metric name sanitization

- Dots (`.`) are replaced with underscores (`_`)
- Hyphens (`-`) are replaced with underscores (`_`)
- Names starting with digits are prefixed with `_`

## Tag and attribute conversion

- Tag keys are sanitized (dots/hyphens → underscores)
- Tag values are preserved as-is

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `OpenTelemetry` `1.16.0` (direct, third-party)
- `OpenTelemetry.Exporter.Console` `1.16.0` (direct, third-party)
- `OpenTelemetry.Extensions.Hosting` `1.16.0` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)