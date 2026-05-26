# Lyo.Xlsx.Models

Interfaces and value types for the Lyo XLSX stack. Defines the contract implemented by
[`Lyo.Xlsx`](../Lyo.Xlsx/README.md) so consumers can depend on `IXlsxService` /
`IXlsxImporter` / `IXlsxExporter` without pulling in ClosedXML or ExcelDataReader
directly.

## Interfaces

- `IXlsxService` — façade that exposes `Exporter`, `Importer`, and the full
  read / write / convert / HTML / batch / multi-sheet surface.
- `IXlsxExporter` — write enumerables, selected `PropertyInfo` columns, custom
  column-name dictionaries, formatter delegates, multi-sheet workbooks
  (`IReadOnlyDictionary<string, IEnumerable<T>>`), row/column dictionaries, and
  `Lyo.DataTable.Models.DataTable` snapshots to file, `Stream`, or byte array.
  Sync overloads work on all targets; async, custom-header, and formatter overloads
  are gated on `!NETSTANDARD2_0`.
- `IXlsxImporter` — parse the first worksheet of files, streams, and byte arrays as
  a row/column dictionary or `Lyo.DataTable.Models.DataTable` (wrapped in
  `Result<T>`). Async overloads are gated on `!NETSTANDARD2_0`.

## Models

- `XlsxCellValue` — sealed record with the cell's textual `Value` plus optional font
  (`FontSize`, `FontName`, `FontBold`, `FontItalic`, `FontUnderline`,
  `FontStrikethrough`, `FontColor`), fill (`BackgroundColor`), alignment
  (`HorizontalAlignment`, `VerticalAlignment`), number format (`NumberFormat`),
  rotation/wrap (`TextRotation`, `WrapText`), and border (`BorderTop`, `BorderBottom`,
  `BorderLeft`, `BorderRight`, `BorderColor`) metadata. Use
  `XlsxCellValue.FromValue(string)` for a value-only cell.
- `XlsxCellValueExtensions` — helper extensions over `XlsxCellValue`.
- `XlsxExportResult` / `XlsxParseResult` — result envelopes used by the higher-level
  helpers.

## Multi-targeting

Targets `netstandard2.0;net10.0`. Async, custom-header, and formatter overloads are
wrapped in `#if !NETSTANDARD2_0` and are only visible on the `net10.0` TFM.

## Related projects

- [`Lyo.Xlsx`](../Lyo.Xlsx/README.md) — ClosedXML- and ExcelDataReader-backed
  implementation.
- [`Lyo.DataTable.Models`](../../DataTable/Lyo.DataTable.Models/README.md) — `DataTable`
  type produced by the `*AsDataTable` parse methods.
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — `Result<T>` envelope.
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
