# Lyo.Comic.Web.Components

Reusable Blazor components for browsing, previewing, and reading comic series — search panel, result grids/lists, browse cards, and a MangaFire-style tap-to-navigate reader.
The library is data-source agnostic: every component takes the data it needs as parameters and asks the host to provide async callbacks for fetching pages, resolving cover
URLs, and emitting navigation events. There is no DI registration step.

## Examples

### How a host wires this up

```razor
@inject IComicStore Store
@inject ComicFileService Files

<ComicSearch
    SearchFunc="(query, ct) => Store.SearchSeriesAsync(query, ct)"
    AvailableTags="@_tags"
    ResolveCoverUrlFunc="@(series => ComicCoverUrls.Resolve(series.CoverImageRef))"
    OnSeriesBrowse="@OpenBrowsePageAsync"
    OnSeriesRead="@OpenReaderAsync" />

@if (_reading is not null) {
    <ComicViewer
        Series="@_reading"
        Chapters="@_chapters"
        CurrentChapter="@_currentChapter"
        CurrentPage="@_currentPage"
        LoadPageImageAsync="@Files.LoadPageImageAsync"
        CoverImageUrl="@ComicCoverUrls.Resolve(_reading.CoverImageRef)"
        OnChapterChanged="@OnChapterChanged"
        OnPageChanged="@OnPageChanged"
        OnClose="@CloseReader" />
}
```

## Component catalog

All types live in the `Lyo.Comic.Web.Components` namespace.

## Component catalog — `ComicSearch` *(`ComicSearch.razor` + `.razor.cs` + `.razor.css`)*

Top-level search panel: title input, type / status / language selects, a `LyoCheckSelect<string>` for tags, a view-mode toggle, results pagination, and a "Load more" button.

| Parameter (`[EditorRequired]` in **bold**) | Purpose |
| --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| **`Func<ComicSeriesQuery, CancellationToken, Task<IReadOnlyList<ComicSeries>>>? SearchFunc`** | Host-supplied search callback. Receives the query built from the current filter state. |
| `IReadOnlyList<string> AvailableTags` | All tag strings available for filtering. Bound to the tag check-select. |
| `Func<ComicSeries, string?>? ResolveCoverUrlFunc` | Resolves a display-ready cover URL from each series' `CoverImageRef`. Return `null` to fall back to placeholder. |
| `EventCallback<ComicSeries> OnSeriesRead` | Raised when the user jumps into the reader (cover, Read button, list read icon). |
| `EventCallback<ComicSeries> OnSeriesBrowse` | Raised when the user opens the series browse page (card body, Details, or list row). |
| `int PageSize` *(default `20`)* | Page size used both for initial fetch and "Load more" follow-ups. |

Internally it builds `ComicSeriesQuery` from `TitleContains`, `ComicType`, `ComicStatus`, `Language`, `Tags`, `Skip`, and `Limit`, calls `SearchFunc`, tracks `_hasMore`, and
renders results through `ComicBrowseResults` + `ComicCard`.

## Component catalog — `ComicBrowseResults` *(`ComicBrowseResults.razor` + `.razor.cs` + `.razor.css`)*

Layout shell for browse cards. Wraps `ChildContent` in a `<div>` whose CSS class is derived from `ViewMode`:

| `ViewMode` | CSS layout class |
| --------------------------------- | ----------------------------------------------------------------- |
| `ComicBrowseViewMode.GridLarge` | `comic-browse-results__grid comic-browse-results__grid--large` |
| `ComicBrowseViewMode.GridSmall` | `comic-browse-results__grid comic-browse-results__grid--small` |
| `ComicBrowseViewMode.ListNoImage` | `comic-browse-results__list comic-browse-results__list--no-image` |
| `ComicBrowseViewMode.ListLine` | `comic-browse-results__list comic-browse-results__list--line` |

Parameters: `RenderFragment? ChildContent`, `ComicBrowseViewMode ViewMode`, optional extra `string? Class`.

## Component catalog — `ComicCard` *(`ComicCard.razor` + `.razor.cs`)*

Thin adapter that maps a `ComicSeries` onto `ComicBrowseCard`. Computes `TypeBadge` from `Series.ComicType` (omitting `ComicType.Unknown`), wires cover / body / read / details taps to the corresponding events. Parameters: **`ComicSeries Series`** (`[EditorRequired]`), `string? CoverImageUrl`, `ComicBrowseViewMode ViewMode`, `EventCallback<ComicSeries> OnRead`, `EventCallback<ComicSeries> OnOpenSeries`.

## Component catalog — `ComicBrowseCard` *(`ComicBrowseCard.razor` + `.razor.cs` + `.razor.css`)*

Presentational card used for both grid and list layouts. Renders the cover image, status badge, type/language overlays, title, subtitle, description (auto-truncated to 200
characters), up to three visible tag chips with a "+N" overflow indicator, and optional Read/Details actions.

| Parameter | Purpose |
| --------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `string Title` | Display title. |
| `string? Subtitle` | Secondary line (author, publisher, …). |
| `string? Description` | Description; truncated to 200 chars for display. |
| `string? CoverImageUrl` | Resolved cover URL. |
| `string? TypeBadge` / `string? LanguageLine` | Overlays drawn on top of the cover. |
| `int? PageCount` | Optional page-count chip. |
| `ComicStatus? Status` | Drives the status badge CSS modifier (`--ongoing`, `--completed`, `--hiatus`, `--cancelled`, `--unknown`). |
| `string? Demographic`, `int? PublishedYear` | Optional metadata chips. |
| `IReadOnlyList<string> Tags` | Tag chips (max 3 visible, overflow shown as "+N"). |
| `ComicBrowseViewMode ViewMode` | Selects card layout (default `GridSmall`). |
| `bool ShowReadActions` | When true, the Read / Details action row is rendered. |
| `EventCallback OnCoverClick`, `OnPrimaryClick`, `OnRead`, `OnDetails` | Tap targets. |

## Component catalog — `BrowseListPanel` *(`BrowseListPanel.razor` + `.razor.cs` + `.razor.css`)*

Row variant rendered by `ComicBrowseCard` when the `ViewMode` is `ListNoImage` / `ListLine`. Parameters: `ComicBrowseViewMode Mode`, `string Title`, `string? Subtitle`, `string? DisplayDescription`, `string? TypeBadge`, `string? LanguageLine`, `int? PageCount`, `bool ShowReadActions`, `EventCallback PrimaryClicked`, `EventCallback ReadClicked`.

## Component catalog — `ComicViewer` *(`ComicViewer.razor` + `.razor.cs` + `.razor.css`)*

MangaFire-style reader: page area, page counter, slider, chapter selector, previous / next chapter buttons, and a close button. Coordinates with
`./_content/Lyo.Comic.Web.Components/scripts/comicViewer.js` for rapid keyboard / tap navigation, prefetches neighbor pages (radius 5 — widened to 8 after a "jump"), and falls
back to the cover image while the first page resolves.

| Parameter | Purpose |
| ------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| **`ComicSeries Series`** *(`[EditorRequired]`)* | The series being read. |
| `IReadOnlyList<ComicChapter> Chapters` | Ordered chapters available for navigation (already language-filtered by the host). |
| `ComicChapter? CurrentChapter` | Currently rendered chapter; `null` until the host picks one. |
| `int CurrentPage` *(default `1`)* | 1-based page within the active chapter. |
| `int TotalPages` | Total pages for the chapter; falls back to `ComicChapter.PageCount` when `0`. |
| **`Func<ComicSeries, ComicChapter, int, CancellationToken, Task<string?>>? LoadPageImageAsync`** | Host-supplied page loader. Return a URL or Base-64 data URI, or `null` if unavailable. |
| `string? CoverImageUrl` | Cover URL, warmed in parallel and shown while the first page is resolving. |
| `EventCallback<ComicChapter> OnChapterChanged` / `OnPreviousChapterChanged` | Raised when navigation crosses chapter boundaries (the "previous" variant falls back to `OnChapterChanged` if not wired). |
| `EventCallback<int> OnPageChanged` | Raised when navigation changes the current page. |
| `EventCallback OnClose` | Raised when the close button is tapped. |

Public method: `Task ReloadPageAsync(int? forPage = null)` — re-fetches the displayed image; pass a value when calling immediately after updating local state because Blazor
re-renders asynchronously. The component implements `IAsyncDisposable` so the JS module / `DotNetObjectReference` are torn down cleanly.

## Component catalog — Other public types

- `ComicBrowseViewMode` *(enum)* — `GridLarge`, `GridSmall`, `ListNoImage`, `ListLine`.
- `ComicCoverUrls.Resolve(string? coverImageRef)` — utility that maps a `CoverImageRef` to a display URL: when the ref parses as a `Guid`, returns `/comic-files/{id:D}`; otherwise returns the ref as-is (so HTTPS URLs flow through unchanged).

## Static assets

Shipped under `wwwroot/scripts/comicViewer.js` and resolved at runtime as `./_content/Lyo.Comic.Web.Components/scripts/comicViewer.js`. `ComicViewer` imports the module on first render with a cache-busted retry if the static-web-asset fingerprint hint is stale. `_Imports.razor` brings in `MudBlazor`, `Lyo.Web.Components` (`CheckSelect`, `Models`), `Lyo.Common`, and `Lyo.Comic` / `Lyo.Comic.Enums` so consumer pages don't have to.

## Project shape

- **Target framework:** `net10.0`
- **SDK:** `Microsoft.NET.Sdk.Razor`
- **NuGet:** `MudBlazor [9.3,)` plus `Microsoft.AspNetCore.App` framework reference.
- **Project references:** [`Lyo.Comic`](../Lyo.Comic/README.md), [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md), [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Comic` — (direct, lyo)
- `Lyo.Web.Components` — (direct, lyo)
- `MudBlazor` `9.3` — (direct, third-party)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
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
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)