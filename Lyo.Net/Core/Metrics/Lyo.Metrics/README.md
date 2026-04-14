# Lyo.Metrics

A flexible, thread-safe metrics library for .NET applications with support for multiple metric types and
implementations.

## Features

- ✅ **Thread-Safe**: Built with `ConcurrentDictionary` and proper locking mechanisms
- ✅ **Multiple Metric Types**: Counters, Gauges, Histograms, Timings, Errors, Events
- ✅ **Multiple Implementations**: In-memory, OpenTelemetry, and Null (for testing)
- ✅ **Memory Efficient**: Bounded collections, automatic cleanup, configurable limits
- ✅ **Production Ready**: Comprehensive error handling, overflow protection, resource management
- ✅ **Flexible Configuration**: Sampling, tag validation, cleanup intervals
- ✅ **Dependency Injection**: First-class support for .NET DI containers
- ✅ **Type Flexible**: Accepts `IConvertible` for numeric values (int, long, float, decimal, etc.)

## Quick Start

### Basic Usage

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

### Dependency Injection

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

## Metric Types

### Counters

Counters are monotonic values that only increase (or decrease). They're perfect for tracking totals, rates, and
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

### Gauges

Gauges represent a current value at a point in time. They're perfect for tracking current state like cache size, queue
length, or memory usage.

```csharp
// Record current value
metrics.RecordGauge("cache.size", 1500);

// Update gauge value
metrics.RecordGauge("memory.usage_mb", 512.5);

// With tags
metrics.RecordGauge("queue.length", 42, tags: [("queue_name", "email_queue")]);
```

### Histograms

Histograms track the distribution of values. They're perfect for tracking response times, sizes, or any numeric
distribution.

```csharp
// Record a value
metrics.RecordHistogram("response.size_bytes", 2048);

// Record multiple values (they'll be aggregated)
metrics.RecordHistogram("response.size_bytes", 1024);
metrics.RecordHistogram("response.size_bytes", 4096);

// With tags
metrics.RecordHistogram("response.size_bytes", 2048, tags: [("endpoint", "/api/data")]);
```

### Timings

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

### Errors

Record exceptions and errors with context.

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

Record discrete events with optional values.

```csharp
// Simple event
metrics.RecordEvent("user.login");

// Event with value
metrics.RecordEvent("file.uploaded", fileSizeBytes);

// Event with tags
metrics.RecordEvent("user.login", tags: [("provider", "google")]);
```

## Configuration

### MetricsOptions

Configure the behavior of `MetricsService`:

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

### Dependency Injection Configuration

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

// From configuration file (binds to "MetricsOptions" section by default)
services.AddLyoMetricsViaConfiguration();
// Or custom section name:
services.AddLyoMetricsViaConfiguration("MyMetrics");
```

## Implementations

### MetricsService (In-Memory)

The default implementation that stores metrics in memory. Perfect for single-instance applications or development.

```csharp
var metrics = new MetricsService();
// or
var metrics = new MetricsService(new MetricsOptions { ... });
```

**Features:**

- Fast, in-memory storage
- Thread-safe operations
- Bounded collections
- Automatic cleanup
- Export to snapshot

**Use when:**

- Single-instance applications
- Development/testing
- Simple metrics requirements
- No need for distributed observability

### OpenTelemetryMetrics

Implementation that exports metrics to OpenTelemetry. Perfect for production deployments requiring distributed
observability.

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

**Features:**

- OpenTelemetry standard
- Multiple exporters (Console, Prometheus, OTLP, etc.)
- Distributed observability
- Production-grade

**Use when:**

- Production deployments
- Multiple instances/services
- Integration with monitoring systems (Prometheus, Grafana, etc.)
- Need for distributed tracing/observability

See [Lyo.Metrics.OpenTelemetry README](../Lyo.Metrics.OpenTelemetry/README.md) for more details.

### NullMetrics

No-op implementation for testing or when metrics are optional.

```csharp
services.AddNullMetrics();
```

**Features:**

- Zero overhead
- No exceptions
- Perfect for testing

**Use when:**

- Unit testing
- Optional metrics
- Disabling metrics without code changes

## Querying Metrics

### Get Counter Value

```csharp
var metrics = new MetricsService();

metrics.IncrementCounter("requests.total", tags: [("method", "GET")]);

var count = metrics.GetCounterValue("requests.total", tags: [("method", "GET")]);
```

### Get Gauge Value

```csharp
metrics.RecordGauge("cache.size", 1500);

var size = metrics.GetGaugeValue("cache.size");
if (size.HasValue)
{
    Console.WriteLine($"Cache size: {size.Value}");
}
```

### Get Histogram

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

### Get Events

```csharp
// Get events (default: last 1000)
var events = metrics.GetEvents();      // last 1000
var events100 = metrics.GetEvents(100);

foreach (var evt in events)
{
    Console.WriteLine($"{evt.Name}: {evt.Value} at {evt.Timestamp}");
}
```

### Clear Metrics

```csharp
metrics.Clear();  // Clears all counters, gauges, histograms, and events
```

### Export Snapshot

```csharp
var snapshot = metrics.Export();

Console.WriteLine($"Total metrics recorded: {snapshot.TotalMetricsRecorded}");
Console.WriteLine($"Counters: {snapshot.Counters.Count}");
Console.WriteLine($"Gauges: {snapshot.Gauges.Count}");
Console.WriteLine($"Histograms: {snapshot.Histograms.Count}");

// Serialize to JSON
var json = JsonSerializer.Serialize(snapshot);
```

## Best Practices

### 1. Use Meaningful Metric Names

```csharp
// Good
metrics.IncrementCounter("http.requests.total");
metrics.RecordGauge("cache.size_bytes");

// Bad
metrics.IncrementCounter("c1");
metrics.RecordGauge("x");
```

### 2. Use Tags for Dimensions

```csharp
// Good - use tags for filtering/grouping
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200"), ("endpoint", "/api/users")]);

// Bad - create separate metrics for each dimension
metrics.IncrementCounter("requests.get.200.users");
metrics.IncrementCounter("requests.get.200.products");
```

### 3. Limit Tag Cardinality

Avoid high-cardinality tags (like user IDs) that create too many unique metric combinations.

```csharp
// Good - low cardinality
metrics.IncrementCounter("requests.total", tags: [("method", "GET"), ("status", "200")]); // Only a few values

// Bad - high cardinality
metrics.IncrementCounter("requests.total", tags: [("user_id", userId)]); // Thousands of unique values!
```

### 4. Use Sampling for High-Volume Metrics

```csharp
var options = new MetricsOptions
{
    SamplingRate = 0.1 // Sample 10% of metrics
};
```

### 5. Use Timers for Operations

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

### 6. Handle Errors Gracefully

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

## Thread Safety

All implementations are thread-safe and can be used concurrently from multiple threads:

```csharp
// Safe to use from multiple threads
Parallel.ForEach(items, item =>
{
    metrics.IncrementCounter("items.processed");
});
```

## Performance Considerations

- **Sampling**: Use `SamplingRate < 1.0` for high-volume metrics
- **Tag Cardinality**: Limit the number of unique tag combinations
- **Histogram Size**: Configure `MaxHistogramValues` appropriately
- **Event Queue**: Limit `MaxEventQueueSize` based on memory constraints

## Production Readiness

See [PRODUCTION_READINESS.md](../PRODUCTION_READINESS.md) for detailed production readiness assessment, known
limitations, and deployment recommendations.

## Examples

### ASP.NET Core Integration

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

### Background Service Integration

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




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Metrics.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Mathematics`
- `Lyo.Mathematics.Functions`

## Public API (generated)

Top-level `public` types in `*.cs` (*14*). Nested types and file-scoped namespaces may omit some entries.

- `CounterData`
- `Extensions`
- `GaugeData`
- `HistogramData`
- `IMetrics`
- `MathExtensions`
- `MetricData`
- `MetricsOptions`
- `MetricsService`
- `MetricsSnapshot`
- `MetricsTimer`
- `MetricType`
- `NullMetrics`
- `Timer`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]
