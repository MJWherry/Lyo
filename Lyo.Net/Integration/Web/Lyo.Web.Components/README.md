# Lyo.Web.Components

Blazor / MudBlazor components for Lyo web UI. Ships the data-grid and query-builder stack, `LyoForm` change tracking, `LyoFileUpload`, `LyoRichTextEditor`, `JsonEditor`, `LyoTextDiffViewer`, `IdWorkbench`, and smaller inputs. Targets `net10.0` (Razor SDK). Depends on `Blazored.LocalStorage` and `MudBlazor >= 9.3`.

Every visual component joins the `LyoElementRoot` element-id scheme. Each control gets a deterministic DOM id from the `ElementId` parameter, used for tests and analytics.

## Data grid (`DataGrid/`)

- **`LyoDataGrid<T>`.** Wraps `MudDataGrid` with an opt-in toolbar from `LyoDataGridFeatureFlags`: bulk export (CSV / XLSX / PDF), bulk delete, column-visibility menu, refresh, query-builder integration, and per-grid persisted state. `LyoDataGridState` and `ColumnVisibilityBinder` keep layouts in `Blazored.LocalStorage`.
- **`LyoDataGridProjected<T>`.** Grid for projected query rows (sparse / wide datasets). `LyoTypedProjectedColumn`, `LyoProjectedColumn`, and `ProjectedColumnRegistry` / `ProjectedValueHelper` look up values without strongly typed properties. `LyoTimestamp` (via `LyoProjectedColumn.Timestamp` / `TimestampLatest`) converts UTC instants to the browser IANA zone. Null or empty values render as an em dash character. `Identifier` on `LyoProjectedColumn` / `LyoPropertyColumn` renders compact `LyoIdField` (copy + suffix). Quick search ORs `QuickSearchPropertyName` columns and always includes leaf `Id` fields (Guid `Contains` matches the string form). `LyoDateTimeDisplay` parses and formats (UTC when no zone is supplied). `LyoDurationDisplay` shares duration chip colors/text with reporting and jobs. `LyoDataGridColumnStyles` keeps the checkbox and inline action columns content-sized. `LyoTruncatedText` / `MaxDisplayLength` ellipsize long cells with a hover tooltip.
- **`LyoPropertyColumn`.** Column wrapper that feeds the query builder filter and sort metadata.
- **`FilterChipLabel` + `ChipLabelHelper`.** Chip-style filter readouts above the grid.

## Query builder UI

- **Query node editor (`QueryNodeEditor/`).** Visual builder for `Lyo.Query.Models` filter trees: `QueryNodeEditor`, `QueryNodeEditorPanel`, `QueryNodeEditorItem`, and `QueryFilterComponent` for the per-condition row. Comparison operators are constrained per `FilterPropertyType` via `Extensions.GetAvailableComparisonOperators`. `In` / `NotIn` are flagged through `IsMultiValueComparisonOperator`.
- **Query request builder (`QueryRequestBuilder/`).** Form editor for a `ProjectionQueryReq`: `IncludeList`, `SelectList`, `SortByList`, `KeysList`, `ComputedFieldsList`, `StartAmountFields`, and `QueryReqForm`, plus the `QueryRequestBuilder` component that renders the live query score.

## Forms (`Form/`)

- **`LyoForm<TModel>`.** `EditForm`-based change-tracking form. Renders Save/Reset, summarizes pending property changes plus create/update/delete operations for collection-bound children, and cascades itself as `ChangeTrackingForm` so nested inputs can register.
- **`LyoFormInput`.** Change-tracked single-value input that joins the cascaded form.
- **`LyoNullableTextField`.** Text field with explicit nullable semantics for the change tracker.
- **`LyoIdField`.** Read-only identifier field. Copy is the start adornment. UUID/string ids default to a 9-character suffix with expand/abbreviate at the end. Digit-only ids stay unclipped (copy only). `LyoIdAbbreviation` (`None` / `Prefix` / `Suffix`) and `AbbreviationLength` override the clip. `Compact` is the grid-cell variant.

## Rich text editor (`RichTextEditor/`)

- **`LyoRichTextEditor`.** MudBlazor-styled rich-text editor (Quill-derived) with a toolbar.
- **`LyoRichTextEditorController`.** Controller for get/set HTML, focus, and format.
- **`LyoRichTextEditorToolbarState`.** Toolbar state model for button sync.

## JSON editor (`JsonEditor/`)

- **`JsonEditor`.** Read/write JSON editor with JS interop (`JsonEditorJsInterop`).
- **`JsonTreeView`.** Collapsible tree viewer for JSON payloads.

## Text diff viewer (`TextDiff/`)

- **`LyoTextDiffViewer`.** Side-by-side or unified text diff with theme-aware styling.

## File upload (`FileUpload/`)

`LyoFileUpload` wraps `MudFileUpload` with optional drag-and-drop, progress chips, and temp-file streaming via `Lyo.IO.Temp`. Internal state lives in `LyoFileUploadState`. `ClientFileDisplayMode` picks chip vs list layout. For long file names in tight layouts:

- **`ChipFileNameMaxLength`.** Short label inside each completed chip (`prefix…ext`). Full name in a tooltip when truncated.
- **`ChipMaxWidthCss`.** Optional CSS `max-width` on the chip (e.g. `100%` or `min(100%,12rem)`) so chips stay inside narrow columns. Combine with `ChipFileNameMaxLength`.

Completed chips always render the shortened display name, so truncation stays consistent in both chip layouts.

## Inputs

- **`CheckSelect/LyoCheckSelect`.** Multi-select dropdown backed by `LyoSelectOption`.
- **`ChipInput/LyoChipInput`.** Chip-style multi-value input (tags / emails / freeform).
- **`UniqueValueSelector/UniqueValueSelector`.** Selector that loads distinct values for a column via the `SpUniqueValueCount` shape (used by grid filters).
- **`ParamOptions/LyoParameterOptionsSelect`.** MudSelect from definition parameter `Options` JSON (static items or root `/Query`) or pipe `AllowedValues`, with `{{SiblingKey}}` live input binding.
- **`ParamOptions/LyoParameterOptionsEditor`.** Edits static key/label lists or root `QueryReq` via `QueryRootForm` for the `Options` column.
- **`Validation/LyoValidationWrapper`.** Wraps a field with validation message rendering.
- **`LyoDataGrid` / `LyoDataGridProjected`.** Data grids with filtering, bulk actions, and export menu items via [`Lyo.Web.Components.Export`](../Lyo.Web.Components.Export/README.md).
- **`JsonViewDialog` + `LyoElementRoot`.** Utility dialog and the element-id root used by every component. Optional `Path` and `Chips` render in the header (grid Dev request/response).
- **`Dialog/LyoDialog`.** Shared popup chrome (scrollable body, save/close). Header always has a raw-JSON debug button (`DebugData`). Click opens `JsonViewDialog`. Use `LyoDialogPresets` for size.

## Identifier workbench (`Identifiers/`)

- **`IdWorkbench`.** Interactive generator for GUID / KSUID / ULID / NanoID / Snowflake identifiers (V3 / V4 / V5 / V6 / V7 / COMB Postgres + SQL Server). Uses `Lyo.Common.Identifiers`.
- **`IdResultPanel` + `IdEntry`.** Result display.

## Other top-level pieces

- **`ClientStore`.** `Blazored.LocalStorage`-backed key/value store used by the grid and form state binders.
- **`LyoElementRoot`.** Wraps a component and computes its DOM id from `ElementId` plus a default, normalizing the segment through `ElementIdSegmentNormalizer`. `GridRootElementId` and `ComponentTypeElementId` provide well-known prefixes (`DataGrid`, and others).
- **`IJsInterop` / `JsInterop`.** JS bridge for clipboard, focus, and downloads, plus `GetClientTimeZoneInfo` which imports `wwwroot/scripts/lyoTimeZone.js` (`Intl.DateTimeFormat` IANA id).
- **`ILyoTimeZone` / `LyoBrowserTimeZone`.** Circuit-scoped cache of the browser time zone. Register with `TryAddScoped<ILyoTimeZone, LyoBrowserTimeZone>()` next to `IJsInterop`.
- **`LyoTimestamp`.** UTC to browser-zone stamp (`LyoTimestampKind`: Absolute, Relative, TimeUntil, TimeSince). Absolute text uses a short zone abbrev (EST/EDT, or UTC±offset if the OS has no short name). Relative / until / since text has no zone (`in 2h 15m`). Tooltip is always the absolute local time plus abbrev. Relative kinds use `RelativeWindow` (±, default 24 hours). Outside that window they fall back to absolute.
- **`LyoResultErrorFormatter`.** Renders `Lyo.Result` errors the same way across components.

## Models (`Models/`)

- **`FilterPropertyDefinition` + `FilterPropertyType`.** Describe a queryable column (`String`, `Number`, `Enum`, `DateTime`, `Bool`, and others) for the query builder and grid.
- **`TimestampFormatEnum`.** Display format used by grid cells.
- **`BlazorUserInfo`.** Current-user info shared via cascading values.
- **`LocalBrowserFile` / `LocalBrowserFilePath`.** Uploaded browser files for the file upload component.

## Internal helpers (`Extensions`)

Internal `Extensions` holds shared visual helpers: `GetStatusColor(string)`, `GetStatusIcon(string)`, `GetIcon(FileTypeFlags)`, `GetAvailableComparisonOperators(FilterPropertyType)`, and the `IsMultiValueComparisonOperator` extension on `ComparisonOperatorEnum`. Status badges and operator menus on the grid, query editor, and form call these.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Common` (direct, lyo)
- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `Blazored.LocalStorage` `4.5.0` (direct, third-party)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)