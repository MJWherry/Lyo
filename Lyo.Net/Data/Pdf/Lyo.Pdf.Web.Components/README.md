# Lyo.Pdf.Web.Components

Blazor / MudBlazor PDF UI: an HTML to PDF workbench, a PDF annotation workbench, and `LyoPdfAnnotator` so a user can draw bounding-box regions on a PDF and emit `PdfBoundingBox` payloads.

Targets `net10.0`. Razor SDK with `FrameworkReference Microsoft.AspNetCore.App` and `MudBlazor` 9.3+.

## Examples

### Add annotator services

```csharp
services.AddPdfAnnotatorService(); // IPdfAnnotatorService -> BrowserPdfAnnotator
services.AddPdfAnnotatorInterop(); // LyoPdfAnnotatorController (scoped)
```

## Components

- `HtmlToPdfWorkbench.razor`. Paste HTML or upload an `.html` / `.htm` / `.txt` file, then turn it into a PDF through `IWebRendererService`.
- `PdfAnnotationWorkbench.razor`. Hosts `LyoPdfAnnotator` and shows a live table of saved annotations and their extracted payloads.
- `PdfAnnotator/LyoPdfAnnotator.razor` (+ `LyoPdfAnnotator.razor.cs`, `LyoPdfAnnotatorResultsView.razor`). Drawing canvas and result list. Fires `AnnotationsChanged` and `AnnotationsSaved`.

## Annotator services

- `IPdfAnnotatorService` (`BrowserPdfAnnotator`). After the user finishes annotating a PDF given as `Stream`, `byte[]`, or file path, returns `IReadOnlyDictionary<string, PdfBoundingBox>` (`AnnotateAsync` / `AnnotateFileAsync`).
- `LyoPdfAnnotatorController`. Scoped controller the Blazor components use.
- `LyoPdfAnnotationResult`. Payload for each saved region: `Key`, `BoundingBoxSummary`, `ExtractionType` (`BoundingBoxText`, `KeyValue`, `Table`), `ExtractedText`, optional `KeyValuePairs` / `TableRows`, `KnownKeys`, `TableHeaders`, `YTolerance`, `KeyValueLayout` (`PdfKeyValueLayout`), `InferFormattingFlags` (`PdfInferFormattingFlags`), `KeyValueInferDelimiters`, `TableKeyColumnLabel`, `ColumnCount`, and `ColumnTexts`.

## DI registration

Pair with [`Lyo.Pdf`](../Lyo.Pdf/README.md)'s `AddPdfService(...)` so the annotator can call `IPdfReader.Text` for bounding-box extraction.

## Browser scripts

Scripts sit under `wwwroot/scripts/`. Point at them from the host page or via the usual Razor class-library `_content/Lyo.Pdf.Web.Components/` path.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Pdf` (direct, lyo)
- `Lyo.Pdf.Models` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.WebRenderer` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `PDFsharp` `6.2.4` (transitive, third-party)
- `PdfPig` `0.1.15` (transitive, third-party)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)