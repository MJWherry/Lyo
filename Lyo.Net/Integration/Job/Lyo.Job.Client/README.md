# Lyo.Job.Client

Typed HTTP client for the Lyo Job API. Wraps `IApiClient` with run lifecycle methods (`StartAsync`, `LogAsync`, `FinishAsync`, `RequeueAsync`) and worker-instance endpoints from `Lyo.Job.Models.Constants.Rest.Job`.

## Examples

### Register with DI

```csharp
services.AddSingleton<MyApiClient>();
services.AddJobClient<MyApiClient>();

// Or with an explicit route prefix (when not using HttpClient.BaseAddress):
services.AddJobClient(sp => sp.GetRequiredService<IApiClient>(), new JobClientOptions { RoutePrefix = apiBaseUrl });
```

### Sub-client on a host API client

```csharp
public class MyApiClient : ApiClient
{
    public JobClient Jobs { get; }

    public MyApiClient(MyApiClientOptions options, ...) : base(...)
    {
        Jobs = new JobClient(this);
    }
}

// Usage
await myClient.Jobs.Runs.StartAsync(runId);
await myClient.Jobs.Runs.LogAsync(runId, new JobRunLogReq(...));
await myClient.Jobs.Runs.FinishAsync(runId, results);
await myClient.Jobs.Runs.RequeueAsync(runId); // Running -> Queued hand-back (graceful worker shutdown)
await myClient.Jobs.Runs.ResyncQueuedAsync(); // republish due Queued runs missing from RabbitMQ
```

## Route prefix

When `JobClientOptions.RoutePrefix` is set (e.g. `https://localhost:5074`), all routes are built as `{prefix}/Job/Run/...`. When empty, routes are relative and rely on `HttpClient.BaseAddress`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.MessageQueue` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)