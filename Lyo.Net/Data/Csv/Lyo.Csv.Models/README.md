# Lyo.Csv.Models

CSV stack contracts and value types. [`Lyo.Csv`](../Lyo.Csv/README.md) is the implementation, so you can take ICsvService, ICsvReader, and ICsvWriter without that package.

## Contracts

- ICsvService. Reader plus writer, plus validate, compare, split, and combine. Surfaces Writer, Reader, SetEncoding(Encoding), and SetOptions(CsvOptions).
- ICsvWriter. Emit enumerables / IAsyncEnumerable<T>, chosen PropertyInfo columns, custom column-name dictionaries, formatter delegates, row/column dictionaries (hasFooterRow treats the last row as a trailing footer), and Lyo.DataTable snapshots (Footer is always appended when present) to a file, Stream, TextWriter, string, or byte array. Sync overloads exist on every target. Async, progress (IProgress<CsvProgress>), and append overloads sit behind !NETSTANDARD2_0.
- ICsvReader. Turn files, streams, and byte arrays into typed rows, row/column dictionaries, or Lyo.DataTable.Models.DataTable (inside Result<T>; hasFooterRow moves the last body row into Footer). Async adds streaming (IAsyncEnumerable<T> and string-row streams), CsvParseOptions (continue-on-error, row filter, max rows), chunked processing, statistics, schema validation, column-mapping parsing, and file comparison.

## Types

- CsvOptions. Dialect (delimiter, quote, escape, comments), encoding, culture, header/trim/blank-line/column-count flags, plus nested Pooling (defaults PoolValues=false / PoolFormats=false). Section name Csv.
- CsvColumnAttribute. Rename a property or skip it during typed mapping.
- ICsvValueConverter. Contract that turns cell text into a CLR value.
- CsvBadDataException. Thrown for malformed CSV or conversion failures.
- CsvParseOptions. ContinueOnError, OnError, RowFilter, MaxRows, plus Pooling (CSV defaults match CsvOptions.CreateDefaultPooling).
- CsvSchema + CsvColumn. Columns ValidateAsync expects.
- ColumnMapping. Name-to-property map for ParseFileWithMappingAsync / ParseStreamWithMappingAsync.
- CsvParseError. Error metadata handed to CsvParseOptions.OnError.
- CsvParseResult / CsvExportResult. Envelopes the higher-level helpers return.
- CsvStatistics. Row and column counts GetStatisticsAsync reports.
- CsvComparisonResult + CsvRowDifference. What CompareFilesAsync returns.
- ValidationResult. What ValidateAsync returns.
- CsvProgress. Payload ExportToCsv*WithProgressAsync reports.

## TFMs

Builds for `net10.0;netstandard2.0`. Async, streaming, and option-based overloads sit in `#if !NETSTANDARD2_0`, so only the `net10.0` TFM sees them.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)