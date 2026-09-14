# Lyo.Reporting.Postgres

PostgreSQL schema (`reporting`), EF migrations, CSV/XLSX/JSON renderers, the `ReportService` generation pipeline, and `ReportRetentionService` cleanup.

**HTTP endpoints** live in [`Lyo.Reporting.Api`](../../../Apps/Reporting/Lyo.Reporting.Api/README.md) (`BuildReportingGroup`). This package is service + EF only.

## Examples

### Wire into DI

```csharp
services.AddLyoQueryServices();
services.AddLocalCache();
services.AddIOTempService();
services.AddPostgresReportingManagement(o => {
    o.ConnectionString = cs;
    o.EnableAutoMigrations = true;
    o.MaxReportDataJsonBytes = 5_000_000;
    o.MaxOutputFileBytes = 50_000_000;
    o.AllowAdHocGeneration = false; // default; true accepts caller-authored ReportDataJson
    o.MaxConcurrentGenerations = 4; // 0 = unlimited; saturation throws ReportBusyException (HTTP 503)
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
    // Called before a generation row is removed (retention cleanup, generation delete, or definition delete):
    OnCleanupAsync = async (ctx, ct) => {
        if (ctx.OutputFileId is Guid fileId) {
            var storage = ctx.Services.GetRequiredService<IFileStorageService>();
            await storage.DeleteFileAsync(fileId, ct: ct);
        }
    }
});

// HTTP: Lyo.Reporting.Api AddReportingApi / BuildReportingGroup
```

### Run retention cleanup

```csharp
// e.g. inside a scheduled job handler
var retention = scope.ServiceProvider.GetRequiredService<ReportRetentionService>();
var deleted = await retention.CleanupAsync(ct);
```

### Design-time migrations

```bash
export REPORTING_CONNECTION_STRING="Host=localhost;Database=postgres;..."
dotnet ef migrations add YourMigration --project Plugins/Reporting/Lyo.Reporting.Postgres
```

## Rendering

`IEnumerable<IReportRenderer>` is resolved from DI by `ReportService`. It does **not** project-reference `Lyo.Reporting.Web`.

| Format | Package | Registration |
| ---------- | ------------------- | -------------------------------------------------------------------------------------------------------------------- |
| CSV | this package | `AddPostgresReportingManagement` → `CsvReportRenderer` (every table, appended below a blank separator and title row) |
| XLSX | this package | `AddPostgresReportingManagement` → `XlsxReportRenderer` (one worksheet per table) |
| JSON | this package | `AddPostgresReportingManagement` → `JsonReportRenderer` (composition JSON verbatim) |
| HTML / PDF | `Lyo.Reporting.Web` | host calls `AddReportingWebRenderer()` (+ `AddWebRenderer`) |

Optional host `IReportDataProvider` / `ReportingGenerationProfile` (keyed by definition `GenerationProfileKey`) can supply domain data or pre-rendered files before render.

## Hardening options

- `AllowAdHocGeneration` (default `false`, **breaking change** — it used to default to `true`): when `false`, `GenerateAsync` requires a saved `ReportDefinitionId` and rejects `ReportDataJson` / `OverrideReportDataJson` payloads. Composition JSON authors report markup, so accepting it from a request hands rendering control to the caller. Reruns of stored snapshots remain allowed. Hosts that intentionally offer ad-hoc generation set `o.AllowAdHocGeneration = true`.
- `PreRenderedFileRoots` (default empty = the OS temp directory): filesystem roots a data provider's `PreRenderedFilePath` may live under. Generation copies that path into staging, so confinement stops a provider from publishing any file the host can read. Symlinks are resolved before the check and traversal outside a root is rejected with `ReportValidationException`.
- Rendered markup is sanitized: `Html` and `Chart` content blocks pass through `ReportHtmlSanitizer` (allowlist of elements and attributes) before `ReportViewer` writes them to the DOM, so composition JSON cannot carry script, event handlers, or `javascript:` URLs.
- `MaxConcurrentGenerations` (default `0` = unlimited): gates the provider + render section via a process-wide `ReportGenerationThrottle`. When saturated, generate waits briefly then throws `ReportBusyException` (mapped to HTTP 503 by the API).
- `GenerationRetention` (default `null` = disabled): age after which terminal generations are eligible for `ReportRetentionService` cleanup.
- Input hygiene: request file names are sanitized (directory segments, `..`, invalid/control characters stripped; length capped) and `ReportDataJson` must parse as JSON. Malformed payloads fail fast with `ReportValidationException` **before** a generation row is persisted.
- Failure resilience: `Failed` status is persisted with `CancellationToken.None`, so client disconnects can't strand generations in `Running`.
- `StuckGenerationTimeout` recovery guards on liveness, not just age: generations executing in this process are skipped via the singleton `ReportGenerationLiveTracker`, and when `GenerationTimeout` is longer than the stuck timeout the cutoff stretches to `GenerationTimeout` plus a five-minute grace. Multi-host deployments should set `GenerationTimeout` so recovery can bound a legitimate run.
- `ReportGenerationRes.ReportDataJson` is null on generate and rerun responses unless the caller sets `IncludeReportData` (**breaking change**). The composition JSON reaches `MaxReportDataJsonBytes` (5 MB by default) and most callers only need the status and output identifiers; query and get responses still carry it.

## Parameters

- **Definition schema:** `report_definition_parameter` (Key, Type, default Value, Required, validation, EncryptedValue, Options JSON picker source, DefaultKind + DefaultTemplate for expression defaults)
- **Generation instance:** `report_generation_parameter` (Key, Type, Value, EncryptedValue)
- `ReportService.GenerateAsync` merges request `Parameters` over definition defaults, validates, persists generation rows, and passes typed params plus a synthesized Key→Value JSON map to providers/renderers for transition. After merge, `IReportDataSourceResolver` runs Query/Sproc parameter Options and fills `FromParameter` and Query/Sproc tables so renderers still see static rows. Collection types already serialize as JSON arrays in that map (`{"Tag":["a","b"]}`). Generate writes via EF (not CRUD), so it busts `entity:reportgeneration` and `entity:reportdefinition` query-cache tags after persist.
- A parameter with `DefaultKind = Expression` puts a SmartFormat template in `DefaultTemplate` instead of a literal `Value`. `MergeParameters` renders it through the `LyoTemplateResolver` resolved from DI (registered by `Lyo.Formatter`'s `AddFormatterService`) and normalizes the result to the declared `Type`, so a `System.DateTime` parameter defaulting to `{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}` reaches providers as a date. A resolver that is missing, throws, or renders the wrong type fails the generate with a caller-facing message rather than silently dropping the parameter.

## Parameter validation

- Values are JSON text validated via `LyoTypeInfo.TryValidateJson` against the stored CLR FullName (`System.Int32`, `List<string>`, …). Legacy enum names (`Int`, `String`, `Json`, …) still resolve. `Enum` is constrained via `AllowedValues`.
- `ValidationRegex` runs with a 1-second match timeout and a 500-character pattern cap; a timeout or invalid pattern is reported as a validation error, never an unhandled exception (ReDoS-safe).
- When generating from a definition, request parameter keys not declared on the definition are rejected with a clear error listing the unknown keys. Definition-less (ad-hoc) generates accept any keys.
- A required parameter is satisfied by either `Value` or `EncryptedValue`.
- Write-time checks live in `ReportDefinitionWriteValidator`, which runs `LyoParameterValidator.ValidateSpec` — including the rule that an expression default must carry a template and a literal default must not — so bad metadata fails on save instead of on every generate that needs the default.
- Validation failures throw `ReportValidationException`, which the API maps to HTTP 400.

## Rerun

A past generation is replayed by `ReportService.RerunAsync(generationId, createdBy)` from its stored snapshot (composition JSON, format, file name, path prefix, parameters) into a **new** generation row. The saved definition is intentionally not re-read, so a since-changed or deactivated definition cannot alter the rerun.

## Retention cleanup

Terminal (`Succeeded`/`Failed`) generations older than `GenerationRetention` are batch-deleted by `ReportRetentionService.CleanupAsync`, oldest first, keeping in-flight (`Pending`/ `Running`) rows. Before each row is removed, `ReportGenerationHooks.OnCleanupAsync` runs so the host can delete the persisted output blob; a hook failure logs and retains that row. Emits the `reporting.generation.cleaned` metric. The service is registered by `AddPostgresReportingManagement` but **not scheduled**. hosts trigger it themselves, e.g. via Lyo.Scheduler or a Lyo.Job interval job:

## Generation cost

- `CsvReportRenderer` writes to the output file in row chunks instead of materializing every formatted cell, so peak memory tracks the chunk size rather than the report size.
- The `MaxConcurrentGenerations` slot covers the provider and render work only. It is released as soon as the output is staged, so storage uploads, the terminal status write, and the failure path no longer occupy a slot.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Audit` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
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
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
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