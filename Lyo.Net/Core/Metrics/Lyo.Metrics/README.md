# Lyo.Metrics

Thread-safe counters, gauges, histograms, timings, errors, and events — in-memory, OpenTelemetry, and null implementations.

## Features

- **Concurrency.** Per-key locks plus `ConcurrentDictionary`.
- **Metric types.** Gauges, counters, timings, histograms, events, errors.
- **Implementations.** `OpenTelemetryMetrics`, `MetricsService` (in-memory), and `NullMetrics` for tests.
- **Bounds.** `MaxHistogramValues`, `MaxEventQueueSize`, and key-lock cleanup on `KeyLockCleanupIntervalMinutes`.
- **Safety.** Bounded collections and overflow protection on counters.
- **Options.** `ValidateTags`, `SamplingRate`, `ThrowOnConversionErrors`, `InvalidTagCharacters`.
- **No dependencies.** Only `Lyo.Exceptions`, no NuGet packages. Service registration lives in [Lyo.Metrics.DependencyInjection](../Lyo.Metrics.DependencyInjection/README.md) so instrumenting a low-dependency package stays cheap.
- **Values.** Accepts `IConvertible` numbers (long, int, decimal, float, and similar).

## Examples

### Subscribe to events

```csharp
using Lyo.Metrics;

// Create a metrics service
var metrics = new MetricsService();

// Record a counter
metrics.IncrementCounter("requests.total");

// Record a counter with value
metrics.IncrementCounter("bytes.processed", 1024);

// Record a counter with tags
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200")]);

// Record a gauge (current value)
metrics.RecordGauge("cache.size", 1500);

// Record timing using a timer
using (metrics.StartTimer("operation.duration"))
{
    // Your operation here
    await DoSomethingAsync();
}

// Record an error
try
{
    await ProcessDataAsync();
}
catch (Exception ex)
{
    metrics.RecordError("data.processing", ex);
}
```

### Construct directly, no container

```csharp
using Lyo.Metrics;

// A package that only records metrics needs nothing but this reference.
// Take IMetrics as an optional constructor parameter and fall back to the no-op
// singleton so callers who do not care about metrics pay nothing.
public sealed class MyService(IMetrics? metrics = null)
{
    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;

    public async Task ProcessAsync()
    {
        using (_metrics.StartTimer("my_service.process"))
        {
            _metrics.IncrementCounter("my_service.calls");
            await DoWorkAsync();
        }
    }
}

// For container registration, reference Lyo.Metrics.DependencyInjection
// and call services.AddLyoMetrics().
```

### MetricsOptions

```csharp
var options = new MetricsOptions
{
    // Maximum number of events to keep in the event queue
    MaxEventQueueSize = 10000,
    
    // Maximum number of values per histogram
    MaxHistogramValues = 1000,
    
    // Whether to throw exceptions on conversion errors
    ThrowOnConversionErrors = false,
    
    // Interval for cleaning up unused key locks (in minutes)
    KeyLockCleanupIntervalMinutes = 60,
    
    // Sampling rate (0.0 to 1.0)
    // 1.0 = record all metrics, 0.5 = record 50% of metrics
    SamplingRate = 1.0,
    
    // Whether to validate and sanitize tag keys/values
    ValidateTags = true,
    
    // Characters not allowed in tag keys/values
    InvalidTagCharacters = new HashSet<char> { '|', '=', '\n', '\r' }
};

var metrics = new MetricsService(options);
```

### ASP.NET Core

```csharp
// Registration requires the Lyo.Metrics.DependencyInjection package.
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLyoMetrics(options =>
        {
            options.MaxEventQueueSize = 50000;
            options.SamplingRate = 1.0;
        });
        
        services.AddControllers();
    }
}

public class MyController : ControllerBase
{
    private readonly IMetrics _metrics;
    
    public MyController(IMetrics metrics)
    {
        _metrics = metrics;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        using (_metrics.StartTimer("api.get.duration"))
        {
            _metrics.IncrementCounter("api.requests", tags: [("endpoint", "get"), ("method", "GET")]);
            
            var result = await ProcessRequestAsync();
            
            _metrics.IncrementCounter("api.requests.success");
            return Ok(result);
        }
    }
}
```

### Background service

```csharp
public class MyBackgroundService : BackgroundService
{
    private readonly IMetrics _metrics;
    
    public MyBackgroundService(IMetrics metrics)
    {
        _metrics = metrics;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (_metrics.StartTimer("background.job.duration"))
            {
                try
                {
                    await ProcessJobAsync();
                    _metrics.IncrementCounter("background.job.success");
                }
                catch (Exception ex)
                {
                    _metrics.RecordError("background.job", ex);
                    _metrics.IncrementCounter("background.job.failure");
                }
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### Errors

```csharp
try
{
    await ProcessDataAsync();
}
catch (Exception ex)
{
    metrics.RecordError("data.processing", ex);
    
    // With additional tags
    metrics.RecordError("data.processing", ex, tags: [("source", "api"), ("user_id", userId)]);
}
```

### Events

```csharp
// Simple event
metrics.RecordEvent("user.login");

// Event with value
metrics.RecordEvent("file.uploaded", fileSizeBytes);

// Event with tags
metrics.RecordEvent("user.login", tags: [("provider", "google")]);
```

### Read a counter

```csharp
var metrics = new MetricsService();

metrics.IncrementCounter("requests.total", tags: [("method", "GET")]);

var count = metrics.GetCounterValue("requests.total", tags: [("method", "GET")]);
```

### Read a gauge

```csharp
metrics.RecordGauge("cache.size", 1500);

var size = metrics.GetGaugeValue("cache.size");
if (size.HasValue)
{
    Console.WriteLine($"Cache size: {size.Value}");
}
```

### Read a histogram

```csharp
metrics.RecordHistogram("response.size", 1024);
metrics.RecordHistogram("response.size", 2048);
metrics.RecordHistogram("response.size", 4096);

var histogram = metrics.GetHistogram("response.size");
if (histogram != null)
{
    var min = histogram.Values.Min();
    var max = histogram.Values.Max();
    var avg = histogram.Values.Average();
    Console.WriteLine($"Min: {min}, Max: {max}, Avg: {avg}");
}
```

### Read events

```csharp
// Get events (default: last 1000)
var events = metrics.GetEvents(); // last 1000
var events100 = metrics.GetEvents(100);

foreach (var evt in events)
{
    Console.WriteLine($"{evt.Name}: {evt.Value} at {evt.Timestamp}");
}
```

### Clear metrics

```csharp
metrics.Clear(); // Clears all counters, gauges, histograms, and events
```

### Export a snapshot

```csharp
var snapshot = metrics.Export();

Console.WriteLine($"Total metrics recorded: {snapshot.TotalMetricsRecorded}");
Console.WriteLine($"Counters: {snapshot.Counters.Count}");
Console.WriteLine($"Gauges: {snapshot.Gauges.Count}");
Console.WriteLine($"Histograms: {snapshot.Histograms.Count}");

// Serialize to JSON
var json = JsonSerializer.Serialize(snapshot);
```

### Names that mean something

```csharp
// Good
metrics.IncrementCounter("http.requests.total");
metrics.RecordGauge("cache.size_bytes");

// Bad
metrics.IncrementCounter("c1");
metrics.RecordGauge("x");
```

### Tags as dimensions

```csharp
// Good - use tags for filtering/grouping
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200"), ("endpoint", "/api/users")]);

// Bad - create separate metrics for each dimension
metrics.IncrementCounter("requests.get.200.users");
metrics.IncrementCounter("requests.get.200.products");
```

### Sampling high-volume metrics

```csharp
var options = new MetricsOptions
{
    SamplingRate = 0.1 // Sample 10% of metrics
};
```

### Timers around work

```csharp
// Good - automatic timing
using (metrics.StartTimer("operation.duration"))
{
    await DoWorkAsync();
}

// Bad - manual timing (error-prone)
var sw = Stopwatch.StartNew();
try
{
    await DoWorkAsync();
}
finally
{
    sw.Stop();
    metrics.RecordTiming("operation.duration", sw.Elapsed);
}
```

### Record errors

```csharp
try
{
    await ProcessDataAsync();
}
catch (Exception ex)
{
    metrics.RecordError("data.processing", ex, tags: [("source", "api")]);
    throw; // Re-throw if needed
}
```

## Counters

Monotonic totals are recorded by `IncrementCounter` and `DecrementCounter`. Use them for request counts, bytes processed, or
occurrences.

```csharp
// Increment by 1 (default)
metrics.IncrementCounter("requests.total");

// Increment by specific value
metrics.IncrementCounter("bytes.processed", 1024);

// Decrement counter
metrics.DecrementCounter("items.in_queue", 5);

// With tags
metrics.IncrementCounter("requests.total", tags: [("method", "POST"), ("endpoint", "/api/users")]);
```

## Gauges

The last value for a name and tag set is stored by `RecordGauge`. Use it for cache size, queue
length, or memory usage.

```csharp
// Record current value
metrics.RecordGauge("cache.size", 1500);

// Update gauge value
metrics.RecordGauge("memory.usage_mb", 512.5);

// With tags
metrics.RecordGauge("queue.length", 42, tags: [("queue_name", "email_queue")]);
```

## Histograms

A numeric sample is appended to a bounded value list by `RecordHistogram`. Use it for response sizes or other numeric
distributions.

```csharp
// Record a value
metrics.RecordHistogram("response.size_bytes", 2048);

// Record multiple values (they'll be aggregated)
metrics.RecordHistogram("response.size_bytes", 1024);
metrics.RecordHistogram("response.size_bytes", 4096);

// With tags
metrics.RecordHistogram("response.size_bytes", 2048, tags: [("endpoint", "/api/data")]);
```

## Timings

Duration measurements are a special case of histograms. Use the `Timer` class for automatic timing.

```csharp
// Using StartTimer (recommended)
using (metrics.StartTimer("operation.duration"))
{
    await DoWorkAsync();
}

// Manual timing
var stopwatch = Stopwatch.StartNew();
await DoWorkAsync();
stopwatch.Stop();
metrics.RecordTiming("operation.duration", stopwatch.Elapsed);

// With tags
using (metrics.StartTimer("database.query", tags: [("table", "users")]))
{
    await QueryDatabaseAsync();
}
```

## MetricsOptions

Tune `MetricsService` here:

## DI configuration

Registrations on `IServiceCollection` moved to [Lyo.Metrics.DependencyInjection](../Lyo.Metrics.DependencyInjection/README.md). They still sit in `namespace Lyo.Metrics`, so an existing `using Lyo.Metrics;` keeps working — only the `ProjectReference` changes. This package itself has no NuGet dependencies, which is what lets `Lyo.Query.Evaluation` and other low-dependency packages instrument themselves without inheriting `Microsoft.Extensions.Configuration.Binder`.

## MetricsService (in-memory)

The default `IMetrics` implementation. Counters, gauges, histograms, and events live in process memory.

```csharp
var metrics = new MetricsService();
// or
var metrics = new MetricsService(new MetricsOptions { ... });
```

## OpenTelemetryMetrics

OpenTelemetry is the export path for this `IMetrics` implementation. Reach for it when Prometheus scrapes the process, OTLP ships elsewhere, or several processes need a shared collector.

```csharp
using Lyo.Metrics.OpenTelemetry;

services.AddLyoMetricsWithOpenTelemetryFromConfiguration(builder.Configuration);
// OpenTelemetry:ServiceName / Protocol / MetricExportIntervalMs in appsettings.
// Endpoint stays empty so OTEL_EXPORTER_OTLP_ENDPOINT (JetBrains plugin) can supply it.
```

**Behavior.**

- OpenTelemetry instruments
- OTLP when options `Endpoint` or the OTEL endpoint env var is set; Console / Prometheus via `configureMeterProvider`

**When to use.**

- Multiple instances
- Prometheus, Grafana, a JetBrains OpenTelemetry plugin, or an OTLP collector

The [Lyo.Metrics.OpenTelemetry README](../Lyo.Metrics.OpenTelemetry/README.md) has the package details.

## NullMetrics

Tests and optional recording use this no-op `IMetrics`. Construction is private; the singleton is `NullMetrics.Instance`. Register it through DI or take the static instance.

```csharp
services.AddNullMetrics();
// or
IMetrics metrics = NullMetrics.Instance;
```

**Behavior.**

- No recording
- No exceptions
- `StartTimer` returns `default(MetricsTimer)`. Disposal is a no-op, so `using (metrics.StartTimer(...))` allocates nothing.

**When to use.**

- Unit tests
- Optional metrics
- Turn recording off without changing call sites

## Statistics on histograms (`MathExtensions`)

Histogram samples become [`Lyo.Mathematics.Functions`](../../Mathematics/Lyo.Mathematics.Functions/README.md)
(`StatisticsFunctions`) through `Lyo.Metrics.MathExtensions`. The same helpers attach to `HistogramData?` (cached snapshots) and to `MetricsService` (lookup by name + tags). Missing or empty histograms yield `null` / empty arrays rather than exceptions.

```csharp
// On a HistogramData? (e.g. from snapshot.Histograms.Values or MetricsService.GetHistogram(...))
HistogramData? h = metrics.GetHistogram("latency.ms");
var stats = h.Describe(sample: true); // DescriptiveStatisticsResult?
var quartiles = h.Quartiles(); // QuartilesResult?
var iqr = h.InterquartileRange();
var p95 = h.Percentile(0.95);
var sma = h.MovingAverage(windowSize: 30);
var ema = h.ExponentialMovingAverage(smoothingFactor: 0.2);
var rollingStd = h.RollingStandardDeviation(windowSize: 30);
var rollingMed = h.RollingMedian(windowSize: 30);
var mad = h.MedianAbsoluteDeviation();
var z = h.LatestZScore();
var anomalousZ = h.IsLatestValueAnomalous(threshold: 3d);
var anomalousMad = h.IsLatestValueAnomalousByMad(threshold: 3.5d);
var ci95 = h.MeanConfidenceInterval(confidenceLevel: 0.95);
var pearson = h.PearsonCorrelation(other); // null if either is empty

// Tag-aware lookups directly on MetricsService
var p99 = metrics.GetHistogramPercentile("latency.ms", percentile: 0.99,
                                           tags: new[] { ("endpoint", "/api/users") });
var pcts = snapshot.GetHistogramPercentiles("latency.ms", 0.5, 0.9, 0.99);
var pearr = metrics.GetHistogramPearsonCorrelation(
                "service_a.latency", "service_b.latency");
```

## Keep tag cardinality down

Tags with huge cardinality (user IDs are the usual example) explode unique series. Keep tag values small and bounded.

```csharp
// Good - low cardinality
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200")]); // Only a few values

// Bad - high cardinality
metrics.IncrementCounter("requests.total", tags: [("user_id", userId)]); // Thousands of unique values!
```

## Thread safety

Concurrent calls from many threads are supported; every implementation is thread-safe:

```csharp
// Safe to use from multiple threads
Parallel.ForEach(items, item =>
{
    metrics.IncrementCounter("items.processed");
});
```

## Performance

- **Sampling.** Use `SamplingRate < 1.0` for high-volume metrics.
- **Tag cardinality.** Limit unique tag combinations.
- **Histogram size.** Set `MaxHistogramValues`.
- **Event queue.** Set `MaxEventQueueSize` from available memory.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)