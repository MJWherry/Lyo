# Lyo.Api.Import.Xlsx

Registers an XLSX import handler. A null sheet name uses the first worksheet. A missing sheet is a document error.

## Features

- AddXlsxImport registers IXlsxService and the XLSX handler.

## Examples

### Registration

```csharp
services.AddLyoApiImport();
services.AddXlsxImport();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Import` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `ClosedXML` `0.105.0` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)