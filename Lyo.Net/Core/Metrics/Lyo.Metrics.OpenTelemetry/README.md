# Lyo.Metrics.OpenTelemetry

OpenTelemetry implementation of `IMetrics` for exporting metrics to OpenTelemetry-compatible backends.

## Features

- Full `IMetrics` interface implementation
- Automatic metric name sanitization for OpenTelemetry conventions
- Tag/attribute conversion
- Thread-safe metric recording
- Support for multiple exporters (Console, Prometheus, OTLP, etc.)

## Usage

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

## Metric Type Mapping

- **Counters**: `IncrementCounter` → OpenTelemetry `Counter<long>`
- **Gauges**: `RecordGauge` → OpenTelemetry `Histogram<double>` (push-based)
- **Histograms/Timings**: `RecordHistogram`/`RecordTiming` → OpenTelemetry `Histogram<double>`
- **Errors**: `RecordError` → OpenTelemetry `Counter<long>` with error attributes
- **Events**: `RecordEvent` → OpenTelemetry `Counter<long>`

## Metric Name Sanitization

Metric names are automatically sanitized to follow OpenTelemetry naming conventions:

- Dots (`.`) are replaced with underscores (`_`)
- Hyphens (`-`) are replaced with underscores (`_`)
- Names starting with digits are prefixed with `_`

Example: `email.send.duration` → `email_send_duration`

## Tag/Attribute Conversion

Tags are converted to OpenTelemetry attributes:

- Tag keys are sanitized (dots/hyphens → underscores)
- Tag values are preserved as-is

## Example

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

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Metrics.OpenTelemetry.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `OpenTelemetry` | `1.*` |
| `OpenTelemetry.Exporter.Console` | `1.*` |
| `OpenTelemetry.Extensions.Hosting` | `1.*` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*2*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `OpenTelemetryMetrics`

<!-- LYO_README_SYNC:END -->

