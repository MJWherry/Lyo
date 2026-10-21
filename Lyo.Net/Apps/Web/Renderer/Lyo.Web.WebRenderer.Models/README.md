# Lyo.Web.WebRenderer.Models

Wire contract for an HTML capture. `WebRenderReq` carries the HTML and the PDF or image settings. `WebRenderRes` describes the file `IWebRenderGenerator` wrote. The Puppeteer
implementation lives in `Lyo.Web.WebRenderer`.

## Features

- **`WebRenderReq`.** HTML, format (`Pdf`, `Png`, `Jpeg`, `WebP`), paper (Letter, Legal, Tabloid, A3, A4, A5, or CSS), landscape, margin, scale, CSS page size, full page, viewport,
  JPEG/WebP quality, omit background, and output path.
- **`WebRenderRes`.** Success, content type, file name, output path, byte length, and error.
- **`IWebRenderGenerator`.** `GenerateAsync` writes the file named by the request. A host can call this without referencing PuppeteerSharp.

## Examples

### Describe a Letter PDF

```csharp
var request = new WebRenderReq
{
    Html = html,
    Format = WebRenderFormat.Pdf,
    Paper = "Letter",
    PreferCssPageSize = false,
    OutputFilePath = path
};

WebRenderRes result = await generator.GenerateAsync(request, cancellationToken);
```

## What stays out of this package

Razor component rendering stays on `IWebRendererService` in `Lyo.Web.WebRenderer` because that API takes `IComponent`. This package does not reference Blazor or PuppeteerSharp.