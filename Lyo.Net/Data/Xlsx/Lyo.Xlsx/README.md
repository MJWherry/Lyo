# Lyo.Xlsx

Implementation of
[`Lyo.Xlsx.Models`](../Lyo.Xlsx.Models/README.md). `XlsxService` composes an
`XlsxWriter` (streaming `DocumentFormat.OpenXml` writer) and an `XlsxReader`
(ExcelDataReader / ClosedXML) to read and write XLSX workbooks from files, streams,
and byte arrays, with helpers for converting to CSV, HTML, and `Lyo.DataTable`.
Multi-targets `netstandard2.0;net10.0`; async, custom-header, and formatter export
overloads are only available on `net10.0`.

Export streams rows straight into the worksheet part via `OpenXmlWriter`, keeping
memory bounded regardless of row count. Column widths are approximated from a bounded
sample of the leading rows rather than a full-workbook auto-fit pass.

## Features

- Strongly-typed read/write via `IEnumerable<T>`.
- Multi-sheet workbooks via `IReadOnlyDictionary<string, IEnumerable<T>>` (sheet name → rows).
- Sheet control on read: `ListSheetNames`, parse a specific sheet by name or zero-based index (`ParseXlsx*AsDictionary` / `ParseXlsx*AsDataTable` overloads), or parse every sheet at once (`ParseXlsx*AsAllSheets`).
- Incremental multi-sheet writing sessions via `CreateDocumentWriter` / `IXlsxDocumentWriter` (typed rows, selected properties, `DataTable`, or row/column dictionary per sheet; dispose finalizes the workbook).
- Cell spanning: `DataTable` cells with `ColSpan`/`RowSpan` round-trip as XLSX merged ranges (`<mergeCells>` on write, `MergedRanges` on read).
- Selected-property export (`IReadOnlyList<PropertyInfo>`) and, on `net10.0`, custom-header (`IReadOnlyDictionary<string, PropertyInfo>`) and formatter (`IReadOnlyDictionary<string, Func<T, string>>`) exports.
- Row/column dictionary (`IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>`) read and write.
- `Lyo.DataTable.Models.DataTable` round-trip and HTML table export (`ExportToHtmlTable`).
- XLSX → CSV conversion to file, stream, or byte array (`ConvertXlsxToCsv*`) with optional `Encoding`.
- Batch parse helpers (`BatchParseFilesAsDataTable` / `…Async`) returning one `Result<DataTable>` per input path.
- `XlsxErrorCodes` constants (`XLSX_EXPORT_FAILED`, `XLSX_PARSE_FAILED`, `XLSX_OPERATION_CANCELLED`, `XLSX_FILE_OPERATION_FAILED`, `XLSX_CONVERT_TO_CSV_FAILED`) used when wrapping failures in `Result<T>`.

## Examples

### Register with DI

```csharp
using Lyo.Xlsx;

services.AddXlsxService();
```

### Quick start

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

### Export targets

```csharp
xlsx.ExportToXlsx(rows, "out.xlsx");
xlsx.ExportToXlsx(rows, stream);
byte[] bytes = xlsx.ExportToXlsxBytes(rows);

await xlsx.ExportToXlsxAsync(rows, "out.xlsx", worksheetName: "Data", ct: ct);
await xlsx.ExportToXlsxAsync(rows, stream, worksheetName: "Data", ct: ct);
byte[] bytesAsync = await xlsx.ExportToXlsxBytesAsync(rows, "Data", ct);
```

### Column shaping

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

### DataTable, dictionary, and HTML helpers

```csharp
Result<DataTable> parsed = xlsx.ParseXlsxFileAsDataTable("in.xlsx", useHeaderRow: true);
xlsx.ExportToXlsxFromDataTable(parsed.ValueOrThrow(), "out.xlsx");

var grid = xlsx.ParseXlsxFileAsDictionary("in.xlsx");
xlsx.ExportToXlsxFromDictionary(grid, "out.xlsx", useHeaderRow: true);

string html = xlsx.ExportToHtmlTable(File.ReadAllBytes("in.xlsx"), useHeaderRow: true);
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
    xlsx.BatchParseFilesAsDataTable(paths, useHeaderRow: true);

IReadOnlyList<Result<DataTable>> async =
    await xlsx.BatchParseFilesAsDataTableAsync(paths, useHeaderRow: true, ct);
```

## Benchmarks

- Portfolio suite: `xlsx`

## Dependency injection

`AddXlsxService` registers a singleton `XlsxService` and routes `IXlsxService`, `IXlsxWriter`, and `IXlsxReader` to the same instance.

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
    doc.AddSheetFromDictionary("Raw", grid, useHeaderRow: true);
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

// Or parse everything, keyed by sheet name in workbook order.
IReadOnlyDictionary<string, DataTable> all = xlsx.ParseXlsxStreamAsAllSheets(stream);
```

The no-arg parse methods keep their first-sheet behavior. Async variants of all
sheet-control methods are available on `net10.0`.

## Public API (generated)

- `Extensions`
- `XlsxErrorCodes`
- `XlsxService`

## License

Copyright © Lyo

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Lyo.Xlsx.Models` — (direct, lyo)
- `ClosedXML` `0.105.0` — (direct, third-party)
- `DocumentFormat.OpenXml` `3.1.1` — (direct, third-party)
- `ExcelDataReader` `3.9.0` — (direct, third-party)
- `ExcelDataReader.DataSet` `3.9.0` — (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)