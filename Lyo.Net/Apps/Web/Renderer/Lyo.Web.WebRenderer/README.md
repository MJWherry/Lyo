# Lyo.Web.WebRenderer

Server-side Razor rendering uses `Microsoft.AspNetCore.Components.Web.HtmlRenderer`. HTML capture uses one shared Chromium process (PuppeteerSharp) and writes PDF, PNG, JPEG, or WebP into a caller-owned stream. Request and response types live in `Lyo.Web.WebRenderer.Models` so a host can pass `WebRenderReq` without referencing the browser.

## Examples

### Register in DI ([`Extensions.cs`](Extensions.cs))

```csharp
services.AddWebRendererService(options =>
{
    options.BrowserExePath = "/usr/bin/chromium";
    options.EnableMetrics = true;
});

services.AddWebRendererServiceFromConfiguration(builder.Configuration);
```

## `IWebRendererService` methods

Three families of operations sit on the service, plus three observability events.

## Render Razor components to HTML

| Method (sync + `Async` overloads) | What you get |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `RenderToHtml<T>(parameterDictionary?)` | HTML string for component `T : IComponent`. Generic, `Type`, and FullName-string overloads share one code path (`LyoTypeInfo.TryResolveClrType`). |
| `RenderToHtml<T, TOptions>(options)` | HTML string that passes a strongly-typed options object as the `Options` parameter. |
| `RenderToHtmlBytes<T>(parameterDictionary?)` | UTF-8 bytes (records a size metric). |
| `RenderToHtmlBytes<T, TOptions>(options)` | Same, with typed options. |
| `RenderToFile<T>(filePath, parameterDictionary?)` | Writes HTML to disk at `filePath`. |
| `RenderToFile<T, TOptions>(filePath, options)` | Same, with typed options. |

## Turn HTML into PDF

| Method (sync + `Async` overloads) | What you get |
| --------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `ConvertHtmlToPdf(string htmlContent)` | PDF bytes from an HTML string. |
| `ConvertHtmlToPdf(byte[] htmlBytes)` | PDF bytes from already-encoded HTML. |
| `ConvertHtmlToPdfFromFile(string htmlFilePath)` | PDF bytes from an HTML file on disk. |
| `ConvertHtmlToPdfFile(string htmlContent, string pdfFilePath)` | Writes PDF to `pdfFilePath`. |
| `ConvertHtmlToPdfFile(byte[] htmlBytes, string pdfFilePath)` | Same, from bytes. |
| `ConvertHtmlFileToPdfFile(string htmlFilePath, string? pdfFilePath = null)` | End-to-end file → file (PDF path defaults to `<htmlFilePath>.pdf` when null). |

`CopyCaptureToAsync` writes PDF, PNG, JPEG, or WebP into a stream you own. `ConvertHtmlToPdfAsync` is the PDF bytes wrapper. `IWebRenderGenerator.GenerateAsync` writes `WebRenderReq.OutputFilePath` and returns `WebRenderRes`. A CSS `@page` size wins when `PreferCssPageSize` is true; otherwise the named paper is used and A4 is the fallback.

## Events

`ComponentRenderedToBytes`, `ComponentRendered`, `ComponentSavedToFile` fire after the matching render operations and carry the resulting payload plus parameter and options snapshots, used for archival or diffing.

## Options ([`WebRenderOptions`](WebRenderOptions.cs))

`WebRenderOptions` is the configuration section name. `Validate` requires `MaxConcurrentPages` of at least 1, a positive `ContentTimeout`, and an existing file when `BrowserExePath` is set. An unset path is legal so HTML-only hosts still boot.

| Property | Default | What it controls |
| -------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------- |
| `BrowserExePath` | `Utilities.DetectBrowserPath(SupportedBrowser.Chrome)` | Chromium/Chrome executable path PuppeteerSharp uses for HTML→PDF. |
| `EnableMetrics` | `false` | When `true` and an `IMetrics` is registered, records one timer, success counter, failure counter, and size gauge per operation. |
| `MaxConcurrentPages` | `4` | How many captures may use the shared browser at once. |
| `ContentTimeout` | `30s` | How long a page may take to load HTML and produce a capture. Cancellation closes the page. |
| `NoSandbox` | `false` | Adds `--no-sandbox`. Leave false outside containers. `--disable-dev-shm-usage` is always passed. |

Metrics stay off unless `EnableMetrics` is true. Names are `webrenderer.render_to_html`, `webrenderer.render_to_html_bytes`, `webrenderer.render_to_file`, `webrenderer.capture`, and `webrenderer.browser.launch`, each with `.duration`, `.success`, and `.failure`. Capture and render also record size gauges. Tags are `component_type`, `operation`, and `format`.

Logs: Information when the browser launches and shuts down, Debug when a render or capture starts, Warning with the exception on failure. A subscriber that throws is logged and the HTML is still returned.

## Register in DI ([`Extensions.cs`](Extensions.cs))

`AddWebRendererService(Action<WebRenderOptions>)`, `AddWebRendererService(WebRenderOptions)`, and `AddWebRendererServiceFromConfiguration` register options, a singleton `BrowserSession`, a scoped `HtmlRenderer`, and a scoped `WebRendererService` as `IWebRendererService` and `IWebRenderGenerator`. `TryAdd` keeps a host registration. `ILogger<WebRendererService>` is required. `IMetrics` is used only when `EnableMetrics` is true.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Web.WebRenderer.Models` (direct, lyo)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `PuppeteerSharp` `24.0.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)