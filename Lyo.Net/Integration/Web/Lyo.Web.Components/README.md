# Lyo.Web.Components

Blazor / MudBlazor component library for the Lyo web UI. Provides the data-grid + query-builder stack, change-tracking form, file upload, rich-text editor, JSON editor, text-diff
viewer, identifier workbench, and several smaller inputs. Targets `net10.0` (Razor SDK) and depends on `Blazored.LocalStorage` and `MudBlazor >= 9.3`.

Every visual component participates in the `LyoElementRoot` element-id scheme, so each rendered control gets a deterministic DOM id derived from the component's `ElementId`
parameter — useful for testing and analytics.

## Data grid (`DataGrid/`)

- **`LyoDataGrid<T>`** — wraps `MudDataGrid` with an opt-in feature toolbar driven by `LyoDataGridFeatureFlags`: bulk export (CSV / XLSX / PDF), bulk delete, column-visibility
  menu, refresh, query-builder integration, and per-grid persisted state. Backed by `LyoDataGridState` and `ColumnVisibilityBinder` so layouts survive reloads via
  `Blazored.LocalStorage`.
- **`LyoDataGridProjected<T>`** — variant for grids whose rows are projected from a query (sparse / wide datasets). Uses `LyoTypedProjectedColumn`, `LyoProjectedColumn`, and
  `ProjectedColumnRegistry` / `ProjectedValueHelper` to look up values without strongly-typed properties.
- **`LyoPropertyColumn`** — column wrapper that integrates with the query builder for filtering / sorting metadata.
- **`FilterChipLabel`** + **`ChipLabelHelper`** — chip-style filter readouts shown above the grid.

## Query builder UI

- **Query node editor (`QueryNodeEditor/`)** — visual builder for `Lyo.Query.Models` filter trees: `QueryNodeEditor`, `QueryNodeEditorPanel`, `QueryNodeEditorItem`, and
  `QueryFilterComponent` for the per-condition row. Comparison operators are constrained per `FilterPropertyType` via the internal `Extensions.GetAvailableComparisonOperators`,
  with `In` / `NotIn` flagged through `IsMultiValueComparisonOperator`.
- **Query request builder (`QueryRequestBuilder/`)** — full-form editor for a `ProjectionQueryReq`: `IncludeList`, `SelectList`, `SortByList`, `KeysList`, `ComputedFieldsList`,
  `StartAmountFields`, and `QueryReqForm` plus the umbrella `QueryRequestBuilder` component that renders the live query score.

## Forms (`Form/`)

- **`LyoForm<TModel>`** — `EditForm`-based change-tracking form. Renders Save/Reset actions, summarises pending property changes plus create/update/delete operations for
  collection-bound children, and cascades itself as `ChangeTrackingForm` so nested inputs can register themselves.
- **`LyoFormInput`** — change-tracked single-value input that participates in the cascaded form.
- **`LyoNullableTextField`** — text field with explicit nullable semantics for the change tracker.

## Rich text editor (`RichTextEditor/`)

- **`LyoRichTextEditor`** — MudBlazor-styled rich-text editor (Quill-derived) with full toolbar.
- **`LyoRichTextEditorController`** — controller object for programmatic interactions (get/set HTML, focus, format).
- **`LyoRichTextEditorToolbarState`** — toolbar state model for synchronising buttons.

## JSON editor (`JsonEditor/`)

- **`JsonEditor`** — read/write JSON editor with JS interop (`JsonEditorJsInterop`).
- **`JsonTreeView`** — collapsible tree viewer for inspecting JSON payloads.

## Text diff viewer (`TextDiff/`)

- **`LyoTextDiffViewer`** — side-by-side / unified text diff with theming-aware styling.

## File upload (`FileUpload/`)

**`LyoFileUpload`** wraps **`MudFileUpload`** with optional drag-and-drop, progress chips, and temp-file streaming via `Lyo.IO.Temp`. Internal state lives in `LyoFileUploadState`;
the chip vs. list layout is selected by `ClientFileDisplayMode`. For long file names in tight layouts:

- **`ChipFileNameMaxLength`** — Short label inside each completed chip (`prefix…ext`); full name in a tooltip when truncated.
- **`ChipMaxWidthCss`** — Optional CSS `max-width` on the chip (e.g. `100%` or `min(100%,12rem)`) so chips stay inside narrow columns; combine with **`ChipFileNameMaxLength`**.

Completed chips always render the shortened display name (not the raw full name when under the max length), so truncation stays consistent in both chip layouts.

## Inputs

- **`CheckSelect/LyoCheckSelect`** — multi-select dropdown backed by `LyoSelectOption`.
- **`ChipInput/LyoChipInput`** — chip-style multi-value input (tags / emails / freeform).
- **`UniqueValueSelector/UniqueValueSelector`** — selector that loads distinct values for a column via the `SpUniqueValueCount` shape (used by grid filters).
- **`Validation/LyoValidationWrapper`** — wraps a field with consistent validation message rendering.
- **`ExportColumnSelectorDialog`** (top-level) — dialog for choosing which columns participate in CSV / XLSX export.
- **`JsonViewDialog`** + **`LyoElementRoot`** — utility dialog and the element-id root used by every component.

## Identifier workbench (`Identifiers/`)

- **`IdWorkbench`** — interactive generator for GUID / KSUID / ULID / NanoID / Snowflake identifiers (V3 / V4 / V5 / V6 / V7 / COMB Postgres + SQL Server). Uses
  `Lyo.Common.Identifiers`.
- **`IdResultPanel`** + **`IdEntry`** — supporting result display.

## Other top-level pieces

- **`ClientStore`** — `Blazored.LocalStorage`-backed key/value store used by the grid + form state binders.
- **`LyoElementRoot`** — wraps a component and computes its DOM id from `ElementId` + a default, normalising the segment through `ElementIdSegmentNormalizer`. `GridRootElementId`
  and `ComponentTypeElementId` provide well-known prefixes (`DataGrid`, etc.).
- **`IJsInterop` / `JsInterop`** — small JS bridge for clipboard / focus operations.
- **`LyoResultErrorFormatter`** — renders `Lyo.Result` errors consistently across components.

## Models (`Models/`)

- **`FilterPropertyDefinition`** + **`FilterPropertyType`** — describe a queryable column (`String`, `Number`, `Enum`, `DateTime`, `Bool`, …) for the query builder and grid.
- **`TimestampFormatEnum`** — display format used by grid cells.
- **`BlazorUserInfo`** — current-user info shared via cascading values.
- **`LocalBrowserFile`** / **`LocalBrowserFilePath`** — abstractions over uploaded browser files for the file upload component.

## Internal helpers (`Extensions`)

Internal `Extensions` provides shared visual helpers — `GetStatusColor(string)`, `GetStatusIcon(string)`, `GetIcon(FileTypeFlags)`,
`GetAvailableComparisonOperators(FilterPropertyType)`, and the `IsMultiValueComparisonOperator` extension on `ComparisonOperatorEnum`. These power the consistent status badges and
operator menus used across the grid, query editor, and form.

## Dependencies

*(Synchronized from `Lyo.Web.Components.csproj`.)*

**Target framework:** `net10.0` (Razor SDK)

### NuGet packages

| Package                 | Version  |
|-------------------------|----------|
| `Blazored.LocalStorage` | `4.5.0`  |
| `MudBlazor`             | `[9.3,)` |

### Project references

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Csv`](../../../Data/Csv/Lyo.Csv/README.md)
- [`Lyo.IO.Temp`](../../../Data/IOTemp/Lyo.IO.Temp/README.md)
- [`Lyo.Pdf`](../../../Data/Pdf/Lyo.Pdf/README.md)
- [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
- [`Lyo.Validation`](../../../Core/Validation/Lyo.Validation/README.md)
- [`Lyo.Xlsx`](../../../Data/Xlsx/Lyo.Xlsx/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
