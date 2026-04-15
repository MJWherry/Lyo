# Lyo.Pdf

PDF loading and text extraction for .NET. The main entry point is **`IPdfService`** (implemented by **`PdfService`**): load documents into memory, obtain a stable **`pdfId`**, then query words, lines, regions, key–value pairs, and tables. Merging and round-tripping bytes use **PDFsharp**; parsing uses **PdfPig** (UglyToad.PdfPig).

Types such as `PdfWord`, `PdfTextLine`, `PdfBoundingBox`, and `ColumnHeader` live in **`Lyo.Pdf.Models`** (the `IPdfService` contract is defined there).

## `IPdfService` / `PdfService`

### Loading and lifetime

- Load from **file**, **URL** (optional `HttpClient` from `IHttpClientFactory`), **bytes**, or **stream**.
- Each successful load returns a **`LoadedPdfLease`**: dispose it (or call `UnloadPdf`) when finished so pages and bytes are released. The service enforces **per-PDF** and **total** size limits via **`PdfServiceOptions`**.
- Batch helpers load multiple PDFs and return one lease per document; on failure, already-loaded IDs in that batch are unloaded.

### Extraction

- **Words and lines** — `GetWords` / `GetLines`, optionally per page, with configurable vertical tolerance for line grouping.
- **Between anchors** — `GetWordsBetween` / `GetLinesBetween` using start/end text on a page.
- **Bounding regions** — `GetLinesInBoundingBox` for a `PdfBoundingBox` (page + box in PDF points). Includes page text and relevant form/annotation values where they intersect the region.
- **Columnar text in a region** — `GetColumnarTextInBoundingBox` splits a box into one or more columns (heuristics for two columns; equal bands for more).
- **Key–value** — `ExtractKeyValuePairs` for known labels; set `keyValueColumnCount` &gt; 1 to split the region into that many vertical bands when the same keys appear side by side.
- **Tables** — `ExtractTable` / `ExtractDataTable` using `ColumnHeader[]` to find a header row and map cells; can produce **`Lyo.DataTable.Models.DataTable`**.
- **Sections** — helpers like `GetSection` / `GetLinesBetweenSections` for document sections defined by ordered header names.
- **Merge and export** — merge loaded PDFs by id; write bytes to file or stream; `GetPdfBytes` for raw content.

### Dependency injection

```csharp
using Lyo.Pdf;
using Lyo.Pdf.Models;

// Startup
services.AddPdfService();
// or: services.AddPdfService(o => { ... });
// or: services.AddPdfServiceFromConfiguration(configuration);

// Inject
public class MyExtractor(IPdfService pdf)
{
    public async Task RunAsync(string path)
    {
        await using var lease = await pdf.LoadPdfFromFileAsync(path);
        var lines = pdf.GetLines(lease.PdfId, page: 1);
        // ...
    }
}
```

`AddPdfService` registers **`PdfService`** as scoped and **`IPdfService`** to the same instance. For URL loading, register an `HttpClient` (e.g. `services.AddHttpClient(nameof(PdfService), ...)`).

## Pairing with the annotator

To define regions by drawing boxes in the browser (IDs → `PdfBoundingBox`), use **[Lyo.Pdf.Annotator](../Lyo.Pdf.Annotator/README.md)** and feed those boxes into `GetLinesInBoundingBox` — see the annotator README for a full example.

## Dependencies

| Package / project | Role |
| --- | --- |
| `UglyToad.PdfPig` | Open PDFs, text and layout |
| `PDFsharp` | Merge and byte-level output |
| `Microsoft.Extensions.Http` | Optional URL fetch via `IHttpClientFactory` |
| `Lyo.Pdf.Models` | `IPdfService`, DTOs |
| `Lyo.Common`, `Lyo.Exceptions`, `Lyo.Metrics` | Shared helpers, metrics |

**Target frameworks:** `netstandard2.0`, `net10.0`.
