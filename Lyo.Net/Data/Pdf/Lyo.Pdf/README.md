# Lyo.Pdf

PdfPig-backed reading and PDFsharp-backed editing for [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md).
`PdfService` is the entry point; it returns disposable `IPdfReader` instances for
read/extract workflows and `IPdfWriter` instances for structural edits and merges.

Multi-targets `netstandard2.0;net10.0`.

## Examples

### Open a PDF

```csharp
await using var pdf = await pdfService.OpenFromFileAsync("invoice.pdf", ct);
var info = pdf.GetInfo();
(var width, var height) = pdf.GetPageSizePoints(1);
```

### Edit and merge PDFs

```csharp
using var writer = pdfService.OpenForEdit(bytes);
writer.ImportPagesFrom(otherReader);
writer.InsertBlankPage(writer.PageCount);
writer.ReorderPages([2, 0, 1]);
await writer.SaveAsync("out.pdf", ct);
```

### Register with DI

```csharp
services.AddPdfService();
services.AddPdfService(options => options.DefaultYTolerance = 4.0);
services.AddPdfServiceFromConfiguration(configuration); // section "PdfServiceOptions"
services.AddPdfService(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("pdf"));
services.AddPdfService(httpClientName: "pdf");
services.AddPdfServiceKeyed("primary", configure: o => o.EnableMetrics = true);
```

## Loading

`IPdfService` exposes paired sync and async loaders for **file**, **bytes**, and
**stream** (`OpenFromFile`, `OpenFromFileAsync`, `OpenFromBytes`,
`OpenFromBytesAsync`, `OpenFromStream`, `OpenFromStreamAsync`) plus matching batch
overloads (`OpenFromFiles`, `OpenFromFilesAsync`, `OpenFromBytesBatch`,
`OpenFromBytesBatchAsync`, `OpenFromStreams`, `OpenFromStreamsAsync`).

URL loaders are **async only** — `OpenFromUrlAsync` and `OpenFromUrlsAsync` — so the
service never blocks on synchronous HTTP. Register an `HttpClient` via DI to share
connection pooling and timeouts (see below); without one, a new `HttpClient` is
created per call and disposed.

Each loader returns an `IPdfReader` (PdfPig + immutable byte snapshot).
**The caller owns the instance** and must dispose it (`using` /
`await using`); `PdfService` itself does not implement `IDisposable`.

`PdfServiceOptions.MaxPdfSizeBytes` enforces a per-PDF byte cap (default
`SuggestedMaxPdfSizeBytes = 25 MiB`).

## Extraction (`pdf.Text`)

- **Words and lines** — `GetWords` / `GetLines` (+ async) with optional page and line tolerance.
- **Anchored slices** — `GetWordsBetween` / `GetLinesBetween` (+ async).
- **Regions** — `GetLinesInBoundingBox`, `GetColumnarTextInBoundingBox`, and a word-list overload `GetColumnarText(words, columnCount, yTolerance?)`.
- **Key/value pairs** — `ExtractKeyValuePairs` with `int? page`, `PdfWord[]`, `PdfSection`, and section-name overloads (`startSection` + ordered `sectionsInOrder` + optional `defaultEndSection`, page range, and `yTolerance`). Section-name overloads return `null` when the requested section is not found.
- **Tables** — `ExtractTable(headers, …)` returns `IReadOnlyList<IReadOnlyDictionary<string, string?>>`. `ExtractDataTable(headers, …)` returns a `Lyo.DataTable`. Section-name overloads return `null` when the section is missing. `ParseBytesAsDataTable` re-opens a byte buffer for one-shot extraction.
- **Inference helpers** — `InferKeyValuePairsFromFormatting(words, yTolerance, columnCount, inferFlags, keyValueDelimiters?)` and `InferTableHeadersFromFormatting(words, …)` use `PdfInferFormattingFlags` (Bold, Semicolon, Underline) and optional punctuation terminators.
- **Sections** — `GetSection`, `GetWordsBetweenSections`, `GetLinesBetweenSections` (+ async).

## Editing and merging (`IPdfWriter`)

- `MergePdfs` / `MergePdfsAsync` — return merged bytes.
- `MergePdfsToFile` / `MergePdfsToFileAsync` — write merged bytes to a path.
- `MergePdfsToStream` / `MergePdfsToStreamAsync` — write merged bytes to a stream.
- `MergePdfFiles` / `MergePdfBytes` (+ async) — file-path or byte-array variants with a designated initial PDF.

## Dependency injection

`PdfServiceOptions` is registered as a singleton; `PdfService` and `IPdfService` share the same scoped instance. When an `IHttpClientFactory` is available the default registrations create a named client (`nameof(PdfService)`) for URL loads; otherwise `HttpClient` is created per-call. `IMetrics` is optional — when present and `EnableMetrics = true`, PDF operations emit metrics through the registered implementation.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Pdf.Models` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (direct, microsoft)
- `PDFsharp` `6.2.4` — (direct, third-party)
- `PdfPig` `0.1.15` — (direct, third-party)
- `Lyo.DataTable.Models` — (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)