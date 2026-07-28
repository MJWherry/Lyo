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

| Parameter           | Notes                                                       |
|---------------------|-------------------------------------------------------------|
| `BaseRoute`         | Reporting route prefix (default `"Reporting"`).             |
| `DownloadFileAsync` | Host callback for blob download by `OutputFileId`.          |
| `ViewFileUrlAsync`  | Host callback returning a browser URL for HTML/PDF preview. |

When download/view callbacks are omitted, those menu actions show a snackbar that the host has not configured them. Generations without `OutputFileId` (persist hook skipped) only
support the Details dialog.

## Components

| Component              | Role                                                        |
|------------------------|-------------------------------------------------------------|
| `ReportManagement`     | Tabs: Definitions / Generations                             |
| `ReportDefinitionGrid` | Definition list + Run                                       |
| `ReportDefinitionView` | Definition detail / JSON preview                            |
| `RunReportDialog`      | Parameters + format override → `POST …/Generation/Generate` |
| `ReportGenerationGrid` | Generation list + View/Download                             |
| `ReportGenerationView` | Generation detail + View/Download actions                   |
