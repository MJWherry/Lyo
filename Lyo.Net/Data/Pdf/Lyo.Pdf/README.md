# Lyo.Pdf

Read with PdfPig and edit with PDFsharp for [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md). Start at `PdfService`. It hands back disposable `IPdfReader` instances for read/extract work and `IPdfWriter` instances for structural edits and merges.

Multi-targets `netstandard2.0;net10.0`.

## Examples

### Open a document

```csharp
await using var pdf = await pdfService.OpenFromFileAsync("invoice.pdf", ct);
var info = pdf.GetInfo();
(var width, var height) = pdf.GetPageSizePoints(1);
```

### Edit and merge

```csharp
using var writer = pdfService.OpenForEdit(bytes);
writer.ImportPagesFrom(otherReader);
writer.InsertBlankPage(writer.PageCount);
writer.ReorderPages([2, 0, 1]);
await writer.SaveAsync("out.pdf", ct);
```

### Add PdfService in DI

```csharp
services.AddPdfService();
services.AddPdfService(options => options.DefaultYTolerance = 4.0);
services.AddPdfServiceFromConfiguration(configuration); // section "PdfServiceOptions"
services.AddPdfService(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("pdf"));
services.AddPdfService(httpClientName: "pdf");
services.AddPdfServiceKeyed("primary", configure: o => o.EnableMetrics = true);
```

## How documents are opened

`IPdfService` has paired sync and async loaders for file, bytes, and
stream (`OpenFromFile`, `OpenFromFileAsync`, `OpenFromBytes`,
`OpenFromBytesAsync`, `OpenFromStream`, `OpenFromStreamAsync`) plus matching batch
overloads (`OpenFromFiles`, `OpenFromFilesAsync`, `OpenFromBytesBatch`,
`OpenFromBytesBatchAsync`, `OpenFromStreams`, `OpenFromStreamsAsync`).

URL loaders are async only (`OpenFromUrlAsync` and `OpenFromUrlsAsync`) so the
service never blocks on synchronous HTTP. Register an `HttpClient` via DI to share
connection pooling and timeouts (see below). If you skip that, a new `HttpClient` is
created per call and disposed.

Each loader returns an `IPdfReader` (PdfPig + immutable byte snapshot).
The caller owns the instance and must dispose it (`using` /
`await using`). `PdfService` itself does not implement `IDisposable`.

`PdfServiceOptions.MaxPdfSizeBytes` is the per-PDF byte cap (default
`SuggestedMaxPdfSizeBytes = 25 MiB`).

## Extraction (`pdf.Text`)

- **Words and lines.** `GetWords` / `GetLines` (+ async), with optional page and line tolerance.
- **Anchored slices.** `GetWordsBetween` / `GetLinesBetween` (+ async).
- **Regions.** `GetLinesInBoundingBox`, `GetColumnarTextInBoundingBox`, plus a word-list overload `GetColumnarText(words, columnCount, yTolerance?)`.
- **Key/value pairs.** `ExtractKeyValuePairs` with `int? page`, `PdfWord[]`, `PdfSection`, and section-name overloads (`startSection` + ordered `sectionsInOrder` + optional `defaultEndSection`, page range, and `yTolerance`). Section-name overloads return `null` when that section is missing.
- **Tables.** `ExtractTable(headers, …)` yields `IReadOnlyList<IReadOnlyDictionary<string, string?>>`. `ExtractDataTable(headers, …)` yields a `Lyo.DataTable`. Section-name overloads return `null` when the section is missing. `ParseBytesAsDataTable` re-opens a byte buffer for one-shot extraction.
- **Inference helpers.** `InferKeyValuePairsFromFormatting(words, yTolerance, columnCount, inferFlags, keyValueDelimiters?)` and `InferTableHeadersFromFormatting(words, …)` rely on `PdfInferFormattingFlags` (Bold, Semicolon, Underline) and optional punctuation terminators.
- **Sections.** `GetSection`, `GetWordsBetweenSections`, `GetLinesBetweenSections` (plus async).

## Edit and merge (`IPdfWriter`)

- `MergePdfs` / `MergePdfsAsync`. Merged bytes come back.
- `MergePdfsToFile` / `MergePdfsToFileAsync`. Write the merged bytes to a path.
- `MergePdfsToStream` / `MergePdfsToStreamAsync`. Write the merged bytes to a stream.
- `MergePdfFiles` / `MergePdfBytes` (+ async). File-path or byte-array variants; you designate the initial PDF.

## DI registration

`PdfServiceOptions` is a singleton. `PdfService` and `IPdfService` share one scoped instance. If `IHttpClientFactory` is present, the default registrations create a named client (`nameof(PdfService)`) for URL loads. Otherwise a new `HttpClient` is created per call. `IMetrics` is optional. When it is registered and `EnableMetrics = true`, PDF operations emit metrics through that implementation.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Pdf.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `PDFsharp` `6.2.4` (direct, third-party)
- `PdfPig` `0.1.15` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)