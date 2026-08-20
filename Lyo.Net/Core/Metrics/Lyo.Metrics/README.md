# Lyo.Metrics

Thread-safe counters, gauges, histograms, timings, errors, and events, with in-memory, OpenTelemetry, and null implementations.

## Features

- **Concurrency.** `ConcurrentDictionary` plus per-key locks.
- **Metric types.** Counters, gauges, histograms, timings, errors, events.
- **Implementations.** `MetricsService` (in-memory), `OpenTelemetryMetrics`, and `NullMetrics` for tests.
- **Bounds.** `MaxEventQueueSize`, `MaxHistogramValues`, and key-lock cleanup on `KeyLockCleanupIntervalMinutes`.
- **Safety.** Overflow protection on counters and bounded collections.
- **Options.** `SamplingRate`, `ValidateTags`, `InvalidTagCharacters`, `ThrowOnConversionErrors`.
- **DI.** `AddLyoMetrics`, `AddLyoMetricsFromConfiguration`, `AddNullMetrics`.
- **Values.** Accepts `IConvertible` numbers (int, long, float, decimal, and similar).

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

### Dependency injection

```csharp
using Lyo.Metrics;
using Microsoft.Extensions.DependencyInjection;

// Register metrics service
services.AddLyoMetrics();

// Or with custom configuration
services.AddLyoMetrics(options =>
{
    options.MaxEventQueueSize = 50000;
    options.SamplingRate = 0.1; // Sample 10% of metrics
    options.ValidateTags = true;
});

// Use in your services
public class MyService
{
    private readonly IMetrics _metrics;
    
    public MyService(IMetrics metrics)
    {
        _metrics = metrics;
    }
    
    public async Task ProcessAsync()
    {
        using (_metrics.StartTimer("my_service.process"))
        {
            _metrics.IncrementCounter("my_service.calls");
            // Your logic here
        }
    }
}
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

### DI configuration

```csharp
// Basic registration
services.AddLyoMetrics();

// With options
services.AddLyoMetrics(options =>
{
    options.MaxEventQueueSize = 50000;
    options.SamplingRate = 0.1;
});

// With options factory
services.AddLyoMetrics((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    options.MaxEventQueueSize = config.GetValue<int>("Metrics:MaxEventQueueSize");
    options.SamplingRate = config.GetValue<double>("Metrics:SamplingRate");
});

// From configuration (binds to "MetricsOptions" section by default,
// validating on start via Options.ValidateOnStart())
services.AddLyoMetricsFromConfiguration(configuration);
// Or custom section name:
services.AddLyoMetricsFromConfiguration(configuration, configSectionName: "MyMetrics");
```

### ASP.NET Core

```csharp
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

### Get counter value

```csharp
var metrics = new MetricsService();

metrics.IncrementCounter("requests.total", tags: [("method", "GET")]);

var count = metrics.GetCounterValue("requests.total", tags: [("method", "GET")]);
```

### Get gauge value

```csharp
metrics.RecordGauge("cache.size", 1500);

var size = metrics.GetGaugeValue("cache.size");
if (size.HasValue)
{
    Console.WriteLine($"Cache size: {size.Value}");
}
```

### Get histogram

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

### Get events

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

### Export snapshot

```csharp
var snapshot = metrics.Export();

Console.WriteLine($"Total metrics recorded: {snapshot.TotalMetricsRecorded}");
Console.WriteLine($"Counters: {snapshot.Counters.Count}");
Console.WriteLine($"Gauges: {snapshot.Gauges.Count}");
Console.WriteLine($"Histograms: {snapshot.Histograms.Count}");

// Serialize to JSON
var json = JsonSerializer.Serialize(snapshot);
```

### Meaningful metric names

```csharp
// Good
metrics.IncrementCounter("http.requests.total");
metrics.RecordGauge("cache.size_bytes");

// Bad
metrics.IncrementCounter("c1");
metrics.RecordGauge("x");
```

### Tags for dimensions

```csharp
// Good - use tags for filtering/grouping
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200"), ("endpoint", "/api/users")]);

// Bad - create separate metrics for each dimension
metrics.IncrementCounter("requests.get.200.users");
metrics.IncrementCounter("requests.get.200.products");
```

### Sampling for high-volume metrics

```csharp
var options = new MetricsOptions
{
    SamplingRate = 0.1 // Sample 10% of metrics
};
```

### Timers for operations

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

`IncrementCounter` and `DecrementCounter` record monotonic totals. Use them for request counts, bytes processed, or
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

`RecordGauge` stores the last value for a name and tag set. Use it for cache size, queue
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

`RecordHistogram` appends a numeric sample to a bounded value list. Use it for response sizes or other numeric
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

Timings are a special case of histograms for measuring duration. Use the `Timer` class for automatic timing.

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

Configure the behavior of `MetricsService`:

## DI configuration

The `IServiceCollection` registrations live in `Lyo.Metrics.Extensions` and cover: parameterless, `Action<MetricsOptions>`, `Action<IServiceProvider, MetricsOptions>`, `Func<IServiceProvider, MetricsOptions>`, and `AddLyoMetricsFromConfiguration(IConfiguration, string configSectionName = "MetricsOptions")`. `AddNullMetrics()` registers `NullMetrics` for the same `IMetrics` contract.

## MetricsService (in-memory)

Default `IMetrics` implementation. Stores counters, gauges, histograms, and events in process memory.

```csharp
var metrics = new MetricsService();
// or
var metrics = new MetricsService(new MetricsOptions { ... });
```

**Behavior.**

- In-memory storage
- Thread-safe recorders
- Bounded collections
- Key-lock cleanup on the configured interval
- `Export()` snapshot

**When to use.**

- One process
- Development or tests
- No remote exporter

## OpenTelemetryMetrics

`IMetrics` implementation that exports through OpenTelemetry. Use it when you scrape Prometheus, send OTLP, or otherwise
collect from multiple processes.

```csharp
using Lyo.Metrics.OpenTelemetry;

services.AddLyoMetricsWithOpenTelemetry("MyApp.Metrics", configureMeterProvider: builder =>
{
    builder.AddConsoleExporter(); // For development
    builder.AddPrometheusExporter(); // For Prometheus scraping
    builder.AddOtlpExporter(options => // For OTLP collection
    {
        options.Endpoint = new Uri("http://otel-collector:4317");
    });
});
```

**Behavior.**

- OpenTelemetry instruments
- Console, Prometheus, and OTLP exporters

**When to use.**

- Multiple instances
- Prometheus, Grafana, or an OTLP collector

See [Lyo.Metrics.OpenTelemetry README](../Lyo.Metrics.OpenTelemetry/README.md).

## NullMetrics

No-op `IMetrics` for tests or when recording is optional. The class is a singleton (`NullMetrics.Instance`) with a private constructor. Use the DI extension or
the static instance directly.

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

`Lyo.Metrics.MathExtensions` bridges recorded histogram values into [`Lyo.Mathematics.Functions`](../../Mathematics/Lyo.Mathematics.Functions/README.md)
(`StatisticsFunctions`). The extensions hang off both `HistogramData?` (so they work on cached snapshots) and `MetricsService` (so they look up the histogram by name + tags), and
return `null` / empty arrays for missing or empty histograms instead of throwing.

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

## Limit tag cardinality

Avoid high-cardinality tags (like user IDs) that create too many unique metric combinations.

```csharp
// Good - low cardinality
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200")]); // Only a few values

// Bad - high cardinality
metrics.IncrementCounter("requests.total", tags: [("user_id", userId)]); // Thousands of unique values!
```

## Thread safety

All implementations are thread-safe and can be used concurrently from multiple threads:

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
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)