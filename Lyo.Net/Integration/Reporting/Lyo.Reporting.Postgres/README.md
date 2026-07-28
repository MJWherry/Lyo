# Lyo.Reporting.Postgres

PostgreSQL schema (`reporting`), EF migrations, CSV/XLSX/JSON renderers, `ReportService` generation pipeline, and `ReportRetentionService` cleanup.

**HTTP endpoints** live in [`Lyo.Api.Reporting`](../../Api/Lyo.Api.Reporting/README.md) (`BuildReportingGroup`). This package is service + EF only.

## Rendering

`ReportService` resolves `IEnumerable<IReportRenderer>` from DI — it does **not** project-reference `Lyo.Reporting.Web`.

| Format     | Package             | Registration                                                                        |
|------------|---------------------|-------------------------------------------------------------------------------------|
| CSV        | this package        | `AddPostgresReportingManagement` → `CsvReportRenderer` (first grid)                 |
| XLSX       | this package        | `AddPostgresReportingManagement` → `XlsxReportRenderer` (one worksheet per grid)    |
| JSON       | this package        | `AddPostgresReportingManagement` → `JsonReportRenderer` (composition JSON verbatim) |
| HTML / PDF | `Lyo.Reporting.Web` | host calls `AddReportingWebRenderer()` (+ `AddWebRenderer`)                         |

Optional host `IReportDataProvider` / `ReportingGenerationProfile` (keyed by definition `GenerationProfileKey`) supply domain data or pre-rendered files before render.

## DI

```csharp
services.AddLyoQueryServices();
services.AddLocalCache();
services.AddIOTempService();
services.AddPostgresReportingManagement(o => {
    o.ConnectionString = cs;
    o.EnableAutoMigrations = true;
    o.MaxReportDataJsonBytes = 5_000_000;
    o.MaxOutputFileBytes = 50_000_000;
    o.AllowAdHocGeneration = true;              // false = saved definitions only, no JSON overrides
    o.MaxConcurrentGenerations = 4;             // 0 = unlimited; saturation throws ReportBusyException (HTTP 503)
    o.GenerationRetention = TimeSpan.FromDays(90); // null = retention cleanup disabled
});

services.AddReportingWebRenderer(); // optional HTML/PDF
services.AddReportDataProvider<MyProvider>();
services.AddReportingGenerationProfile("my-profile", p => p.DefaultFormat(ReportFormat.Csv));

services.AddReportingGenerationHooks(new ReportGenerationHooks {
    AfterRenderAsync = async (ctx, ct) => {
        var storage = ctx.Services.GetRequiredService<IFileStorageService>();
        var saved = await storage.SaveFileAsync(
            ctx.StagedFilePath!,
            ctx.FileName,
            pathPrefix: ctx.PathPrefix ?? ctx.Request.PathPrefix,
            contentType: ctx.ContentType,
            ct: ct);
        ctx.OutputFileId = saved.Id;
    },
    // Called before a generation row is removed (retention cleanup or definition delete):
    OnCleanupAsync = async (ctx, ct) => {
        if (ctx.OutputFileId is Guid fileId) {
            var storage = ctx.Services.GetRequiredService<IFileStorageService>();
            await storage.DeleteFileAsync(fileId, ct: ct);
        }
    }
});

services.AddLyoApiReporting();
app.BuildReportingGroup(new ReportingApiOptions {
    DefinitionAuth = EndpointAuth.RequireAuthenticatedUser(),
    GenerationAuth = EndpointAuth.RequireAuthenticatedUser(),
    GenerateAuth = EndpointAuth.RequireAuthorization("ReportingGenerate")
});
```

## Hardening options

- `AllowAdHocGeneration` (default `true`): when `false`, `GenerateAsync` requires a saved `ReportDefinitionId` and rejects `ReportDataJson` / `OverrideReportDataJson` payloads.
  Reruns of stored snapshots remain allowed.
- `MaxConcurrentGenerations` (default `0` = unlimited): gates the provider + render section via a process-wide `ReportGenerationThrottle`. When saturated, generate waits briefly
  then throws `ReportBusyException` (mapped to HTTP 503 by the API).
- `GenerationRetention` (default `null` = disabled): age after which terminal generations are eligible for `ReportRetentionService` cleanup.
- Input hygiene: request file names are sanitized (directory segments, `..`, invalid/control characters stripped; length capped) and `ReportDataJson` must parse as JSON — malformed
  payloads fail fast with `ReportValidationException` **before** a generation row is persisted.
- Failure resilience: `Failed` status is persisted with `CancellationToken.None`, so client disconnects can't strand generations in `Running`.

## Parameters

- **Definition schema:** `report_definition_parameter` (Key, Type, default Value, Required, validation, EncryptedValue)
- **Generation instance:** `report_generation_parameter` (Key, Type, Value, EncryptedValue)
- `ReportService.GenerateAsync` merges request `Parameters` over definition defaults, validates, persists generation rows, and passes typed params plus a synthesized Key→Value JSON
  map to providers/renderers for transition. `AllowMultiple` keys serialize as JSON arrays in that map, so no value is lost.

### Validation semantics

- Values are validated against the declared `ReportParameterType` (`Guid`, `Int`, `Long`, `Decimal`, `Bool`, `DateTime`, `DateOnly`, `TimeOnly`, `Json`, `Regex`, `Xml`); `String`/
  `Enum`/`Unknown` accept any string (`Enum` is constrained via `AllowedValues`).
- `ValidationRegex` runs with a 1-second match timeout and a 500-character pattern cap; a timeout or invalid pattern is reported as a validation error, never an unhandled
  exception (ReDoS-safe).
- When generating from a definition, request parameter keys not declared on the definition are rejected with a clear error listing the unknown keys. Definition-less (ad-hoc)
  generates accept any keys.
- A required parameter is satisfied by either `Value` or `EncryptedValue`.
- Validation failures throw `ReportValidationException`, which the API maps to HTTP 400.

Definitions are also validated at **write time** (`ReportDefinitionWriteValidator`, wired into the API create/update/patch hooks): `ReportDataJson` must parse and respect
`MaxReportDataJsonBytes`, `DefaultFormat` and parameter `Type` must be valid enum values, `ValidationRegex` must compile within the length cap, and `MinLength`/`MaxLength` must be
coherent.

## Rerun

`ReportService.RerunAsync(generationId, createdBy)` replays a past generation from its stored snapshot (composition JSON, format, file name, path prefix, parameters) into a **new**
generation row. The saved definition is intentionally not re-read, so a since-changed or deactivated definition cannot alter the rerun.

## Retention cleanup

`ReportRetentionService.CleanupAsync` batch-deletes terminal (`Succeeded`/`Failed`) generations older than `GenerationRetention`, oldest first, keeping in-flight (`Pending`/
`Running`) rows. Before each row is removed, `ReportGenerationHooks.OnCleanupAsync` runs so the host can delete the persisted output blob; a hook failure logs and retains that row.
Emits the `reporting.generation.cleaned` metric.

The service is registered by `AddPostgresReportingManagement` but **not scheduled** — hosts trigger it themselves, e.g. via Lyo.Scheduler or a Lyo.Job interval job:

```csharp
// e.g. inside a scheduled job handler
var retention = scope.ServiceProvider.GetRequiredService<ReportRetentionService>();
var deleted = await retention.CleanupAsync(ct);
```

## Design-time migrations

```bash
export REPORTING_CONNECTION_STRING="Host=localhost;Database=postgres;..."
dotnet ef migrations add YourMigration --project Integration/Reporting/Lyo.Reporting.Postgres
```
