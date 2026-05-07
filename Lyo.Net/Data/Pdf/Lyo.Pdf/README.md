# Lyo.Pdf

PDF loading, text extraction, and PDFsharp-backed editing for .NET.

- **`IPdfService`** (**`PdfService`**) is the façade for **`Open`** (**`IPdfReadLoader`**), **`CreateEmpty`** / **`OpenForEdit`**, and all **merge** helpers.
- **`IPdfReadDocument`** exposes **`Text`** (**`IPdfDocumentText`**) and **`Sections`** (**`IPdfDocumentSections`**) — no parallel service surface that repeats the document as the
  first argument.
- Layout types (`PdfWord`, `PdfTextLine`, `PdfBoundingBox`, `ColumnHeader`) and contracts live in **`Lyo.Pdf.Models`**.

## Loading and lifetime

- Open from **file**, **bytes**, or **stream** synchronously (`Open.OpenFrom…`) or asynchronously (`Open.OpenFrom…Async`).
- Opens from **URL** are **async only** (`OpenFromUrlAsync` / `OpenFromUrlsAsync`): the loader never blocks with sync-over-async.
- Inject an **`HttpClient`** (from `IHttpClientFactory`) into **`PdfService`** / **`PdfReadLoader`** when fetching URLs so connection pooling and timeouts are consistent.
- Each open returns **`IPdfReadDocument`**: disposable, caller-owned (PdfPig + byte snapshot). **`SourceBytes`** is the immutable buffer for merges and **`OpenForEdit`**. *
  *`PdfService` itself does not implement `IDisposable`.**
- Per-PDF size limits use **`PdfServiceOptions.MaxPdfSizeBytes`**.

## Extraction and sections (**`IPdfDocumentText`**, **`IPdfDocumentSections`**)

Use the document’s facets (**`pdf.Text`** / **`pdf.Sections`**) rather than repeating **`IPdfReadDocument`** elsewhere:

- Words and lines, optional page and line tolerance (`pdf.Text.GetWords` / `GetLines`).
- Anchors (`GetWordsBetween` / `GetLinesBetween`).
- Regions (`GetLinesInBoundingBox`, columnar variants).
- Key–value and tables (`ExtractKeyValuePairs`, `ExtractTable`, `ExtractDataTable`, inference helpers and `ParseBytesAsDataTable`).
- Section slicing (`pdf.Sections.GetSection`, `GetLinesBetweenSections`, …).

Word-only overloads on **`pdf.Text`** operate on **`PdfWord`** lists and ignore the PdfPig page content; options still reflect the **`PdfServiceOptions`** wired into that service
instance.

## Editing and merging (**`IPdfService`**)

- **`CreateEmpty`**, **`OpenForEdit`** (PdfSharp): import pages from another **`IPdfReadDocument`**, remove/insert/reorder pages, **`ToBytes`**, **`Save`**, **`CopyTo`** (+ async
  counterparts).
- Merge helpers produce bytes or write to paths/streams from **`byte[]`** buffers (typically **`document.SourceBytes.ToArray()`** from each reader).

## Dependency injection

```csharp
services.AddPdfService();
// Optional: PdfService resolves HttpClient for URL loads if registered
services.AddHttpClient(/* ... */);
```

Scoped registration: **`IPdfService`** and **`PdfService`** share one instance.

## Blazor annotator

**`Lyo.Pdf.Web.Components`** (MudBlazor under **`PdfAnnotator/`**) uses **`await using`** (or **`using`**) **`IPdfReadDocument`** from **`PdfService.Open`**, then **`pdf.Text.…`**
for bounding-box and layout-derived extraction.

## Dependencies

| Package / project                             | Role                        |
|-----------------------------------------------|-----------------------------|
| `UglyToad.PdfPig`                             | Read PDFs, text and layout  |
| `PDFsharp`                                    | Structural edits and merges |
| `Lyo.Pdf.Models`                              | Contracts and DTOs          |
| `Lyo.Common`, `Lyo.Exceptions`, `Lyo.Metrics` | Shared helpers and metrics  |

Target frameworks: **`netstandard2.0`**, **`net10.0`**.
