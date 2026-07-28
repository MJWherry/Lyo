# Lyo.Reporting.Client

Typed HTTP client for the Lyo Reporting API (`netstandard2.0;net10.0`).

Workers and Discord bots should call Generate through this client against an API host that runs [`Lyo.Api.Reporting`](../../Api/Lyo.Api.Reporting/README.md) — do **not** host
`ReportService` in the worker.

```csharp
services.AddReportingClient<MyApiClient>();
// or
services.AddReportingClient(sp => sp.GetRequiredService<IApiClient>());
```

```csharp
await reporting.Definitions.CreateAsync(req);
await reporting.DefinitionParameters.CreateAsync(new ReportDefinitionParameterReq {
    ReportDefinitionId = id,
    Key = "ClientId",
    Type = ReportParameterType.Guid,
    Required = true
});
await reporting.Generations.GenerateAsync(new GenerateReportReq {
    ReportDefinitionId = id,
    Parameters = [new ReportGenerationParameterReq("ClientId", ReportParameterType.Guid, clientId.ToString())],
    Format = ReportFormat.Csv // optional; else definition/profile default
});

// Re-run a past generation from its stored snapshot (new generation row):
var rerun = await reporting.Generations.RerunAsync(generationId);

// Stream a generation's persisted output (requires the API host to configure
// ReportingApiOptions.DownloadStreamFactory):
var (content, fileName, contentLength) = await reporting.Generations.DownloadAsync(rerun.Id);
```

Auth is on the underlying `IApiClient` / `HttpClient` (same bearer as other API calls). Output persistence (FileStorage, etc.) is configured on the API host via
`ReportGenerationHooks`.
