# Lyo.Api.Export

Optional export feature for [Lyo.Api](../Lyo.Api/README.md). Registers the **Export** CRUD endpoint and `IExportService<TContext>`.

## Setup

```csharp
builder.Services.AddLyoCrudServices<MyDbContext>();
builder.Services.AddLyoApiExport<MyDbContext>();
builder.Services.AddCsvExport();   // Lyo.Api.Export.Csv
builder.Services.AddXlsxExport();  // Lyo.Api.Export.Xlsx
```

Enable export on endpoint builders with `ExportApiFeature.Instance`:

```csharp
.WithCrud(ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, config)
// or
.WithCrud(crud => crud.WithFlags(ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance))
```

JSON export is built into `Lyo.Api` via `AddBuiltInExportFormatHandlers()` (called from `AddLyoCrudServices`). CSV and XLSX require the respective format addon packages.
