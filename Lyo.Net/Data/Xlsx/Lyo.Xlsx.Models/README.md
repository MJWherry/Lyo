# Lyo.Xlsx.Models

Interfaces and value types for the Lyo XLSX stack. Defines the contract implemented by [`Lyo.Xlsx`](../Lyo.Xlsx/README.md) so consumers can depend on `IXlsxService` / `IXlsxReader` / `IXlsxWriter` without pulling in ClosedXML or ExcelDataReader directly.

## Interfaces

- `IXlsxService` — façade that exposes `Writer`, `Reader`, and the full read / write / convert / HTML / batch / multi-sheet surface.
- `IXlsxWriter` — write enumerables, selected `PropertyInfo` columns, custom column-name dictionaries, formatter delegates, multi-sheet workbooks (`IReadOnlyDictionary<string, IEnumerable<T>>`), row/column dictionaries (`useFooterRow` peels last row as bold footer), and `Lyo.DataTable.Models.DataTable` snapshots (always appends `Footer` + formats at row `-2` when present) to file, `Stream`, or byte array. `CreateDocumentWriter` opens an incremental multi-sheet session. Sync overloads work on all targets; async, custom-header, and formatter overloads are gated on `!NETSTANDARD2_0`.
- `IXlsxReader` — parse worksheets of files, streams, and byte arrays as a row/column dictionary or `Lyo.DataTable.Models.DataTable` (wrapped in `Result<T>`; `useFooterRow` peels the last body row into `Footer`). `ParseXlsx*AsDataTable` is values + merge spans only; `ParseXlsx*AsDataTableWithFormatting` also fills the table's sparse format map. Sheet control via `ListSheetNames`, by-name / by-index overloads, and `ParseXlsx*AsAllSheets` / `…WithFormatting`. Async overloads are gated on `!NETSTANDARD2_0`.
- `IXlsxDocumentWriter` — disposable incremental writing session: each `AddSheet` / `AddSheetFromDataTable` / `AddSheetFromDictionary` call streams one worksheet; dispose finalizes the workbook. Sheet names are unique per session (case-insensitive).

## Models

- `XlsxCellValue` — sealed record with textual `Value` plus merge spans (`ColSpan`, `RowSpan`). Formatting uses `Lyo.DataTable.Models.DataTableCellFormat` on the `DataTable` map, not this type.
- `XlsxOptions` — includes nested `DataTablePoolingOptions` (`PoolValues`, `PoolFormats`, `PoolingCellThreshold`, default 512).
- `XlsxCellValueExtensions` — maps `XlsxCellValue` to a thin `IDataTableCell`.
- `XlsxExportResult` / `XlsxParseResult` — result envelopes used by the higher-level helpers.

## Multi-targeting

Targets `netstandard2.0;net10.0`. Async, custom-header, and formatter overloads are wrapped in `#if !NETSTANDARD2_0` and are only visible on the `net10.0` TFM.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.DataTable.Models` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)