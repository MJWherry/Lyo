# Lyo.Api.Export

Optional export feature for [Lyo.Api](../Lyo.Api/README.md). Registers the **Export** CRUD endpoint and `IExportService<TContext>`.

## Examples

### Setup

```csharp
builder.Services.AddLyoCrudServices<MyDbContext>();
builder.Services.AddLyoApiExport<MyDbContext>();
builder.Services.AddCsvExport(); // Lyo.Api.Export.Csv
builder.Services.AddXlsxExport(); // Lyo.Api.Export.Xlsx
```

### Setup (2)

```csharp
.WithCrud(ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, config)
// or
.WithCrud(crud => crud.WithFlags(ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance))
```

## Setup

Enable export on endpoint builders with `ExportApiFeature.Instance`: JSON export is built into `Lyo.Api` via `AddBuiltInExportFormatHandlers()` (called from `AddLyoCrudServices`).
CSV and XLSX require the respective format addon packages.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` — (direct, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Cache` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` — (transitive, lyo)
- `Lyo.Diff` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Formatter` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` — (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` — (transitive, microsoft)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` — (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)