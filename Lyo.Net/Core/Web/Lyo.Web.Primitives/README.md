# Lyo.Web.Primitives

The small end of the Lyo Blazor stack: record cards, chips, the responsive table/card switch, the workbench status shell, and browser interop. It exists so packages under `Core/` can share UI without referencing `Apps/Web/Lyo.Web.Components`, which the monorepo dependency law forbids. Types live in `Lyo.Web.Primitives` (plus `ChipInput`, `CheckSelect`, and `DataGrid` for the pieces that already had those folders). [`Lyo.Web.Components`](../../../Apps/Web/Lyo.Web.Components/README.md) references this package for grids and host chrome.

## Features

- Public types live in `Lyo.Web.Primitives` and its feature sub-namespaces, not `Lyo.Web.Components`.
- No dependency on the query builder, data grids, local storage, or `IApiClient`.
- `LyoResponsiveTable` reads the user's layout choice through `ILyoLayoutPreferences`, resolved optionally so hosts without an implementation still work.

## Examples

### Record cards and status chips

```razor
<LyoChip Label="Succeeded" Color="Color.Success" Icon="@Icons.Material.Filled.Check"/>

<LyoResponsiveTable>
    <TableContent>
        <MudSimpleTable>...</MudSimpleTable>
    </TableContent>
    <CardContent>
        @foreach (var row in rows) {
            <LyoRecordCard Title="@row.Name" Subtitle="@row.Kind">
                <LyoRecordField Label="Created" Value="@row.CreatedAt.ToString()"/>
                <LyoRecordField Label="Notes" Value="@row.Notes" HideWhenEmpty="true"/>
            </LyoRecordCard>
        }
    </CardContent>
</LyoResponsiveTable>
```

### Status banner for a workbench

```razor
@inherits LyoWorkbenchBase

<LyoWorkbenchStatus Message="@StatusMessage" Severity="@StatusSeverity"/>
<MudButton OnClick="RunAsync">Run</MudButton>
```

### Toolbar cluster with rows and wrappers

```razor
<LyoToolbar Dense="true" Disabled="@_loading">
    <Rows>
        <LyoToolbarRow>
            <LyoToolbarGroup>
                <LyoToolbarButton Icon="@Icons.Material.Filled.Add" Text="New" OnClick="AddAsync"/>
            </LyoToolbarGroup>
            <LyoToolbarGroup AlignEnd="true">
                <LyoToolbarIconButton Icon="@Icons.Material.Filled.Refresh" Tooltip="Refresh" OnClick="ReloadAsync"/>
            </LyoToolbarGroup>
        </LyoToolbarRow>
    </Rows>
</LyoToolbar>
```

## Components

| Component | What it does |
| ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `LyoChip` / `LyoChips` / `LyoChipSpec` / `LyoChipHue` | MudChip wrapper taking either a spec or individual color, label, variant, and icon. Named `LyoChipHue` values (Blue, Teal, Rose, …) set `--lyo-chip-hue`; the chip mixes that into `--mud-palette-surface` so identity colors stay readable in light and dark themes. |
| `LyoChipInput` | Chip-style multi-value input for emails, tags, and freeform lists, with bulk paste splitting. |
| `LyoCheckSelect` / `LyoSelectOption` | Checkbox-backed multi-select dropdown, with optional search. |
| `LyoResponsiveTable` | Cards on narrow viewports, table on wide ones, for hand-rolled tables that cannot use the shared grids. |
| `LyoRecordCard` / `LyoRecordField` | The card primitives to fill `CardContent` with, so hand-rolled tables match the grid cards. |
| `LyoWorkbenchBase` / `LyoWorkbenchStatus` | Transient status banner mirrored to a snackbar, with versioned auto-clear. |
| `LyoViewportWatcher` | Wraps MudBlazor's `IBrowserViewportService` subscription, and stays inert when the host never registered MudBlazor services. |
| `LyoStatusChip` / `ILyoStatusPalette` | Status string to chip, with a pluggable domain palette and a shared vocabulary fallback. |
| `LyoResultBoundary` / `LyoEmptyState` / `LyoSkeletonPanel` | Empty, error, loading, and content branches from a `Lyo.Result`. |
| `LyoPageHeader` / `LyoBreadcrumbs` / `LyoSection` / `LyoToolbar` | Page-shell primitives so hosts stop inventing action and heading rows. `LyoToolbar` is a control cluster: shorthand `Start` / middle / `End` for one row, or a `Rows` slot of `LyoToolbarRow` children. Wrappers (`LyoToolbarButton`, `LyoToolbarIconButton`, `LyoToolbarMenu`, `LyoToolbarField`) plus `LyoToolbarGroup` / `LyoToolbarSeparator` own compact density. `Disabled` cascades to wrappers. Collapsible rows share one `RowsExpanded` toggle. |
| `LyoTabSet` / `LyoTab` | Tabs that persist the active panel per user and deep-link it. |
| `LyoLogViewer` / `LyoCodeBlock` | A read-only code surface with copy and line numbers, plus a virtualized log pane. |
| `LyoWorkbenchHost` / `LyoNavMenu` | Registry-driven workbench page and drawer links. Packages call `AddLyoWorkbench`. |

## Options and services

| Type | What it does |
| ------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| `IJsInterop` / `JsInterop` | Browser time zone, clipboard, and chunked or streaming file downloads. |
| `ILyoTimeZone` / `LyoBrowserTimeZone` | Circuit-scoped cache of the browser IANA time zone. JS failures are not cached, so prerender can retry. |
| `ILyoLayoutPreferences` | The user's card/table choice. Implemented by `ClientStore` in `Lyo.Web.Components`; register both with `AddLyoClientStore`. |
| `LyoDataGridOptions` / `LyoDataGridLayout` | Host layout defaults shared by the data grids and the responsive table. Registered with `AddLyoDataGrid`. |

## Wiring into a host

Register MudBlazor services, `ILyoTimeZone`, and `IJsInterop` as usual. `LyoResponsiveTable` resolves `IBrowserViewportService`, `IOptions<LyoDataGridOptions>`, and `ILyoLayoutPreferences` with `GetService` rather than injecting them, so every one of them is optional and a host that registers none of them still renders the table and follows the viewport.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)