# Lyo.Reporting.Api

Authenticated HTTP endpoints for Lyo Reporting. Postgres stays service-only (`ReportService` + EF). `BuildReportingGroup` lives in this package.

## Examples

### Set up the host

```csharp
// One call registers Postgres management (DbContext factory, migrations, CRUD services,
// CSV/XLSX/JSON renderers, ReportService, retention, throttle) plus the API export contributor.
// Equivalent to AddPostgresReportingManagement + AddLyoApiReporting.
services.AddReportingApi(o => {
    o.ConnectionString = cs;
    o.EnableAutoMigrations = true;
});
// Or bind from appsettings ("PostgresReporting" section by default):
// services.AddReportingApiFromConfiguration(builder.Configuration);

services.AddReportingMaintenanceWorker(); // optional retention cleanup + stuck-run sweeper
services.AddReportingWebRenderer(); // optional HTML/PDF
services.AddReportDataProvider<MyReportDataProvider>();
services.AddReportingGenerationProfile("email-client-message", p => p
    .DefaultFormat(ReportFormat.Csv)
    .DefaultFileName("report.csv")
    .DefaultPathPrefix("reports/email"));

// Worker / Discord policy example:
services.AddAuthorization(o => {
    o.AddPolicy("ReportingGenerate", p => p.RequireAuthenticatedUser());
});

var app = builder.Build();
app.BuildReportingGroup(new ReportingApiOptions {
    DefinitionAuth = EndpointAuth.RequireAuthenticatedUser(),
    GenerationAuth = EndpointAuth.RequireAuthenticatedUser(),
    GenerateAuth = EndpointAuth.RequireAuthorization("ReportingGenerate"),
    DownloadAuth = EndpointAuth.RequireAuthenticatedUser(),
    // Download endpoint is only mapped when this factory is set:
    DownloadStreamFactory = (ctx, ct) => {
        var storage = ctx.Services.GetRequiredService<IFileStorageService>();
        return storage.GetFileStreamAsync(ctx.OutputFileId, ct: ct);
    }
});
```

### How a worker calls it

```csharp
await reporting.Generations.GenerateAsync(new GenerateReportReq {
    ReportDefinitionId = definitionId,
    Parameters = [new() { Key = "ClientId", Type = LyoTypeInfo.Guid.FullName, Value = LyoTypeInfo.Guid.ToJson(clientId) }],
    Format = ReportFormat.Csv
});

// Re-run a past generation and stream its output:
var rerun = await reporting.Generations.RerunAsync(generationId);
var (stream, fileName, length) = await reporting.Generations.DownloadAsync(rerun.Id);
```

## Authorization matrix

| Surface | Options property | Endpoints |
| -------------------------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Definitions | `DefinitionAuth` | Export + CRUD |
| Parameters on a definition | `DefinitionAuth` | CRUD under `Reporting/Definition/Parameter` |
| Generations | `GenerationAuth` | Get / Query / Delete / DeleteBulk (include `Parameters`). Delete runs `OnCleanupAsync` so persisted output is removed from storage. |
| Generate | `GenerateAuth` | `POST Reporting/Generation/Generate` (`Parameters` list in the body) |
| Resolve parameter Options | `DefinitionAuth` | `POST Reporting/ResolveParameterOptions` (Sproc/Query picker rows for the workbench; not a public execute-any-sproc API) |
| Rerun | `GenerateAuth` | `POST Reporting/Generation/{id}/Rerun` |
| Download | `DownloadAuth` | `GET Reporting/Generation/{id}/Download` (mapped only when `DownloadStreamFactory` is set) |

If every auth slot shares one policy, prefer `ReportingApiOptions.WithAuth(auth, downloadStreamFactory)` over setting all four properties.

All auth slots default to `EndpointAuth.RequireAuthorization()` (authenticated user). **Breaking change:** previously the defaults were `null` (endpoint fell through to the
builder/host default); anonymous access now requires an explicit `EndpointAuth.Anonymous()` per slot. Setting a property to `null` restores the old fall-through behavior, but
prefer explicit values for Worker and Discord hosts.

## CreatedBy

The authenticated identity always wins: `GenerateReportReq.CreatedBy` is overwritten with `User.Identity.Name` when the caller is authenticated. Client-supplied `CreatedBy` is only honored for unauthenticated/service callers, falling back to `"Unknown"`.

## HTTP status codes

- Validation failures (`ReportValidationException`: unknown keys, bad parameters, malformed/oversized JSON, inactive or missing definition, ad-hoc disabled) → **400** ProblemDetails.
- Concurrency saturation (`ReportBusyException`, see `PostgresReportingOptions.MaxConcurrentGenerations`) → **503** ProblemDetails.
- Download: **404** when the generation or blob is missing, **409** when the generation has no downloadable output (not `Succeeded` or no `OutputFileId`).

## Validating definition writes

- `ReportDataJson` must parse as JSON and stay under `MaxReportDataJsonBytes`.
- Parameter `Type` and `DefaultFormat` must be valid enum values.
- `ValidationRegex` must compile (1s timeout) and stay within the 500-char cap.
- `MaxLength`/`MinLength` must be non-negative and coherent.

## Protecting sensitive fields

`QueryProject` and projected `Export` read raw entities and skip the response mapper's masking, so reporting endpoints deny selecting `Value`/`EncryptedValue` on parameter paths (including nested paths like `Parameters.EncryptedValue` and computed-field templates) via `DeniedSelectFields`. `Get`/`QueryConcrete` map through the response types, which mask parameter values (`***` for encrypted-backed values, `EncryptedValue` never returned).

## Cleanup when a definition is deleted

Deleting a definition cascades its generation rows. Before the delete, `ReportGenerationHooks.OnCleanupAsync` runs for each generation with an `OutputFileId` so the persisted blob can be removed; a hook failure aborts the delete rather than orphaning storage.

## How a worker calls it

Workers call `IReportingClient.Generations.GenerateAsync` with a bearer token that satisfies `GenerateAuth`:

FileStorage hooks, renderers, and data providers run on the API host. Not in the worker process.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Reporting.Postgres` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Csv` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Xlsx` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)