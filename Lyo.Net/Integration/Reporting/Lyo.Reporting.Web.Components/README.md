# Lyo.Reporting.Web.Components

MudBlazor ops UI for Lyo Reporting: browse definitions, run reports, and view/download generations.

This package is a **Razor component library** — no `AddXxx` DI. The host supplies an authenticated `IApiClient` (and optional FileStorage download/view callbacks).

Pair with [`Lyo.Api.Reporting`](../../Api/Lyo.Api.Reporting/README.md) on the API host and [`Lyo.Reporting.Client`](../Lyo.Reporting.Client/README.md) for typed HTTP access.

## Host page

```razor
@using Lyo.Reporting.Web.Components

<ReportManagement BaseRoute="Reporting"
                  DownloadFileAsync="DownloadReportFileAsync"
                  ViewFileUrlAsync="GetReportViewUrlAsync" />

@code {
    // Wire to your FileStorage (or Gateway) download/view endpoints.
    private Task DownloadReportFileAsync(Guid fileId, CancellationToken ct)
        => /* stream via IJsInterop.DownloadFileFromStream */ Task.CompletedTask;

    private Task<string?> GetReportViewUrlAsync(Guid fileId, CancellationToken ct)
        => Task.FromResult<string?>($"/files/{fileId}");
}
```

| Parameter | Notes |
| ------------------- | ----------------------------------------------------------- |
| `BaseRoute` | Reporting route prefix (default `"Reporting"`). |
| `DownloadFileAsync` | Host callback for blob download by `OutputFileId`. |
| `ViewFileUrlAsync` | Host callback returning a browser URL for HTML/PDF preview. |

When download/view callbacks are omitted, those menu actions show a snackbar that the host has not configured them. Generations without `OutputFileId` (persist hook skipped) only
support the Details dialog.

## Components

| Component | Role |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `ReportManagement` | Tabs: Definitions / Generations |
| `ReportDefinitionGrid` | Definition list + Run |
| `ReportDefinitionView` | Definition detail / JSON preview |
| `RunReportDialog` | Parameters + format override → `POST …/Generation/Generate`; Options / AllowedValues render as MudSelect with live sibling binding |
| `ReportGenerationGrid` | Generation list + View/Download |
| `ReportGenerationView` | Generation detail + View/Download actions |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.DataTable.Models` — (direct, lyo)
- `Lyo.Reporting.Client` — (direct, lyo)
- `Lyo.Reporting.Models` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `Lyo.Web.Components.Export` — (direct, lyo)
- `Lyo.Web.Components.Export.Csv` — (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)