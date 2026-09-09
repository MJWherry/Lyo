# Lyo.Metrics.OpenTelemetry

OpenTelemetry-backed `IMetrics` that ships metrics to OpenTelemetry-compatible backends.

## Features

- `IMetrics` implementation sitting on OpenTelemetry instruments
- Sanitizes metric names to OpenTelemetry conventions
- Converts tags into attributes
- Records metrics in a thread-safe way
- `OpenTelemetryOptions` with `AddLyoMetricsWithOpenTelemetry` / `FromConfiguration`
- OTLP export (and `ILogger`) when `Endpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- Optional Console / Prometheus exporters via `configureMeterProvider`

## Examples

### Register from a configure delegate

```csharp
using Lyo.Metrics.OpenTelemetry;

services.AddLyoMetricsWithOpenTelemetry(o =>
{
    o.ServiceName = "MyApp.Metrics";
    o.Protocol = "grpc";
    o.MetricExportIntervalMs = 1000;
    // Leave Endpoint empty so OTEL_EXPORTER_OTLP_ENDPOINT (JetBrains plugin) can supply it.
});
```

### Bind from appsettings

```csharp
services.AddLyoMetricsWithOpenTelemetryFromConfiguration(builder.Configuration);

// appsettings.json:
// "OpenTelemetry": { "ServiceName": "MyApp", "Protocol": "grpc", "MetricExportIntervalMs": 1000 }
```

### Export to Prometheus

```csharp
services.AddLyoMetricsWithOpenTelemetry(o => o.ServiceName = "MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddPrometheusExporter(options =>
    {
        options.ScrapeEndpointPath = "/metrics";
    });
});
```

### Add a console exporter

```csharp
services.AddLyoMetricsWithOpenTelemetry(o => o.ServiceName = "MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddConsoleExporter();
});
```

### Sample

```csharp
services.AddLyoMetricsWithOpenTelemetryFromConfiguration(builder.Configuration);

var metrics = serviceProvider.GetRequiredService<IMetrics>();
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200")]);
using (metrics.StartTimer("operation.duration"))
{
    // Your operation here
}
```

## OTLP environment

- `OpenTelemetry:Endpoint` — optional collector URL. Leave empty in committed config; the JetBrains plugin injects `OTEL_EXPORTER_OTLP_ENDPOINT` (`http://localhost:<port>` gRPC).
- `OpenTelemetry:Protocol` / `OTEL_EXPORTER_OTLP_PROTOCOL` — `grpc` or `http/protobuf`. The JetBrains plugin expects `grpc`.
- `OpenTelemetry:MetricExportIntervalMs` / `OTEL_METRIC_EXPORT_INTERVAL` — flush interval in milliseconds (use `1000` so the plugin updates quickly).
- When neither options `Endpoint` nor the OTLP endpoint variables are set, `IMetrics` still records on OpenTelemetry instruments but nothing is exported.

## How metric types map

- **Counters.** OpenTelemetry `Counter<long>` is what `IncrementCounter` becomes.
- **Gauges.** OpenTelemetry `Histogram<double>` (push-based) is what `RecordGauge` becomes.
- **Histograms / timings.** OpenTelemetry `Histogram<double>` is what `RecordHistogram` / `RecordTiming` become.
- **Errors.** OpenTelemetry `Counter<long>` plus error attributes is what `RecordError` becomes.
- **Events.** OpenTelemetry `Counter<long>` is what `RecordEvent` becomes.

## Sanitizing metric names

- Dots (`.`) become underscores (`_`)
- Hyphens (`-`) become underscores (`_`)
- A leading `_` is added when a name starts with a digit

## Turning tags into attributes

- Dots and hyphens in tag keys become underscores
- Tag values are left as-is

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `OpenTelemetry` `1.16.0` (direct, third-party)
- `OpenTelemetry.Exporter.Console` `1.16.0` (direct, third-party)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` `1.16.0` (direct, third-party)
- `OpenTelemetry.Extensions.Hosting` `1.16.0` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)