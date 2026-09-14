# Lyo.Reporting.Web

`IReportRenderer` writes HTML and PDF; the package also ships a Blazor `ReportViewer`. Sample business-document templates live in `Lyo.Reporting.Business.Example`.

## Examples

### Wire into DI

```csharp
services.AddWebRenderer(...); // Lyo.Web.WebRenderer
services.AddReportingWebRenderer();
```

## Dependency injection

Composition types come from `Lyo.Reporting.Models`. The package does not reference Postgres or EF. `AddReportingWebRenderer` registers `HtmlPdfReportRenderer` as scoped so it can consume scoped `IWebRendererService` / `HtmlRenderer`.

## Safe markup

- Before `ReportViewer` emits markup, `ReportHtmlSanitizer` runs an allowlist of elements and attributes over `Html`, `Chart`, and `Image` blocks, so composition JSON cannot smuggle script, event handlers, or `javascript:` URLs. Nested section/control `Styles` maps are sanitized the same way as report-level CSS. Unknown `ContentType.Component` FullNames render a sanitized placeholder instead of crashing. The same paint path (`ReportViewPainter` / `ReportBodyItemView`) is what `HtmlPdfReportRenderer` uses. Optional `Layout.RootComponentType` renders a named Blazor type instead of `ReportViewer`. Renderers run `ReportCompositionProcessor` first so `{Key}` placeholders and `VisibleWhen` match generate output. The design canvas interpolates example parameters at paint time without cloning, paints TOC headings and FromParameter tables/charts from the example map, and dims `Collapsed` sections instead of using HTML `hidden`. Cards, blocks, and layout grids default to `break-inside: avoid` (`KeepTogether`); tables split unless the flag is on. A `Grid` paints with `display:grid`. The workbench live editor is `ReportDesignCanvas` in `Lyo.Reporting.Web.Components` — generate output never includes drag handles or selection rings.
- Chart builders therefore ship data rather than code: they emit a `canvas` whose `data-lyo-chart` attribute holds configuration, and `ReportViewer` appends a single script — the Chart.js bundle plus a loader that paints every such canvas. Canvas ids are stable per block instance; the loader destroys and redraws when the config fingerprint changes so Blazor re-renders do not leave a blank chart.
- This package vendors the Chart.js bundle. `ChartScriptUrl` falls back to `Constants.Charts.DefaultScriptUrl` = `_content/Lyo.Reporting.Web/scripts/chart.umd.min.js` (the static web asset), so drawing charts needs no outbound network; override with `Constants.Charts.CdnScriptUrl` (or any host-served URL), or leave it empty to skip charts. **Breaking change** — the public CDN used to be the default.
- `InlineChartScript` is set by `HtmlPdfReportRenderer`, which inlines the bundle: the PDF converter receives a standalone file, and a relative asset URL would resolve against nothing.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Web.WebRenderer` (direct, lyo)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)