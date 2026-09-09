# Lyo.Reporting.Web.Components

MudBlazor ops UI for Lyo Reporting: browse definitions, run reports, design compositions with a live preview, and view or download generations.

This package is a Razor component library. No `AddXxx` DI. The host supplies an authenticated `IApiClient` (and optional FileStorage download/view callbacks).

Pair with [`Lyo.Reporting.Api`](../../../Apps/Reporting/Lyo.Reporting.Api/README.md) on the API host and [`Lyo.Reporting.Client`](../../../Apps/Reporting/Lyo.Reporting.Client/README.md) for typed HTTP access.

## Page in the host

```razor
@using Lyo.Reporting.Web.Components

<ReportManagement BaseRoute="Reporting"
                  DownloadFileAsync="DownloadReportFileAsync"
                  ViewFileUrlAsync="GetReportViewUrlAsync" />

@code {
    // Wire to your FileStorage (or Gateway) download/view endpoints.
    private Task DownloadReportFileAsync(Guid fileId, string? fileName, CancellationToken ct)
        => /* stream via IJsInterop.DownloadFileFromStream(stream, fileName ?? $"{fileId}", mime) */ Task.CompletedTask;

    private Task<string?> GetReportViewUrlAsync(Guid fileId, CancellationToken ct)
        => Task.FromResult<string?>($"/files/{fileId}");
}
```

| Parameter | Notes |
| ------------------- | --------------------------------------------------------------------------------------------------- |
| `BaseRoute` | Prefix for reporting routes (default `"Reporting"`). |
| `DownloadFileAsync` | Host callback that downloads a blob by `OutputFileId` and preferred file name (`OriginalFileName`). |
| `ViewFileUrlAsync` | Host callback that returns a browser URL for PDF/HTML preview. |

Omit download/view callbacks and those menu actions snackbar that the host has not configured them. CSV/XLSX generations can still preview tables from `ReportDataJson` without `OutputFileId`. A generation with no `OutputFileId` (persist hook skipped) cannot download.

## Components

| Component | What it does |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ReportManagement` | Tabs: Generations / Definitions. After generate, switches to Generations and opens the generation view (and the report output when viewable). |
| `ReportDefinitionGrid` | List of definitions plus Run / Delete (cascade + storage cleanup) |
| `ReportDefinitionView` | JSON preview / definition detail, with a Design action that opens `/report-design/{id}` when the host wires `Design` |
| `RunReportDialog` | Format override + parameters → `POST …/Generation/Generate`; values via `LyoParameterValueField` (Options / AllowedValues select or typed JSON) |
| `ReportGenerationGrid` | List of generations plus View/Download/Delete (storage cleanup) |
| `ReportGenerationView` | Detail for a generation plus View/Download actions |
| `ReportParameterView` | `LyoParameterEditor` for definition parameters (card or table layout, per `AddLyoParameterEditor`) |
| `ReportDesignWorkbench` | Composition designer: expandable nested tree (canvas hit reopens collapsed parents; selected row has a left accent), accordion inspector, live `ReportDesignCanvas` on the stored composition (example `{Key}` interpolated at paint; TOC/FromParameter fill from examples without cloning; `VisibleWhen` and `Collapsed` items stay and dim). Toolbar: Card, Table, Grid, Block. Table size is numeric Columns/Rows; Query/Sproc tables use `LyoParameterOptionsEditor`. Templates menu: Blank, Controls gallery, Sales summary, Invoice, Operations dashboard. Hover chrome, handle-only drag, grid-child and table-column reorder on the canvas, Chart.js redraw, Badge/Signature/TOC/Timeline/Address/Totals/Checkbox/Notes, dirty confirm, undo/redo, duplicate, Save / Save as definition, generate from stored JSON only |
| `ReportDesignCanvas` | Live editor that paints the same body as `ReportViewer` against the live `_report` graph (not a Bind clone), including `@page` CSS. Hover bounding boxes, a move handle (the only draggable node), delete, and + on containers. Drop slots sit between body items, grid children, and table columns. Generate HTML/PDF still uses `ReportViewer`. |

## `ReportFormatterContext`

`ReportFormatterContext.Build(definition, parameters)` supplies the token list for formatter-typed report parameters, exposing the definition as `Definition` and sibling parameters as a `Parameters` map plus flat `Parameter_{key}` entries. Unlike the job scheduler, reporting has no format pass of its own — nothing here runs a value through `IFormatterService` — so that is deliberately all it offers: whatever consumes the generated value resolves the rest. Host-wide keys come from `AddLyoFormatterValueEditor` instead, with no plumbing needed here.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `Lyo.Reporting.Client` (direct, lyo)
- `Lyo.Reporting.Models` (direct, lyo)
- `Lyo.Reporting.Web` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `Lyo.Web.WebRenderer` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.Components.Web` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `PuppeteerSharp` `24.0.0` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)