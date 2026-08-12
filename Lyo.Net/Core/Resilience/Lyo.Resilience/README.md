# Lyo.Resilience

A thin wrapper around Polly for resilience pipelines with configuration-from-appsettings support and built-in logging. Does not include any library-specific pipeline definitions;
pipelines are defined entirely via configuration.

## Features

- **Default pipelines** – `lyo-basic` and `lyo-http` with sensible retry and timeout; use without config
- Load resilience pipelines from `appsettings.json` (or any `IConfiguration` source)
- Support for Retry, Timeout, and CircuitBreaker strategies
- Built-in logging for retries, timeouts, and circuit breaker state changes
- **Resilience for actions** – `IResilientExecutor` uses default pipeline by default; specify pipeline name to override
- **Result-type support** – pass `isSuccess` predicate to retry when methods return failed Result instead of throwing
- **Resilience for HttpClient** – `AddLyoResilienceHandler()` uses default; or pass pipeline name
- Integrates with `Polly.Extensions` and `ResiliencePipelineProvider<string>` for DI

## Examples

### Nested under service options (recommended)

```json
{
  "TwilioOptions": {
    "AccountSid": "...",
    "AuthToken": "...",
    "DefaultFromPhoneNumber": "+1234567890",
    "Resilience": {
      "sms-pipeline": {
        "Retry": {
          "MaxRetryAttempts": 3,
          "Delay": "00:00:02",
          "MaxDelay": "00:00:30",
          "BackoffType": "Exponential",
          "UseJitter": true
        },
        "Timeout": {
          "Timeout": "00:00:10"
        }
      }
    }
  }
}
```

### Nested under service options (recommended) (2)

```csharp
services.AddLyoResiliencePipelinesFromOptions(builder.Configuration, "TwilioOptions");
// Loads from TwilioOptions:Resilience
```

### Standalone section example

```json
{
  "Lyo": {
    "ResiliencePipelines": {
      "my-pipeline": {
        "Retry": { "MaxRetryAttempts": 3, "Delay": "00:00:02" },
        "Timeout": { "Timeout": "00:00:10" }
      }
    }
  }
}
```

### Standalone section example (2)

```csharp
services.AddLyoResiliencePipelines(builder.Configuration); // default: Lyo:ResiliencePipelines
// Or: services.AddLyoResiliencePipelines(builder.Configuration, "CustomSection:Path");
```

### Default pipelines (quick start)

```csharp
// Adds lyo-basic and lyo-http pipelines (retry + timeout)
builder.Services.AddLyoResilienceDefaults();

// Or: AddResilientExecutor registers defaults automatically
builder.Services.AddResilientExecutor();
```

### Resilience for actions

```csharp
// Uses default pipeline (lyo-basic)
builder.Services.AddResilientExecutor();

// In a service
public class MyService
{
    private readonly IResilientExecutor _executor;

    public MyService(IResilientExecutor executor) => _executor = executor;

    // Void - uses default pipeline
    public async Task DoWorkAsync(CancellationToken ct) =>
        await _executor.ExecuteAsync(ct => SomeExternalCallAsync(ct), ct);

    // With result - uses default pipeline
    public async Task<string> GetDataAsync(CancellationToken ct) =>
        await _executor.ExecuteAsync(ct => FetchAsync(ct), ct);

    // Specify pipeline
    public async Task DoWorkWithCustomPipelineAsync(CancellationToken ct) =>
        await _executor.ExecuteAsync("my-pipeline", ct => SomeExternalCallAsync(ct), ct);

    // Result types - retry when !result.IsSuccess
    public async Task<Result<EmailRequest>> SendEmailWithRetryAsync(CancellationToken ct) =>
        await _executor.ExecuteAsync(ct => _emailService.SendEmailAsync(builder, ct), r => r.IsSuccess, ct);
}
```

### Resilience for actions (2)

```csharp
public MyService(ResiliencePipelineProvider<string> pipelineProvider)
{
    var pipeline = pipelineProvider.GetPipeline("my-pipeline");
    await pipeline.ExecuteAsync(async ct => await DoWork(ct), ct);
}
```

### Resilience for HttpClient

```csharp
// Default pipeline (lyo-http)
builder.Services.AddHttpClient<MyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddLyoResilienceHandler();

// Or specify a pipeline name
builder.Services.AddLyoResiliencePipelines(builder.Configuration);
builder.Services.AddHttpClient<MyApiClient>(/* ... */).AddLyoResilienceHandler("my-pipeline");
```

### Resilience for HttpClient (2)

```csharp
// In your service - call HttpClient directly; resilience is already on the client
public class MyService
{
    private readonly MyApiClient _apiClient;

    public MyService(MyApiClient apiClient) => _apiClient = apiClient;

    public async Task<Data> GetDataAsync(CancellationToken ct) =>
        await _apiClient.GetAsync("/data", ct); // No IResilientExecutor here
}
```

## Configuration

- **Nested under service options** (recommended) – resilience config lives in a `Resilience` subsection of your options (e.g. `TwilioOptions:Resilience`). Use
  `AddLyoResiliencePipelinesFromOptions("TwilioOptions")`.
- **Standalone section** – use `AddLyoResiliencePipelines("Lyo:ResiliencePipelines")` or any custom section path.

## Strategy subsections

- `MaxRetryAttempts` (int, default 3)
- `Delay` (TimeSpan, e.g. "00:00:02")
- `MaxDelay` (TimeSpan)
- `BackoffType` ("Constant" | "Linear" | "Exponential")
- `UseJitter` (bool)

## Choosing resilience: actions vs HttpClient

Apply resilience at **one level only** to avoid exponential retries:

| Use case                         | Use this                                    | Do NOT                                                     |
|----------------------------------|---------------------------------------------|------------------------------------------------------------|
| **HTTP calls**                   | `AddLyoResilienceHandler` on the HttpClient | `IResilientExecutor` around code that uses that HttpClient |
| **Non-HTTP** (DB, SDK, file I/O) | `IResilientExecutor`                        | —                                                          |

Wrapping HttpClient-using code with `IResilientExecutor` when that HttpClient already has `AddLyoResilienceHandler` causes nested resilience: each outer retry can trigger multiple
inner retries, leading to exponential retry counts.

## Resilience for actions

Use `IResilientExecutor` for work that does **not** go through HttpClient (e.g. database calls, SDKs, file I/O): Or use `ResiliencePipelineProvider<string>` directly:

## Resilience for HttpClient

Use `AddLyoResilienceHandler` so resilience is applied at the HttpClient level. Call the client directly; do not wrap those calls with `IResilientExecutor`: The pipeline applies
retry, timeout, and circuit breaker to each HTTP request (exception-based; retries on `HttpRequestException`, `TimeoutException`, etc.).

## Metrics

When `IMetrics` is registered (e.g. via `AddLyoMetrics`), the library records:

| Metric                                       | Type    | Description                          |
|----------------------------------------------|---------|--------------------------------------|
| `lyo.resilience.retry`                       | Counter | Each retry attempt (tag: `pipeline`) |
| `lyo.resilience.timeout`                     | Counter | Each timeout                         |
| `lyo.resilience.circuit_breaker.opened`      | Counter | Circuit breaker opened               |
| `lyo.resilience.circuit_breaker.closed`      | Counter | Circuit breaker closed               |
| `lyo.resilience.circuit_breaker.half_opened` | Counter | Circuit breaker half-opened          |
| `lyo.resilience.execution.duration`          | Timing  | Execution duration                   |
| `lyo.resilience.execution.success`           | Counter | Successful executions                |
| `lyo.resilience.execution.failure`           | Counter | Failed executions                    |
| `lyo.resilience.execution.error`             | Error   | Exceptions                           |

All metrics include a `pipeline` tag with the pipeline name.

## Logging

- **Retry**: Warning on each retry with attempt number and delay
- **Timeout**: Warning when an operation times out
- **CircuitBreaker**: Warning when opened; Info when closed or half-opened

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (direct, microsoft)
- `Polly` `8.7.0` — (direct, third-party)
- `Polly.Extensions` `8.7.0` — (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)