# Lyo.Xlsx.Models

XLSX stack contracts and value types. [`Lyo.Xlsx`](../Lyo.Xlsx/README.md) implements them, so you can take `IXlsxService` / `IXlsxReader` / `IXlsxWriter` without a direct ClosedXML or ExcelDataReader reference.

## Contracts

- `IXlsxService`. Surfaces `Writer`, `Reader`, and the read / write / convert / HTML / batch / multi-sheet methods.
- `IXlsxWriter`. Emit enumerables, chosen `PropertyInfo` columns, custom column-name dictionaries, formatter delegates, multi-sheet workbooks (`IReadOnlyDictionary<string, IEnumerable<T>>`), row/column dictionaries (`useFooterRow` treats the last row as a bold footer), and `Lyo.DataTable.Models.DataTable` snapshots (`Footer` is always appended and formatted at row `-2` when present) to a file, `Stream`, or byte array. `CreateDocumentWriter` starts an incremental multi-sheet session. Sync overloads exist on every target. Async, `IAsyncEnumerable<T>` export, custom-header, and formatter overloads sit behind `!NETSTANDARD2_0`.
- `IXlsxReader`. Turn worksheets from files, streams, and byte arrays into a row/column dictionary or `Lyo.DataTable.Models.DataTable` (inside `Result<T>`; `useFooterRow` moves the last body row into `Footer`). `ParseXlsx*AsDataTable` keeps values and merge spans only. `ParseXlsx*AsDataTableWithFormatting` also fills the table's sparse format map. Pick sheets with `ListSheetNames`, by-name / by-index overloads, and `ParseXlsx*AsAllSheets` / `…WithFormatting`. On `net10.0`, forward-only `IAsyncEnumerable` streaming (`ParseXlsx*RowsStreamingAsync`, typed `ParseXlsx*StreamingAsync`) uses ExcelDataReader and does not materialize the sheet. Async overloads sit behind `!NETSTANDARD2_0`.
- `IXlsxDocumentWriter`. Disposable incremental write session: each `AddSheet` / `AddSheetFromDataTable` / `AddSheetFromDictionary` call streams one worksheet. Dispose writes the workbook. On `net10.0`, `AddSheetAsync` takes `IAsyncEnumerable<T>`. Sheet names must be unique in a session (case-insensitive).

## Types

- `XlsxCellValue`. Sealed record holding textual `Value` and merge spans (`ColSpan`, `RowSpan`). Formatting lives on the `DataTable` map as `Lyo.DataTable.Models.DataTableCellFormat`, not on this type.
- `XlsxOptions`. Nested `DataTablePoolingOptions` (`PoolValues`, `PoolFormats`, `PoolingCellThreshold`, default 512).
- `XlsxCellValueExtensions`. Turns `XlsxCellValue` into a thin `IDataTableCell`.
- `XlsxExportResult` / `XlsxParseResult`. Envelopes the higher-level helpers return.

## TFMs

Builds for `netstandard2.0;net10.0`. Async, custom-header, and formatter overloads sit in `#if !NETSTANDARD2_0`, so only the `net10.0` TFM sees them.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)