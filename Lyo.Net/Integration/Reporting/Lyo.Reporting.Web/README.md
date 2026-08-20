# Lyo.Reporting.Web

Blazor `ReportViewer`, business document templates, and an `IReportRenderer` that emits HTML and PDF.

## Examples

### Register with DI

```csharp
services.AddWebRenderer(...); // Lyo.Web.WebRenderer
services.AddReportingWebRenderer();
```

## DI

Depends on `Lyo.Reporting.Models` for composition types. Does not reference Postgres/EF.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Web.WebRenderer` (direct, lyo)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)