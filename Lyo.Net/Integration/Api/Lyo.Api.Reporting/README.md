# Lyo.Api.Reporting

Authenticated HTTP surface for Lyo Reporting. Postgres stays service-only (`ReportService` + EF); this package owns `BuildReportingGroup`.

## Host setup

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

## Auth matrix

| Surface | Options property | Endpoints |
|---------|------------------|-----------|
| Definitions | `DefinitionAuth` | CRUD + Export |
| Definition parameters | `DefinitionAuth` | CRUD under `Reporting/Definition/Parameter` |
| Generations | `GenerationAuth` | Query / Get only (read-only; include `Parameters`) |
| Generate | `GenerateAuth` | `POST Reporting/Generation/Generate` (body `Parameters` list) |
| Rerun | `GenerateAuth` | `POST Reporting/Generation/{id}/Rerun` |
| Download | `DownloadAuth` | `GET Reporting/Generation/{id}/Download` (mapped only when `DownloadStreamFactory` is set) |

When every surface shares one policy, use `ReportingApiOptions.WithAuth(auth, downloadStreamFactory)` instead of setting all four properties.

All auth surfaces default to `EndpointAuth.RequireAuthorization()` (authenticated user). **Breaking change:** previously the defaults were `null` (endpoint fell through to the builder/host default); anonymous access now requires an explicit `EndpointAuth.Anonymous()` per surface. Setting a property to `null` restores the old fall-through behavior, but prefer explicit values for production Worker/Discord hosts.

## CreatedBy

The authenticated identity always wins: when the caller is authenticated, `GenerateReportReq.CreatedBy` is overwritten with `User.Identity.Name`. Client-supplied `CreatedBy` is only honored for unauthenticated/service callers, falling back to `"Unknown"`.

## Status codes

- Validation failures (`ReportValidationException`: bad parameters, unknown keys, malformed/oversized JSON, inactive or missing definition, ad-hoc disabled) → **400** ProblemDetails.
- Concurrency saturation (`ReportBusyException`, see `PostgresReportingOptions.MaxConcurrentGenerations`) → **503** ProblemDetails.
- Download: **404** when the generation or blob is missing, **409** when the generation has no downloadable output (not `Succeeded` or no `OutputFileId`).

## Definition write-time validation

Create/Update/Patch on definitions and definition parameters run `ReportDefinitionWriteValidator`:

- `ReportDataJson` must parse as JSON and respect `MaxReportDataJsonBytes`.
- `DefaultFormat` and parameter `Type` must be valid enum values.
- `ValidationRegex` must compile (1s timeout) and stay within the 500-char cap.
- `MinLength`/`MaxLength` must be non-negative and coherent.

Failures surface as 400-style CRUD error responses.

## Sensitive field protection

`QueryProject` and projected `Export` read raw entities and bypass the response mapper's masking, so the reporting surfaces deny selecting `EncryptedValue`/`Value` on parameter paths (including nested paths like `Parameters.EncryptedValue` and computed-field templates) via `DeniedSelectFields`. `QueryConcrete`/`Get` map through the response types, which mask parameter values (`***` for encrypted-backed values, `EncryptedValue` never returned).

## Definition delete cleanup

Deleting a definition cascades its generation rows. Before the delete, the host `ReportGenerationHooks.OnCleanupAsync` runs for each generation with an `OutputFileId` so the persisted blob can be removed; a hook failure aborts the delete rather than orphaning storage.

## Worker flow

Workers call `IReportingClient.Generations.GenerateAsync` with a bearer token that satisfies `GenerateAuth`:

```csharp
await reporting.Generations.GenerateAsync(new GenerateReportReq {
    ReportDefinitionId = definitionId,
    Parameters = [new() { Key = "ClientId", Type = ReportParameterType.Guid, Value = clientId.ToString() }],
    Format = ReportFormat.Csv
});

// Re-run a past generation and stream its output:
var rerun = await reporting.Generations.RerunAsync(generationId);
var (stream, fileName, length) = await reporting.Generations.DownloadAsync(rerun.Id);
```

Data providers, renderers, and FileStorage hooks run on the API host — not in the worker process.
