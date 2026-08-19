# Lyo.Reporting.Client

Typed HTTP client for the Lyo Reporting API (`netstandard2.0;net10.0`).

Workers and Discord bots should call Generate through this client against an API host that runs [`Lyo.Api.Reporting`](../../Api/Lyo.Api.Reporting/README.md) — do **not** host `ReportService` in the worker.

```csharp services.AddReportingClient<MyApiClient>(); // or services.AddReportingClient(sp => sp.GetRequiredService<IApiClient>()); ```

```csharp await reporting.Definitions.CreateAsync(req); await reporting.DefinitionParameters.CreateAsync(new ReportDefinitionParameterReq { ReportDefinitionId = id, Key = "ClientId", Type = ReportParameterType.Guid, Required = true }); await reporting.Generations.GenerateAsync(new GenerateReportReq { ReportDefinitionId = id, Parameters = [new ReportGenerationParameterReq("ClientId", ReportParameterType.Guid, clientId.ToString())], Format = ReportFormat.Csv // optional; else definition/profile default });

// Re-run a past generation from its stored snapshot (new generation row): var rerun = await reporting.Generations.RerunAsync(generationId);

// Stream a generation's persisted output (requires the API host to configure // ReportingApiOptions.DownloadStreamFactory): var (content, fileName, contentLength) = await reporting.Generations.DownloadAsync(rerun.Id);

// Delete a generation (host OnCleanupAsync removes the stored file): await reporting.Generations.DeleteAsync(rerun.Id); ```

Auth is on the underlying `IApiClient` / `HttpClient` (same bearer as other API calls). Output persistence (FileStorage, etc.) is configured on the API host via `ReportGenerationHooks`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Api.Models` — (direct, lyo)
- `Lyo.Query.Models` — (direct, lyo)
- `Lyo.Reporting.Models` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft)