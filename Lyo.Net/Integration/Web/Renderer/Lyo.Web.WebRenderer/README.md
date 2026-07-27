# Lyo.Web.WebRenderer

Server-side rendering of Razor components and HTML→PDF conversion. Razor rendering uses `Microsoft.AspNetCore.Components.Web.HtmlRenderer`; PDF conversion is driven by
**PuppeteerSharp** against a locally-installed Chromium/Chrome browser.

## Surface ([`IWebRendererService`](IWebRendererService.cs))

The service exposes three families of operations plus three observability events.

### Render Razor components

| Method (sync + `Async` overloads)                 | Result                                                                          |
|---------------------------------------------------|---------------------------------------------------------------------------------|
| `RenderToHtml<T>(parameterDictionary?)`           | HTML string for component `T : IComponent`.                                     |
| `RenderToHtml<T, TOptions>(options)`              | HTML string passing a strongly-typed options object as the `Options` parameter. |
| `RenderToHtmlBytes<T>(parameterDictionary?)`      | UTF-8 bytes (records size metric).                                              |
| `RenderToHtmlBytes<T, TOptions>(options)`         | Same, with typed options.                                                       |
| `RenderToFile<T>(filePath, parameterDictionary?)` | Writes HTML to disk at `filePath`.                                              |
| `RenderToFile<T, TOptions>(filePath, options)`    | Same, with typed options.                                                       |

### HTML → PDF conversion

| Method (sync + `Async` overloads)                                           | Result                                                                        |
|-----------------------------------------------------------------------------|-------------------------------------------------------------------------------|
| `ConvertHtmlToPdf(string htmlContent)`                                      | PDF bytes from an HTML string.                                                |
| `ConvertHtmlToPdf(byte[] htmlBytes)`                                        | PDF bytes from already-encoded HTML.                                          |
| `ConvertHtmlToPdfFromFile(string htmlFilePath)`                             | PDF bytes from an HTML file on disk.                                          |
| `ConvertHtmlToPdfFile(string htmlContent, string pdfFilePath)`              | Writes PDF to `pdfFilePath`.                                                  |
| `ConvertHtmlToPdfFile(byte[] htmlBytes, string pdfFilePath)`                | Same, from bytes.                                                             |
| `ConvertHtmlFileToPdfFile(string htmlFilePath, string? pdfFilePath = null)` | End-to-end file → file (PDF path defaults to `<htmlFilePath>.pdf` when null). |

> The current interface only exposes Razor render and HTML→PDF; there is no in-process screenshot API. For raw screenshots, use PuppeteerSharp directly through `BrowserExePath`.

### Events

`ComponentRendered`, `ComponentRenderedToBytes`, `ComponentSavedToFile` fire after the corresponding render operations and carry the resulting payload plus parameter and
options snapshots — useful for downstream archival or diffing.

## Options ([`WebRenderOptions`](WebRenderOptions.cs))

Configuration section: `WebRenderOptions`.

| Property         | Default                                                | Description                                                                              |
|------------------|--------------------------------------------------------|------------------------------------------------------------------------------------------|
| `BrowserExePath` | `Utilities.DetectBrowserPath(SupportedBrowser.Chrome)` | Path to the Chromium/Chrome executable used by PuppeteerSharp for HTML→PDF.              |
| `EnableMetrics`  | `false`                                                | When `true` and an `IMetrics` is registered, records timers/counters/gauges (see below). |

When `EnableMetrics` is `false` (default) the service uses `NullMetrics.Instance`, so registering `IMetrics` is optional.

Metrics emitted (see [`Constants.cs`](Constants.cs)) include `webrenderer.render_to_html(.bytes|_to_file).{duration,success,failure,size_bytes}` and
`webrenderer.convert_html_to_pdf.{duration,success,failure,size_bytes,input_size_bytes}`, tagged with `component_type` / `operation`.

## DI registration ([`Extensions.cs`](Extensions.cs))

```csharp
services.AddWebRendererServiceFromConfiguration(builder.Configuration);
```

This binds `WebRenderOptions` from `WebRenderOptions` (default section name) and registers:

- A scoped `Microsoft.AspNetCore.Components.Web.HtmlRenderer` (resolved with the host’s `IServiceProvider` and `ILoggerFactory`).
- A scoped `IWebRendererService` → `WebRendererService`, optionally consuming registered `ILogger<WebRendererService>` and `IMetrics`.

Pass `configSectionName` to the extension if you need a non-default section name. There is currently no inline-options overload — register `WebRenderOptions` yourself before
calling the extension if you need to bypass configuration.

## Related projects

- [`Lyo.Exceptions`](../../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../../Core/Metrics/Lyo.Metrics/README.md)
