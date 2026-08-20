# Lyo.Pdf.Web.Components

Blazor / MudBlazor components for PDF workflows: an HTML to PDF workbench, a PDF annotation workbench, and `LyoPdfAnnotator` so a user can draw bounding-box regions on a PDF and emit `PdfBoundingBox` payloads.

Targets `net10.0`. Razor SDK with `FrameworkReference Microsoft.AspNetCore.App` and `MudBlazor` 9.3+.

## Examples

### Register with DI

```csharp
services.AddPdfAnnotatorService(); // IPdfAnnotatorService -> BrowserPdfAnnotator
services.AddPdfAnnotatorInterop(); // LyoPdfAnnotatorController (scoped)
```

## Components

- `HtmlToPdfWorkbench.razor`. Paste HTML or upload an `.html` / `.htm` / `.txt` file, then convert it to a PDF via `IWebRendererService`.
- `PdfAnnotationWorkbench.razor`. Wraps `LyoPdfAnnotator` and renders a live table of saved annotations and their extracted payloads.
- `PdfAnnotator/LyoPdfAnnotator.razor` (+ `LyoPdfAnnotator.razor.cs`, `LyoPdfAnnotatorResultsView.razor`). Drawing canvas and result list. Exposes `AnnotationsChanged` and `AnnotationsSaved` callbacks.

## Annotator services

- `IPdfAnnotatorService` (`BrowserPdfAnnotator`). Returns `IReadOnlyDictionary<string, PdfBoundingBox>` after the user finishes annotating a PDF supplied as `Stream`, `byte[]`, or file path (`AnnotateAsync` / `AnnotateFileAsync`).
- `LyoPdfAnnotatorController`. Scoped controller used by the Blazor components.
- `LyoPdfAnnotationResult`. Payload emitted per saved region: `Key`, `BoundingBoxSummary`, `ExtractionType` (`BoundingBoxText`, `KeyValue`, `Table`), `ExtractedText`, optional `KeyValuePairs` / `TableRows`, `KnownKeys`, `TableHeaders`, `YTolerance`, `KeyValueLayout` (`PdfKeyValueLayout`), `InferFormattingFlags` (`PdfInferFormattingFlags`), `KeyValueInferDelimiters`, `TableKeyColumnLabel`, `ColumnCount`, and `ColumnTexts`.

## Dependency injection

Combine with [`Lyo.Pdf`](../Lyo.Pdf/README.md)'s `AddPdfService(...)` so the annotator can call into `IPdfReader.Text` for bounding-box driven extraction.

## Static assets

Browser scripts live under `wwwroot/scripts/`. Reference them from your host page or through the standard Razor class-library `_content/Lyo.Pdf.Web.Components/` path.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Pdf` (direct, lyo)
- `Lyo.Pdf.Models` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.WebRenderer` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `PDFsharp` `6.2.4` (transitive, third-party)
- `PdfPig` `0.1.15` (transitive, third-party)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)