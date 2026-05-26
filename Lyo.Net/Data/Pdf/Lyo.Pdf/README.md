# Lyo.Pdf

PdfPig-backed reading and PDFsharp-backed editing for [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md).
`PdfService` is the entry point; it returns disposable `IPdfReader` instances for
read/extract workflows and `IPdfWriter` instances for structural edits and merges.

Multi-targets `netstandard2.0;net10.0`.

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

```csharp
await using var pdf = await pdfService.OpenFromFileAsync("invoice.pdf", ct);
var info = pdf.GetInfo();
(var width, var height) = pdf.GetPageSizePoints(1);
```

`PdfServiceOptions.MaxPdfSizeBytes` enforces a per-PDF byte cap (default
`SuggestedMaxPdfSizeBytes = 25 MiB`).

## Extraction (`pdf.Text`)

`IPdfReader.Text` returns an `ITextExtractor` that combines text/layout/table
extraction (`IPdfDocumentText`) with multi-page section navigation
(`IPdfDocumentSections`). Defaults follow the `PdfServiceOptions` bound at load
time.

- **Words and lines** — `GetWords` / `GetLines` (+ async) with optional page and
  line tolerance.
- **Anchored slices** — `GetWordsBetween` / `GetLinesBetween` (+ async).
- **Regions** — `GetLinesInBoundingBox`, `GetColumnarTextInBoundingBox`, and a
  word-list overload `GetColumnarText(words, columnCount, yTolerance?)`.
- **Key/value pairs** — `ExtractKeyValuePairs` with `int? page`, `PdfWord[]`,
  `PdfSection`, and section-name overloads (`startSection` + ordered
  `sectionsInOrder` + optional `defaultEndSection`, page range, and `yTolerance`).
  Section-name overloads return `null` when the requested section is not found.
- **Tables** — `ExtractTable(headers, …)` returns
  `IReadOnlyList<IReadOnlyDictionary<string, string?>>`. `ExtractDataTable(headers,
  …)` returns a `Lyo.DataTable`. Section-name overloads return `null` when the
  section is missing. `ParseBytesAsDataTable` re-opens a byte buffer for one-shot
  extraction.
- **Inference helpers** — `InferKeyValuePairsFromFormatting(words, yTolerance,
  columnCount, inferFlags, keyValueDelimiters?)` and
  `InferTableHeadersFromFormatting(words, …)` use `PdfInferFormattingFlags` (Bold,
  Semicolon, Underline) and optional punctuation terminators.
- **Sections** — `GetSection`, `GetWordsBetweenSections`,
  `GetLinesBetweenSections` (+ async).

`PdfWord` overloads operate on supplied `IReadOnlyList<PdfWord>` slices and skip the
PdfPig page scan; option-derived defaults (`PdfServiceOptions`) still apply.

## Editing and merging (`IPdfWriter`)

`CreateEmpty()`, `OpenForEdit(bytes/file/stream)`, and `OpenForEditAsync` return an
`IPdfWriter`:

```csharp
using var writer = pdfService.OpenForEdit(bytes);
writer.ImportPagesFrom(otherReader);
writer.InsertBlankPage(writer.PageCount);
writer.ReorderPages([2, 0, 1]);
await writer.SaveAsync("out.pdf", ct);
```

Merge helpers operate directly on `byte[]` buffers (typically
`reader.SourceBytes.ToArray()`):

- `MergePdfs` / `MergePdfsAsync` — return merged bytes.
- `MergePdfsToFile` / `MergePdfsToFileAsync` — write merged bytes to a path.
- `MergePdfsToStream` / `MergePdfsToStreamAsync` — write merged bytes to a stream.
- `MergePdfFiles` / `MergePdfBytes` (+ async) — file-path or byte-array variants
  with a designated initial PDF.

## Dependency injection

```csharp
services.AddPdfService();
services.AddPdfService(options => options.DefaultYTolerance = 4.0);
services.AddPdfServiceFromConfiguration(configuration); // section "PdfServiceOptions"
services.AddPdfService(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("pdf"));
services.AddPdfService(httpClientName: "pdf");
services.AddPdfServiceKeyed("primary", configure: o => o.EnableMetrics = true);
```

`PdfServiceOptions` is registered as a singleton; `PdfService` and `IPdfService`
share the same scoped instance. When an `IHttpClientFactory` is available the
default registrations create a named client (`nameof(PdfService)`) for URL loads;
otherwise `HttpClient` is created per-call.

`IMetrics` is optional — when present and `EnableMetrics = true`, PDF operations
emit metrics through the registered implementation.

## Dependencies

| Package / project                                  | Role                                          |
|----------------------------------------------------|-----------------------------------------------|
| `UglyToad.PdfPig`                                  | Reading PDFs (text, layout)                   |
| `PDFsharp`                                         | Structural edits / merges (`IPdfWriter`)      |
| `Microsoft.Extensions.Configuration.Binder`        | `AddPdfServiceFromConfiguration`              |
| `Microsoft.Extensions.Http`                        | URL loads via `IHttpClientFactory`            |
| `Lyo.Pdf.Models`                                   | Contracts and DTOs                            |
| `Lyo.Common`, `Lyo.Exceptions`, `Lyo.Metrics`, `Lyo.Result` | Shared helpers, validation, metrics, results |

## Related projects

- [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md), [`Lyo.Pdf.Ocr`](../Lyo.Pdf.Ocr/README.md),
  [`Lyo.Pdf.Rendering`](../Lyo.Pdf.Rendering/README.md),
  [`Lyo.Pdf.Web.Components`](../Lyo.Pdf.Web.Components/README.md).
