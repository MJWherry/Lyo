# Lyo.Job.Client

Typed HTTP client for the Lyo Job API. Wraps `IApiClient` with run lifecycle and worker-instance endpoints from `Lyo.Job.Models.Constants.Rest.Job`.

## Sub-client on a host API client

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
```

## DI

```csharp
services.AddSingleton<MyApiClient>();
services.AddJobClient<MyApiClient>();

// Or with an explicit route prefix (when not using HttpClient.BaseAddress):
services.AddJobClient(sp => sp.GetRequiredService<IApiClient>(), new JobClientOptions { RoutePrefix = apiBaseUrl });
```

## Route prefix

When `JobClientOptions.RoutePrefix` is set (e.g. `https://localhost:5074`), all routes are built as `{prefix}/Job/Run/...`. When empty, routes are relative and rely on `HttpClient.BaseAddress`.
