# Lyo.Web.WebRenderer

Server-side Razor rendering and HTML→PDF conversion. Razor rendering uses `Microsoft.AspNetCore.Components.Web.HtmlRenderer`; PDF conversion is driven by **PuppeteerSharp** against a locally-installed Chromium/Chrome browser.

## Examples

### Register in DI ([`Extensions.cs`](Extensions.cs))

```csharp
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

> Only Razor render and HTML→PDF are on the current interface; there is no in-process screenshot API. For raw screenshots, use PuppeteerSharp directly through `BrowserExePath`.

## Events

`ComponentRenderedToBytes`, `ComponentRendered`, `ComponentSavedToFile` fire after the matching render operations and carry the resulting payload plus parameter and options snapshots, used for archival or diffing.

## Options ([`WebRenderOptions`](WebRenderOptions.cs))

`WebRenderOptions` is the configuration section name.

| Property | Default | What it controls |
| ---------------- | ------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| `BrowserExePath` | `Utilities.DetectBrowserPath(SupportedBrowser.Chrome)` | Chromium/Chrome executable path PuppeteerSharp uses for HTML→PDF. |
| `EnableMetrics` | `false` | When `true` and an `IMetrics` is registered, records timers/counters/gauges (see below). |

The service uses `NullMetrics.Instance` when `EnableMetrics` is `false` (default), so registering `IMetrics` is optional.

Metrics emitted (see [`Constants.cs`](Constants.cs)) include `webrenderer.render_to_html(.bytes|_to_file).{duration,success,failure,size_bytes}` and
`webrenderer.convert_html_to_pdf.{duration,success,failure,size_bytes,input_size_bytes}`, tagged with `component_type` / `operation`.

## Register in DI ([`Extensions.cs`](Extensions.cs))

The `WebRenderOptions` configuration section is bound, then a scoped `Microsoft.AspNetCore.Components.Web.HtmlRenderer` (host `IServiceProvider` and `ILoggerFactory`) and a scoped `IWebRendererService` → `WebRendererService` are registered. Registered `ILogger<WebRendererService>` and `IMetrics` are consumed when present. `configSectionName` selects a non-default section. There is no inline-options overload. To skip configuration binding, register `WebRenderOptions` yourself before the extension runs.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `PuppeteerSharp` `24.0.0` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)