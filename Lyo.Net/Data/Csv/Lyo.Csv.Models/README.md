# Lyo.Csv.Models

Interfaces and value types for the Lyo CSV stack. Defines the contract implemented by
[`Lyo.Csv`](../Lyo.Csv/README.md) so consumers can depend on `ICsvService` /
`ICsvImporter` / `ICsvExporter` without pulling in CsvHelper directly.

## Interfaces

- `ICsvService` — façade with the full read/write/validate/compare/split/combine surface.
  Exposes `Exporter`, `Importer` and `SetEncoding(Encoding)`.
- `ICsvExporter` — write enumerables, selected `PropertyInfo` columns, custom column-name
  dictionaries, formatter delegates, row/column dictionaries, and `Lyo.DataTable` snapshots
  to file, `Stream`, `TextWriter`, string, or byte array. Sync overloads work on all targets;
  async, progress (`IProgress<CsvProgress>`), and append overloads are gated on
  `!NETSTANDARD2_0`.
- `ICsvImporter` — parse files, streams, and byte arrays as typed rows, row/column
  dictionaries, or `Lyo.DataTable.Models.DataTable` (wrapped in `Result<T>`). Async-only
  surface adds streaming (`IAsyncEnumerable<T>`), `CsvParseOptions` (continue-on-error,
  row filter, max rows), chunked processing, statistics, schema validation, column-mapping
  parsing, and file comparison.

## Models

- `CsvParseOptions` — `ContinueOnError`, `OnError`, `RowFilter`, `MaxRows`.
- `CsvSchema` + `CsvColumn` — describe expected columns for `ValidateAsync`.
- `ColumnMapping` — explicit column-name → property mapping for
  `ParseFileWithMappingAsync` / `ParseStreamWithMappingAsync`.
- `CsvParseError` — error metadata surfaced to `CsvParseOptions.OnError`.
- `CsvParseResult` / `CsvExportResult` — result envelopes used by the higher-level helpers.
- `CsvStatistics` — row/column counts produced by `GetStatisticsAsync`.
- `CsvComparisonResult` + `CsvRowDifference` — output of `CompareFilesAsync`.
- `ValidationResult` — output of `ValidateAsync`.
- `CsvProgress` — progress payload for `ExportToCsv*WithProgressAsync`.

## Multi-targeting

Targets `net10.0;netstandard2.0`. Async, streaming, and option-based overloads are wrapped
in `#if !NETSTANDARD2_0` and are only visible on the `net10.0` TFM.

## Related projects

- [`Lyo.Csv`](../Lyo.Csv/README.md) — CsvHelper-backed implementation.
- [`Lyo.DataTable.Models`](../../DataTable/Lyo.DataTable.Models/README.md) — `DataTable`
  type produced by the `*AsDataTable` parse methods.
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — `Result<T>` envelope for
  fallible operations.
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
