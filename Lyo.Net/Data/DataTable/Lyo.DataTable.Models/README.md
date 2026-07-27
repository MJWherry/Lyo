# Lyo.DataTable.Models

Mutable in-memory data table with sparse columns, headers, footer, per-cell formatting,
fluent builders, and an HTML renderer. Used as the canonical tabular exchange format for
[`Lyo.Csv`](../../Csv/Lyo.Csv/README.md), [`Lyo.Xlsx`](../../Xlsx/Lyo.Xlsx/README.md), and
[`Lyo.Pdf`](../../Pdf/Lyo.Pdf/README.md), and for ad-hoc programmatic table construction.

This project contains all of the runtime types. The sibling
[`Lyo.DataTable`](../Lyo.DataTable/README.md) project is an empty package placeholder.

## Core types

- `DataTable` — sparse table with `Headers` (column → cell), `Rows`
  (`IReadOnlyList<DataTableRow>`), and `Footer`. Indexer `this[int row, int col]` uses
  `row == -1` for header and `row == -2` for footer; out-of-range reads return
  `DataTableCell.Empty`. Writes auto-grow the row list. Helpers: `SetHeader`,
  `SetFooter`, `SetCell`, `AddRow`, `MaxColumn`.
- `DataTableRow` — sparse `Cells` dictionary keyed by column index, with `SetCell` and
  an indexer.
- `IDataTableCell` — common interface exposing `DisplayValue` plus optional formatting
  (`FontSize`, `FontName`, `FontBold`, `FontItalic`, `FontUnderline`,
  `FontStrikethrough`, `FontColor`, `BackgroundColor`, `HorizontalAlignment`,
  `VerticalAlignment`, `NumberFormat`, `TextRotation`, `WrapText`, `BorderTop`,
  `BorderBottom`, `BorderLeft`, `BorderRight`, `BorderColor`) and cell spanning
  (`ColSpan`, `RowSpan`, default 1; the cell is the top-left anchor of the span).
- `DataTableCell<T>` — sealed record implementing `IDataTableCell`; `DisplayValue` is
  `Value?.ToString() ?? ""`. `DataTableCell<string>.Empty` is the shared empty cell.
- `DataTableCell` (static) — non-generic helpers: `Empty`, `FromValue(string?)`.
- `DataTableToHtml.ToHtmlDocument(DataTable)` — renders a full HTML document with
  inline styles derived from the per-cell formatting (font, color, background,
  alignment, wrap). Cells with `ColSpan`/`RowSpan` > 1 emit `colspan`/`rowspan`
  attributes and covered cells are skipped.

## Fluent builders

- `DataTableBuilder` — top-level builder. `AddColumn(col, configure)` defines
  conditional formatting; `AddHeader` / `AddHeaders`, `AddFooter` / `AddFooters` /
  `AddSumFooter(params IConvertible[])`, and several `AddRow` overloads (including a
  parameterless one that returns a `DataTableRowBuilder` for chaining via
  `BuildAndAdd`). `Build()` materializes the `DataTable` and computes any sum footers
  configured via `WithSumFooter` / `AddSumFooter`.
- `DataTableRowBuilder` — `SetCell<T>(col, value)` applies any column-level
  `FormatWhen` rules; `AddCell(col, …)` accepts strings, `IDataTableCell`, or a
  `DataTableCellBuilder`; `AddCells(params …)` fills consecutive columns from 0.
- `DataTableColumnBuilder` — `WithSumFooter()` enables the auto-summed footer for that
  column; `FormatWhen<T>(when, apply)` adds a conditional formatting rule evaluated
  when a `SetCell<T>` value matches the predicate.
- `DataTableCellBuilder` — fluent setter for every formatting property, e.g.
  `WithBold`, `WithFontColor`, `WithBackgroundColor`, `WithBorders`,
  `WithNumberFormat`, `WithTextRotation`, `WithColSpan` / `WithRowSpan`, plus
  `Build<T>()` / non-generic `Build()`.

## Sum footers

`AddSumFooter` (or `WithSumFooter` on a column definition) flags a column for
auto-summation. At `Build()` time the builder reads `DisplayValue` from each row's cell,
trims common currency prefixes (`$ £ € ¥`), removes thousands separators, and parses
with `InvariantCulture`; unparseable cells contribute `0`.

## Targeting

`netstandard2.0;net10.0`. No NuGet dependencies; references
[`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md) for argument validation.

## Related projects

- [`Lyo.DataTable`](../Lyo.DataTable/README.md) — empty placeholder package.
- [`Lyo.Csv`](../../Csv/Lyo.Csv/README.md), [`Lyo.Xlsx`](../../Xlsx/Lyo.Xlsx/README.md),
  [`Lyo.Pdf`](../../Pdf/Lyo.Pdf/README.md) — consumers that parse to / export from
  `DataTable`.
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
