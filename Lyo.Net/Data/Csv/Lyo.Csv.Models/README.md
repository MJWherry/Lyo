# Lyo.Csv.Models

Interfaces and value types for the Lyo CSV stack. Defines the contract implemented by [`Lyo.Csv`](../Lyo.Csv/README.md) so consumers can depend on `ICsvService` / `ICsvReader` / `ICsvWriter` without the implementation package.

## Interfaces

- `ICsvService` — façade with the full read/write/validate/compare/split/combine surface. Exposes `Writer`, `Reader`, `SetEncoding(Encoding)`, and `SetOptions(CsvOptions)`.
- `ICsvWriter` — write enumerables / `IAsyncEnumerable<T>`, selected `PropertyInfo` columns, custom column-name dictionaries, formatter delegates, row/column dictionaries (`hasFooterRow` peels last row as trailing footer), and `Lyo.DataTable` snapshots (always appends `Footer` when present) to file, `Stream`, `TextWriter`, string, or byte array. Sync overloads work on all targets; async, progress (`IProgress<CsvProgress>`), and append overloads are gated on `!NETSTANDARD2_0`.
- `ICsvReader` — parse files, streams, and byte arrays as typed rows, row/column dictionaries, or `Lyo.DataTable.Models.DataTable` (wrapped in `Result<T>`; `hasFooterRow` peels the last body row into `Footer`). Async-only surface adds streaming (`IAsyncEnumerable<T>` and string-row streams), `CsvParseOptions` (continue-on-error, row filter, max rows), chunked processing, statistics, schema validation, column-mapping parsing, and file comparison.

## Models

- `CsvOptions` — dialect (delimiter, quote, escape, comments), encoding, culture, header/trim/blank-line/column-count flags, and nested `Pooling` (defaults `PoolValues=false` / `PoolFormats=false`). Section name `Csv`.
- `CsvColumnAttribute` — rename or ignore properties for typed mapping.
- `ICsvValueConverter` — cell text ↔ CLR conversion contract.
- `CsvBadDataException` — malformed CSV / conversion failures.
- `CsvParseOptions` — `ContinueOnError`, `OnError`, `RowFilter`, `MaxRows`, and `Pooling` (same CSV defaults as `CsvOptions.CreateDefaultPooling`).
- `CsvSchema` + `CsvColumn` — describe expected columns for `ValidateAsync`.
- `ColumnMapping` — explicit column-name → property mapping for `ParseFileWithMappingAsync` / `ParseStreamWithMappingAsync`.
- `CsvParseError` — error metadata surfaced to `CsvParseOptions.OnError`.
- `CsvParseResult` / `CsvExportResult` — result envelopes used by the higher-level helpers.
- `CsvStatistics` — row/column counts produced by `GetStatisticsAsync`.
- `CsvComparisonResult` + `CsvRowDifference` — output of `CompareFilesAsync`.
- `ValidationResult` — output of `ValidateAsync`.
- `CsvProgress` — progress payload for `ExportToCsv*WithProgressAsync`.

## Multi-targeting

Targets `net10.0;netstandard2.0`. Async, streaming, and option-based overloads are wrapped in `#if !NETSTANDARD2_0` and are only visible on the `net10.0` TFM.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.DataTable.Models` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)