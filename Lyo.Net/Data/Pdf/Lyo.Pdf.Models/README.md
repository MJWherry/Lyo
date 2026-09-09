# Lyo.Pdf.Models

PDF stack contracts and value types. [`Lyo.Pdf`](../Lyo.Pdf/README.md) implements them, so you can take `IPdfService`, `IPdfReader`, `IPdfWriter`, and `ITextExtractor` without a direct PdfPig or PDFsharp reference.

## Service contracts

- `IPdfService`. Open PDFs (`OpenFromFile/Bytes/Stream` plus `…Async` and batch variants), open URLs (`OpenFromUrlAsync` / `OpenFromUrlsAsync`, async only), create empty docs or open for edit (`CreateEmpty`, `OpenForEdit` / `OpenForEditAsync`), and merge (`MergePdfs`, `MergePdfsToFile`, `MergePdfsToStream`, `MergePdfFiles`, `MergePdfBytes`, all sync + async).
- `IPdfReader` (`IDisposable`, `IAsyncDisposable`). Open PDF: `SourceBytes` (immutable buffer for merges / `OpenForEdit`), `Metrics`, `Text` (`ITextExtractor`), `GetInfo()`, and `GetPageSizePoints(pageNumber1Based)`. Not safe across threads.
- `IPdfWriter` (`IDisposable`). PdfSharp editor with `PageCount`, `ImportPagesFrom(IPdfReader)` / `ImportPagesFrom(ReadOnlySpan<byte>)`, `RemovePage`, `InsertBlankPage`, `ReorderPages`, `ToBytes`, `Save` / `SaveAsync`, and `CopyTo` / `CopyToAsync`. Page indices are zero-based. Not thread-safe.
- `ITextExtractor`. `IPdfDocumentText` + `IPdfDocumentSections` composed together, reached as `IPdfReader.Text`.
- `IPdfDocumentText`. Words / lines (`GetWords`, `GetLines`, `GetWordsBetween`, `GetLinesBetween`), bounding-box and columnar reads (`GetLinesInBoundingBox`, `GetColumnarTextInBoundingBox`, `GetColumnarText`), key/value extraction (`ExtractKeyValuePairs` with page, word-list, `PdfSection`, and section-name overloads, plus `InferKeyValuePairsFromFormatting`), table extraction (`ExtractTable` / `ExtractDataTable` with the same overload shapes plus `ParseBytesAsDataTable`), and inference helpers (`InferTableHeadersFromFormatting`). Each method has matching sync and async variants.
- `IPdfDocumentSections`. Slice by section: `GetWordsBetweenSections`, `GetLinesBetweenSections` / `GetLinesBetweenSectionsAsync`, `GetSection` / `GetSectionAsync`. Navigation is anchored by ordered section labels with optional `defaultEndSection`, `startPage`, `endPage`, and `yTolerance`.

## Value types

- `PdfInfo`. `PageCount`, `Title`, `Author`, `Subject`, `Creator`, `Producer`, `FilePath`, `Url`, `CreationDate`, `ModifiedDate`.
- `PdfWord(Text, BoundingBox, Format?)` and `PdfWordFormat(FontSize?, FontName?, FontBold, FontItalic, FontColor?, FontUnderline)`.
- `PdfTextLine(Y, Words, Text)`. Words that sit on the same visual row.
- `PdfBoundingBox(Page, Box)`. 1-based page plus `Lyo.Common.Metadata.Records.BoundingBox2D` expressed in PDF points.
- `PdfColumnarText(Columns)` plus `ToCombinedString(separator)`.
- `PdfSection(Name, StartPage, EndPage, Lines)` plus a computed `Words` property.
- `ColumnHeader(Label, IsKey = false)`. Controls table extraction. `IsKey` columns start a new row. Other columns let unmatched lines append to the previous row.
- `KvColumnResult(ColumnIndex, Values)` plus a `KvColumnResult.Merge(...)` helper.
- `PdfKeyValueLayout`. `Horizontal` (value sits to the right of the key) or `Vertical` (value sits below the key).
- `PdfInferFormattingFlags`. `None`, `Bold`, `Semicolon` (labels terminated by punctuation), `Underline`.

## Options

- `PdfServiceOptions`. `AddPdfService(...)` registers this as a singleton: `DefaultYTolerance` (5.0), `DefaultKeyValueGap` (0.0), `TableHeaderMergeThreshold` (20.0), `TableHeaderMatchThreshold` (0.75), `TableColumnXTolerance` (5.0), `BoundingBoxOverlapThreshold` (0.8), `MaxContinuationYGap` (10.0), `MaxContinuationXDistance` (100.0), `ValueColumnXTolerance` (20.0), `KeyValueStackedMaxFirstGap` (120.0), `MaxPdfSizeBytes` (falls back to `SuggestedMaxPdfSizeBytes = 25 MiB`), `EnableMetrics` (default `false`), and configuration `SectionName = "PdfServiceOptions"`. There is no shared catalog and no total-loaded-bytes cap; the caller owns each `IPdfReader`.

## TFMs

`netstandard2.0;net10.0`. Depends on `Lyo.Common.Core`, `Lyo.Exceptions`, `Lyo.Metrics`, `Lyo.Result`, and `Lyo.DataTable.Models`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)