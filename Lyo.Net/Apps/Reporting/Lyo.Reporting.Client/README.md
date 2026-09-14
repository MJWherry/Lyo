# Lyo.Reporting.Client

Typed HTTP client targeting the Lyo Reporting API (`netstandard2.0;net10.0`).

Workers and Discord bots should call Generate through this client against an API host that runs [`Lyo.Reporting.Api`](../Lyo.Reporting.Api/README.md). Do **not** host `ReportService` in the worker.

```csharp services.AddReportingClient<MyApiClient>(); services.AddReportingClient<MyApiClient>(o => o.RoutePrefix = "api"); services.AddReportingClientFromConfiguration<MyApiClient>(configuration); // section ReportingClientOptions // or resolve the API client through a factory: services.AddReportingClient(sp => sp.GetRequiredService<IApiClient>()); ```

```csharp await reporting.Definitions.CreateAsync(req); await reporting.DefinitionParameters.CreateAsync(new ReportDefinitionParameterReq { ReportDefinitionId = id, Key = "ClientId", Type = LyoTypeInfo.Guid.FullName, Required = true }); await reporting.Generations.GenerateAsync(new GenerateReportReq { ReportDefinitionId = id, Parameters = [new ReportGenerationParameterReq("ClientId", LyoTypeInfo.Guid, LyoTypeInfo.Guid.ToJson(clientId))], Format = ReportFormat.Csv // optional; else definition/profile default });

// Re-run a past generation from its stored snapshot (new generation row): var rerun = await reporting.Generations.RerunAsync(generationId);

// Generate and rerun responses omit ReportDataJson (it reaches megabytes); ask for it // with IncludeReportData / includeReportData, or read it back through GetAsync. var withJson = await reporting.Generations.RerunAsync(generationId, includeReportData: true);

// Stream a generation's persisted output (requires the API host to configure // ReportingApiOptions.DownloadStreamFactory): var (content, fileName, contentLength) = await reporting.Generations.DownloadAsync(rerun.Id);

// Delete a generation (host OnCleanupAsync removes the stored file): await reporting.Generations.DeleteAsync(rerun.Id); ```

Auth rides on the underlying `IApiClient` / `HttpClient` (the same bearer as other API calls). Output persistence (FileStorage, etc.) is configured on the API host via `ReportGenerationHooks`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)