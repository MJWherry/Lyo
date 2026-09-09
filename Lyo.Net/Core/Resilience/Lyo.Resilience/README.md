# Lyo.Resilience

Thin Polly wrapper for resilience pipelines, with appsettings binding and built-in logging. Library-specific pipeline definitions are not included; pipelines are defined entirely via configuration.

## Features

- **Default pipelines.** `lyo-http` and `lyo-basic` with timeout and retry. Usable without config.
- Load resilience pipelines from any `IConfiguration` source (including `appsettings.json`).
- Timeout, Retry, and CircuitBreaker strategies.
- Logging for timeouts, retries, and circuit breaker state changes.
- **Actions.** `IResilientExecutor` uses the default pipeline. Pass a pipeline name to override.
- **Result types.** Pass an `isSuccess` predicate to retry when methods return a failed Result instead of throwing.
- **HttpClient.** `AddLyoResilienceHandler()` uses the default pipeline, or pass a pipeline name.
- Wires `ResiliencePipelineProvider<string>` and `Polly.Extensions` into DI.

## Examples

### Nested under service options (preferred)

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

### Nested under service options (preferred) (2)

```csharp
services.AddLyoResiliencePipelinesFromOptions(builder.Configuration, "TwilioOptions");
// Loads from TwilioOptions:Resilience
```

### Standalone section

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

### Standalone section (2)

```csharp
services.AddLyoResiliencePipelines(builder.Configuration); // default: Lyo:ResiliencePipelines
// Or: services.AddLyoResiliencePipelines(builder.Configuration, "CustomSection:Path");
```

### Default pipelines (first steps)

```csharp
// Adds lyo-basic and lyo-http pipelines (retry + timeout)
builder.Services.AddLyoResilienceDefaults();

// Or: AddResilientExecutor registers defaults automatically
builder.Services.AddResilientExecutor();
```

### Resilience around actions

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

### Resilience around actions (2)

```csharp
public MyService(ResiliencePipelineProvider<string> pipelineProvider)
{
    var pipeline = pipelineProvider.GetPipeline("my-pipeline");
    await pipeline.ExecuteAsync(async ct => await DoWork(ct), ct);
}
```

### Resilience on HttpClient

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

### Resilience on HttpClient (2)

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

- **Nested under service options** (preferred). Resilience config lives in a `Resilience` subsection of your options (e.g. `TwilioOptions:Resilience`). Use `AddLyoResiliencePipelinesFromOptions("TwilioOptions")`.
- **Standalone section.** Use `AddLyoResiliencePipelines("Lyo:ResiliencePipelines")` or any custom section path.

## Subsections per strategy

- `MaxRetryAttempts` (int, default 3)
- `Delay` (TimeSpan, e.g. "00:00:02")
- `MaxDelay` (TimeSpan)
- `BackoffType` ("Linear" | "Constant" | "Exponential")
- `UseJitter` (bool)

## Actions versus HttpClient

Apply resilience at **one level only** so retries do not nest exponentially:

| Use case | Use this | Do NOT |
| ---------------------------- | ------------------------------------------- | ---------------------------------------------------------- |
| HTTP calls | `AddLyoResilienceHandler` on the HttpClient | `IResilientExecutor` around code that uses that HttpClient |
| Non-HTTP (SDK, DB, file I/O) | `IResilientExecutor` | |

Do not wrap HttpClient work in `IResilientExecutor` if that same HttpClient already has `AddLyoResilienceHandler`. The two layers nest: each outer retry can fire several
inner retries, and the retry count grows exponentially.

## Resilience around actions

Use `IResilientExecutor` for work that does **not** go through HttpClient (e.g. SDK calls, database work, file I/O): Or use `ResiliencePipelineProvider<string>` directly:

## Resilience on HttpClient

Use `AddLyoResilienceHandler` so resilience is applied at the HttpClient level. Call the client directly; do not wrap those calls with `IResilientExecutor`: The pipeline applies timeout, retry, and circuit breaker to each HTTP request (exception-based; retries on `TimeoutException`, `HttpRequestException`, etc.).

## Metrics

Register `IMetrics` (for example `AddLyoMetrics`) and the library records:

| Metric | Type | Description |
| -------------------------------------------- | ------- | ------------------------------------ |
| `lyo.resilience.retry` | Counter | Each retry attempt (tag: `pipeline`) |
| `lyo.resilience.timeout` | Counter | Each timeout |
| `lyo.resilience.circuit_breaker.opened` | Counter | Circuit breaker opened |
| `lyo.resilience.circuit_breaker.closed` | Counter | Circuit breaker closed |
| `lyo.resilience.circuit_breaker.half_opened` | Counter | Circuit breaker half-opened |
| `lyo.resilience.execution.duration` | Timing | Execution duration |
| `lyo.resilience.execution.success` | Counter | Successful executions |
| `lyo.resilience.execution.failure` | Counter | Failed executions |
| `lyo.resilience.execution.error` | Error | Exceptions |

Every metric includes a `pipeline` tag with the pipeline name.

## Logging

- **Retry.** Warning on each retry with delay and attempt number.
- **Timeout.** Warning when an operation times out.
- **CircuitBreaker.** Warning when opened. Info when half-opened or closed.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Polly` `8.7.0` (direct, third-party)
- `Polly.Extensions` `8.7.0` (direct, third-party)