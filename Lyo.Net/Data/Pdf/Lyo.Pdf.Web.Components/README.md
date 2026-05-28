# Lyo.Pdf.Web.Components

Reusable Blazor / MudBlazor components for PDF workflows: an HTML → PDF workbench, a
PDF annotation workbench, and a low-level annotator (`LyoPdfAnnotator`) that lets
end-users draw bounding-box regions on a PDF and emit `PdfBoundingBox` payloads.

Targets `net10.0`. Razor SDK with `FrameworkReference Microsoft.AspNetCore.App` and
`MudBlazor` 9.3+.

## Components

- `HtmlToPdfWorkbench.razor` — paste HTML or upload an `.html` / `.htm` / `.txt`
  file, then convert it to a PDF via `IWebRendererService`.
- `PdfAnnotationWorkbench.razor` — wraps `LyoPdfAnnotator` and renders a live table
  of saved annotations and their extracted payloads.
- `PdfAnnotator/LyoPdfAnnotator.razor` (+ `LyoPdfAnnotator.razor.cs`,
  `LyoPdfAnnotatorResultsView.razor`) — drawing surface and result list. Exposes
  `AnnotationsChanged` and `AnnotationsSaved` callbacks.

## Annotator services

- `IPdfAnnotatorService` (`BrowserPdfAnnotator` implementation) — returns
  `IReadOnlyDictionary<string, PdfBoundingBox>` after the user finishes annotating a
  PDF supplied as `Stream`, `byte[]`, or file path
  (`AnnotateAsync` / `AnnotateFileAsync`).
- `LyoPdfAnnotatorController` — scoped controller used by the Blazor components.
- `LyoPdfAnnotationResult` — payload emitted per saved region: `Key`,
  `BoundingBoxSummary`, `ExtractionType` (`BoundingBoxText`, `KeyValue`, `Table`),
  `ExtractedText`, optional `KeyValuePairs` / `TableRows`, `KnownKeys`,
  `TableHeaders`, `YTolerance`, `KeyValueLayout` (`PdfKeyValueLayout`),
  `InferFormattingFlags` (`PdfInferFormattingFlags`), `KeyValueInferDelimiters`,
  `TableKeyColumnLabel`, `ColumnCount`, and `ColumnTexts`.

## Dependency injection

```csharp
services.AddPdfAnnotatorService();   // IPdfAnnotatorService -> BrowserPdfAnnotator
services.AddPdfAnnotatorInterop();   // LyoPdfAnnotatorController (scoped)
```

Combine with [`Lyo.Pdf`](../Lyo.Pdf/README.md)'s `AddPdfService(...)` so the
annotator can call into `IPdfReader.Text` for bounding-box driven extraction.

## Static assets

Browser scripts live under `wwwroot/scripts/`. Reference them from your host page or
through the standard Razor class-library `_content/Lyo.Pdf.Web.Components/` path.

## Dependencies

| Project / package           | Role                                    |
|-----------------------------|-----------------------------------------|
| `Lyo.Pdf`, `Lyo.Pdf.Models` | Reader / extraction surface and DTOs    |
| `Lyo.Web.Components`        | File upload and shared MudBlazor pieces |
| `Lyo.Web.WebRenderer`       | HTML → PDF rendering for the workbench  |
| `MudBlazor`                 | UI primitives                           |

## Related projects

- [`Lyo.Pdf`](../Lyo.Pdf/README.md), [`Lyo.Pdf.Models`](../Lyo.Pdf.Models/README.md),
  [`Lyo.Pdf.Ocr`](../Lyo.Pdf.Ocr/README.md),
  [`Lyo.Pdf.Rendering`](../Lyo.Pdf.Rendering/README.md).
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md),
  [`Lyo.Web.WebRenderer`](../../../Integration/Web/Renderer/Lyo.Web.WebRenderer/README.md).
