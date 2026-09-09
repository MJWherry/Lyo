# Lyo.Xlsx

Implements [`Lyo.Xlsx.Models`](../Lyo.Xlsx.Models/README.md). `XlsxService` composes an `XlsxWriter` (streaming `DocumentFormat.OpenXml` writer) and an `XlsxReader` (ExcelDataReader / ClosedXML) to read and write XLSX workbooks from files, streams, and byte arrays, with helpers for converting to CSV, HTML, and `Lyo.DataTable`. Multi-targets `netstandard2.0;net10.0`; async, custom-header, and formatter export overloads exist only on `net10.0`.

Export streams rows straight into the worksheet part via `OpenXmlWriter`, so memory stays bounded regardless of row count. Column widths are approximated from a bounded sample of the leading rows rather than a full-workbook auto-fit pass.

## Features

- Strongly-typed read/write through `IEnumerable<T>`; on `net10.0`, `IAsyncEnumerable<T>` export and forward-only streaming reads (`ParseXlsx*RowsStreamingAsync` / typed `ParseXlsx*StreamingAsync`) via ExcelDataReader.
- Multi-sheet workbooks through `IReadOnlyDictionary<string, IEnumerable<T>>` (sheet name → rows).
- On read, control sheets with `ListSheetNames`, parse one sheet by name or zero-based index (`ParseXlsx*AsDictionary` / `ParseXlsx*AsDataTable` / `ParseXlsx*AsDataTableWithFormatting`), or parse every sheet at once (`ParseXlsx*AsAllSheets` / `…WithFormatting`).
- Thin vs formatted DataTable import: `AsDataTable` skips ClosedXML style reads (values + spans only); `AsDataTableWithFormatting` fills the sparse `(row,col) → DataTableCellFormat` map (absent key = no format; default black/white/theme colors and font size/name are stripped so unstyled sheets stay empty). Export writes styles for mapped cells (unique custom styles capped at 512). Skipping style reads does not remove ClosedXML workbook-load cost.
- Parse-scoped value/format pooling through `XlsxOptions.Pooling` (`PoolValues`, `PoolFormats`, `PoolingCellThreshold` default 512; one `DataTableValueInterner` per parse).
- Incremental multi-sheet write sessions through `CreateDocumentWriter` / `IXlsxDocumentWriter` (typed rows, selected properties, `DataTable`, or row/column dictionary per sheet; dispose finalizes the workbook).
- Cell spanning: `DataTable` cells that carry `ColSpan`/`RowSpan` round-trip as XLSX merged ranges (`<mergeCells>` on write, `MergedRanges` on read).
- Export selected properties (`IReadOnlyList<PropertyInfo>`). On `net10.0`, also custom-header (`IReadOnlyDictionary<string, PropertyInfo>`) and formatter (`IReadOnlyDictionary<string, Func<T, string>>`) exports.
- Read and write a row/column dictionary (`IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>`).
- Round-trip `Lyo.DataTable.Models.DataTable` including `Footer` (export always appends footer when present; import peels the last body row when `useFooterRow: true`; formats on map row `-2`) plus HTML table export (`ExportToHtmlTable`).
- Convert XLSX → CSV to a file, stream, or byte array (`ConvertXlsxToCsv*`) with optional `Encoding`.
- Batch parse helpers (`BatchParseFilesAsDataTable` / `…Async`) that return one `Result<DataTable>` per input path.
- When wrapping failures in `Result<T>`, uses `XlsxErrorCodes` constants (`XLSX_EXPORT_FAILED`, `XLSX_PARSE_FAILED`, `XLSX_OPERATION_CANCELLED`, `XLSX_FILE_OPERATION_FAILED`, `XLSX_CONVERT_TO_CSV_FAILED`).

## Examples

### Add XlsxService in DI

```csharp
using Lyo.Xlsx;

services.AddXlsxService();
services.AddXlsxService(o => {
    o.Pooling.PoolValues = true;
    o.Pooling.PoolFormats = true;
    o.Pooling.PoolingCellThreshold = 512; // 0 = always when enabled
});
```

### Export and import

```csharp
public sealed class ReportingService(IXlsxService xlsx)
{
    public async Task<byte[]> ExportAsync(IEnumerable<Person> rows, CancellationToken ct)
        => await xlsx.ExportToXlsxBytesAsync(rows, worksheetName: "People", ct: ct);

    public async Task<Result<DataTable>> ImportAsync(string path, CancellationToken ct)
        => await xlsx.ParseXlsxFileAsDataTableAsync(path, useHeaderRow: true, ct: ct);
}

public sealed record Person(int Id, string Name, int Age);
```

### Where exports can go

```csharp
xlsx.ExportToXlsx(rows, "out.xlsx");
xlsx.ExportToXlsx(rows, stream);
byte[] bytes = xlsx.ExportToXlsxBytes(rows);

await xlsx.ExportToXlsxAsync(rows, "out.xlsx", worksheetName: "Data", ct: ct);
await xlsx.ExportToXlsxAsync(rows, stream, worksheetName: "Data", ct: ct);
byte[] bytesAsync = await xlsx.ExportToXlsxBytesAsync(rows, "Data", ct);
```

### Pick and rename columns

```csharp
IReadOnlyList<PropertyInfo> selected = [
    typeof(Person).GetProperty(nameof(Person.Name))!,
    typeof(Person).GetProperty(nameof(Person.Age))!,
];
xlsx.ExportToXlsx(rows, selected, "out.xlsx");

IReadOnlyDictionary<string, PropertyInfo> namedColumns = new Dictionary<string, PropertyInfo> {
    ["Full Name"] = typeof(Person).GetProperty(nameof(Person.Name))!,
    ["Years"] = typeof(Person).GetProperty(nameof(Person.Age))!,
};
await xlsx.ExportToXlsxAsync(rows, namedColumns, stream, ct: ct);

IReadOnlyDictionary<string, Func<Person, string>> formatters = new Dictionary<string, Func<Person, string>> {
    ["Display"] = p => $"{p.Name} ({p.Age})",
    ["Id"] = p => p.Id.ToString("D6"),
};
await xlsx.ExportToXlsxAsync(rows, formatters, stream, ct: ct);
```

### DataTable, dictionary, and HTML

```csharp
Result<DataTable> thin = xlsx.ParseXlsxFileAsDataTable("in.xlsx", useHeaderRow: true, useFooterRow: true);
Result<DataTable> styled = xlsx.ParseXlsxFileAsDataTableWithFormatting("in.xlsx", useHeaderRow: true, useFooterRow: true);
xlsx.ExportToXlsxFromDataTable(styled.ValueOrThrow(), "out.xlsx"); // writes Footer + formats when present

var grid = xlsx.ParseXlsxFileAsDictionary("in.xlsx");
xlsx.ExportToXlsxFromDictionary(grid, "out.xlsx", useHeaderRow: true, useFooterRow: true);

string html = xlsx.ExportToHtmlTable(File.ReadAllBytes("in.xlsx"), useHeaderRow: true, useFooterRow: true);
```

### XLSX ↔ CSV

```csharp
xlsx.ConvertXlsxToCsv("in.xlsx", "out.csv");
xlsx.ConvertXlsxToCsv(inputStream, outputStream, Encoding.UTF8);
byte[] csv = xlsx.ConvertXlsxToCsvBytes(File.ReadAllBytes("in.xlsx"));

await xlsx.ConvertXlsxToCsvAsync("in.xlsx", "out.csv", Encoding.UTF8, ct);
byte[] csvAsync = await xlsx.ConvertXlsxToCsvBytesAsync(inputStream, Encoding.UTF8, ct);
```

### Batch parses

```csharp
IReadOnlyList<Result<DataTable>> sync =
    xlsx.BatchParseFilesAsDataTable(paths, useHeaderRow: true, useFooterRow: true);

IReadOnlyList<Result<DataTable>> async =
    await xlsx.BatchParseFilesAsDataTableAsync(paths, useHeaderRow: true, useFooterRow: true, ct);
```

## Benchmarks

Async workbook export of 100,000 rows finishes in under a second.

- Portfolio suite: `xlsx`
- [XLSX async export](/benchmarks/xlsx)

## Dependency injection

`AddXlsxService` registers a singleton `XlsxService` and maps `IXlsxService`, `IXlsxWriter`, and `IXlsxReader` to that same instance. Overloads accept `Action<XlsxOptions>`, an options instance, or `AddXlsxServiceFromConfiguration` (binds `Xlsx` and optional `DataTablePooling` sections).

## Multi-sheet workbooks

```csharp
var workbook = new Dictionary<string, IEnumerable<Person>> {
    ["Active"] = activePeople,
    ["Archived"] = archivedPeople,
};
xlsx.ExportToXlsx(workbook, "people.xlsx");
await xlsx.ExportToXlsxAsync(workbook, stream, ct);
```

For heterogeneous sheets (different row types or sources per sheet), open an
incremental writing session; each `AddSheet*` call streams one worksheet, and
disposing the session finalizes the workbook:

```csharp
using (var doc = xlsx.CreateDocumentWriter("report.xlsx"))
{
    doc.AddSheet("People", people); // typed rows
    doc.AddSheet("Names", people, selectedProps); // selected properties
    doc.AddSheetFromDataTable("Summary", summaryTable); // Lyo DataTable
    doc.AddSheetFromDictionary("Raw", grid, useHeaderRow: true, useFooterRow: true);
}
```

Duplicate sheet names (case-insensitive) are rejected. `CreateDocumentWriter(stream)`
leaves the destination stream open for the caller; the file-path overload closes the
file on dispose.

## Sheet control (read)

```csharp
IReadOnlyList<string> names = xlsx.ListSheetNames("in.xlsx");

// Select a sheet by name or zero-based index.
var dict = xlsx.ParseXlsxBytesAsDictionary(bytes, "Archived");
Result<DataTable> dt = xlsx.ParseXlsxFileAsDataTable("in.xlsx", 1, useHeaderRow: true);
Result<DataTable> styled = xlsx.ParseXlsxFileAsDataTableWithFormatting("in.xlsx", 1, useHeaderRow: true);

// Or parse everything, keyed by sheet name in workbook order.
IReadOnlyDictionary<string, DataTable> all = xlsx.ParseXlsxStreamAsAllSheets(stream);
```

The no-arg parse methods keep their first-sheet behavior. `AsDataTable` is thin (no styles); use `AsDataTableWithFormatting` when you need the sparse format map. Async variants of all sheet-control methods are available on `net10.0`.

## Style export limits

Dynamic OpenXML styles cover the fields on `DataTableCellFormat` (RGB colors, common borders/align/numFmt). Theme colors do not round-trip on import (`TryGetColorHex` returns null for theme). Unique custom cell formats cap at 512 per workbook; further formats fall back to the default style. FontSize/FontName are intentionally not imported (ClosedXML defaults would fill the sparse map).

## Types (generated)

- `Extensions`
- `XlsxErrorCodes`
- `XlsxService`

## License

Copyright © Lyo

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Xlsx.Models` (direct, lyo)
- `ClosedXML` `0.105.0` (direct, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (direct, third-party)
- `ExcelDataReader` `3.9.0` (direct, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (direct, third-party)
- `Microsoft.Extensions.Configuration` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `System.Text.Encoding.CodePages` `10.0.5` (direct, microsoft)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)