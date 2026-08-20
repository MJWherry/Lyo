# Lyo.Pdf.Models

Interfaces and value types for the Lyo PDF stack. Defines the contracts implemented by [`Lyo.Pdf`](../Lyo.Pdf/README.md) so consumers can depend on `IPdfService`, `IPdfReader`, `IPdfWriter`, and `ITextExtractor` without pulling in PdfPig or PDFsharp directly.

## Service contracts

- `IPdfService`. Loads PDFs (`OpenFromFile/Bytes/Stream` plus `…Async` and batch variants), opens URLs (`OpenFromUrlAsync` / `OpenFromUrlsAsync`, async only), creates empty docs or opens for edit (`CreateEmpty`, `OpenForEdit` / `OpenForEditAsync`), and merges (`MergePdfs`, `MergePdfsToFile`, `MergePdfsToStream`, `MergePdfFiles`, `MergePdfBytes`, all sync + async).
- `IPdfReader` (`IDisposable`, `IAsyncDisposable`). Open PDF: `SourceBytes` (immutable buffer for merges / `OpenForEdit`), `Metrics`, `Text` (`ITextExtractor`), `GetInfo()`, and `GetPageSizePoints(pageNumber1Based)`. Not thread-safe.
- `IPdfWriter` (`IDisposable`). PdfSharp-backed editor with `PageCount`, `ImportPagesFrom(IPdfReader)` / `ImportPagesFrom(ReadOnlySpan<byte>)`, `RemovePage`, `InsertBlankPage`, `ReorderPages`, `ToBytes`, `Save` / `SaveAsync`, and `CopyTo` / `CopyToAsync`. Page indices are zero-based. Not thread-safe.
- `ITextExtractor`. Composition of `IPdfDocumentText` + `IPdfDocumentSections`, reachable as `IPdfReader.Text`.
- `IPdfDocumentText`. Words / lines (`GetWords`, `GetLines`, `GetWordsBetween`, `GetLinesBetween`), bounding-box and columnar reads (`GetLinesInBoundingBox`, `GetColumnarTextInBoundingBox`, `GetColumnarText`), key/value extraction (`ExtractKeyValuePairs` with page, word-list, `PdfSection`, and section-name overloads, plus `InferKeyValuePairsFromFormatting`), table extraction (`ExtractTable` / `ExtractDataTable` with the same overload shapes plus `ParseBytesAsDataTable`), and inference helpers (`InferTableHeadersFromFormatting`). Every method has matching sync and async variants.
- `IPdfDocumentSections`. Section slicing: `GetWordsBetweenSections`, `GetLinesBetweenSections` / `GetLinesBetweenSectionsAsync`, `GetSection` / `GetSectionAsync`. Section navigation is anchored by ordered section labels with optional `defaultEndSection`, `startPage`, `endPage`, and `yTolerance`.

## Value types

- `PdfInfo`. `PageCount`, `Title`, `Author`, `Subject`, `Creator`, `Producer`, `FilePath`, `Url`, `CreationDate`, `ModifiedDate`.
- `PdfWord(Text, BoundingBox, Format?)` and `PdfWordFormat(FontSize?, FontName?, FontBold, FontItalic, FontColor?, FontUnderline)`.
- `PdfTextLine(Y, Words, Text)`. Words on the same visual row.
- `PdfBoundingBox(Page, Box)`. 1-based page plus `Lyo.Common.Records.BoundingBox2D` in PDF points.
- `PdfColumnarText(Columns)` with `ToCombinedString(separator)`.
- `PdfSection(Name, StartPage, EndPage, Lines)` with computed `Words` property.
- `ColumnHeader(Label, IsKey = false)`. Drives table extraction. `IsKey` columns anchor a new row. Others let unmatched lines append to the previous row.
- `KvColumnResult(ColumnIndex, Values)` with `KvColumnResult.Merge(...)` helper.
- `PdfKeyValueLayout`. `Horizontal` (value to the right of the key) or `Vertical` (value below the key).
- `PdfInferFormattingFlags`. `None`, `Bold`, `Semicolon` (punctuation-terminated labels), `Underline`.

## Options

- `PdfServiceOptions`. Registered as a singleton by `AddPdfService(...)`: `DefaultYTolerance` (5.0), `DefaultKeyValueGap` (0.0), `TableHeaderMergeThreshold` (20.0), `TableHeaderMatchThreshold` (0.75), `TableColumnXTolerance` (5.0), `BoundingBoxOverlapThreshold` (0.8), `MaxContinuationYGap` (10.0), `MaxContinuationXDistance` (100.0), `ValueColumnXTolerance` (20.0), `KeyValueStackedMaxFirstGap` (120.0), `MaxPdfSizeBytes` (falls back to `SuggestedMaxPdfSizeBytes = 25 MiB`), `EnableMetrics` (default `false`), and configuration `SectionName = "PdfServiceOptions"`. `MaxTotalLoadedBytes` is `[Obsolete]`. The shared catalog is gone. Each `IPdfReader` is caller-owned.

## Targeting

`netstandard2.0;net10.0`. References `Lyo.Common`, `Lyo.Exceptions`, `Lyo.Metrics`, `Lyo.Result`, and `Lyo.DataTable.Models`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)