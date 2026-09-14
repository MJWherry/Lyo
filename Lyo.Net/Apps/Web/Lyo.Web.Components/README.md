# Lyo.Web.Components

MudBlazor / Blazor pieces for Lyo web UI. Ships the data-grid and query-builder stack, `LyoForm` change tracking, `LyoFileUpload`, `LyoRichTextEditor`, `JsonEditor`, `LyoTextDiffViewer`, `IdWorkbench`, and smaller inputs. Targets `net10.0` (Razor SDK). Depends on `Blazored.LocalStorage` and `MudBlazor >= 9.3`.

Every visual component joins the `LyoElementRoot` element-id scheme. Each control gets a deterministic DOM id from the `ElementId` parameter, used for tests and analytics.

## Examples

### Projected grid — most recent address from ContactAddresses

```razor
<LyoDataGridProjected ApiClient="ApiClient" GridKey="PersonGrid" Route="Person"
                      KeySelector="@(i => ProjectedGridKeys.RawId(i))"
                      BeforeQuery="@(q => q.AddSelects("ContactAddresses.Address.UpdatedTimestamp", "ContactAddresses.Address.CreatedTimestamp"))">
    <Columns>
        <LyoProjectedColumn Field="FirstName" Title="Firstname"/>
        <LyoProjectedColumn Field="ContactAddresses.Address.FullAddress" Title="Most recent address" Sortable="false"/>
    </Columns>
</LyoDataGridProjected>
```

### Typed QueryConcrete grid — most recent address via LyoIdColumn

```razor
<LyoDataGrid T="PersonRes" ApiClient="ApiClient" GridKey="PersonGrid" Route="Person"
             KeySelector="@(i => new object[] { i.Id })"
             BeforeQuery="@(query => query.AddIncludes("contactaddresses.address"))">
    <Columns>
        <LyoPropertyColumn T="PersonRes" TProperty="string?" Property="x => x.FirstName" Title="Firstname"/>
        <LyoIdColumn T="PersonRes" TRes="PersonAddressRes"
                     Property="x => x.MostRecentAddressId"
                     Route="PersonAddress"
                     Title="Most recent address"
                     Select="@(new[] { "FullAddress", "City" })"
                     Context="address">
            @(address?.FullAddress)
        </LyoIdColumn>
    </Columns>
</LyoDataGrid>
```

### TypeScript — createLyoIdColumn

```tsx
createLyoIdColumn({
  id: "recentAddr",
  field: "MostRecentAddress.Id",
  header: "Most recent address",
  related: { route: "PersonAddress", select: ["FullAddress", "City"] },
  cell: (_row, address) => {
    const rec = address as { FullAddress?: string } | null;
    return rec?.FullAddress ?? "—";
  },
})
```

## Data grid (`DataGrid/`)

- **`LyoDataGrid<T>`.** Wraps `MudDataGrid` with an opt-in toolbar from `LyoDataGridFeatureFlags`: bulk export (CSV / XLSX / PDF), bulk delete, column-visibility menu, refresh, query-builder integration, and per-grid persisted state. `LyoDataGridState` and `ColumnVisibilityBinder` keep layouts in `Blazored.LocalStorage`.
- **Selection.** `DataGridSelectionTracker` keeps selected row keys as the store (`SelectedKeys`). `SelectedItems` is only the current page, derived after each load, so sort, paging, and delete cannot leave Mud's header-select-all checked against a different page. Bulk export, patch, and delete use `SelectedKeys`; call `ClearSelectionAsync` after a parent bulk delete. Custom `BulkMenuItems` should read `SelectedKeys` (or `ProjectedGridKeys.RowsFromKeys`) rather than `SelectedItems`.
- **`LyoDataGridProjected<T>`.** Grid for projected query rows (sparse / wide datasets). `LyoTypedProjectedColumn`, `LyoProjectedColumn`, and `ProjectedColumnRegistry` / `ProjectedValueHelper` look up values without strongly typed properties. `LyoTimestamp` (via `LyoProjectedColumn.Timestamp` / `TimestampLatest`) converts UTC instants to the browser IANA zone. Null or empty values render as an em dash character. `Identifier` on `LyoProjectedColumn` / `LyoPropertyColumn` renders compact `LyoIdField` (copy + suffix). Quick search ORs `QuickSearchPropertyName` columns and always includes leaf `Id` fields (Guid `Contains` matches the string form). `LyoDateTimeDisplay` parses and formats (UTC when no zone is supplied). `LyoDurationDisplay` shares duration chip colors/text with reporting and jobs. `LyoDataGridColumnStyles` keeps the checkbox and inline action columns content-sized. `LyoTruncatedText` / `MaxDisplayLength` ellipsize long cells and reveal the full value in a `LyoHoverPopover`, which stays open while the pointer is on it so the text can be selected.
- **Card layout.** Both grids render `LyoDataGridCards` instead of the table on narrow viewports, one card per row using the same cell markup, plus a sort menu for the sortable columns. Paging, sorting, selection, and loading stay with the grid. See *Responsive layout* for how the layout is chosen.
- **`LyoPropertyColumn`.** Column wrapper that feeds the query builder filter and sort metadata.
- **`LyoIdColumn`.** Related-entity column for a parent FK. After the page loads, the grid QueryProjects the related route with `Keys` = the ids on the page (one request per route + result type, chunked at 100). Typed child content receives `TRes` (or null). Blazor cannot overload a component tag by generic arity, so both grids use `LyoIdColumn<T, TRes>`: projected passes `T="object?"` and `Field`; QueryConcrete passes the row type and `Property`. `Select` lists related paths (or scalar properties of `TRes`); `Id` is always included. Prefer a navigation Select (`JobDefinition.Name`) when EF already has one. Live sample: TestGateway `PersonGrid2` looks up most recent address by `MostRecentAddressId`; `PersonGrid` (projected) selects `ContactAddresses.Address` and picks the newest `FullAddress`.
- **`FilterChipLabel` + `ChipLabelHelper`.** Chip-style filter readouts above the grid.
- **`ProjectedGridKeys`.** Key selectors for projected grids: `GuidId` (parses the `Id` field as a `Guid`, falling back to the raw value), `RawId`, `Composite(item, fields)`, and `RowsFromKeys` (stand-in rows so bulk handlers can read `Id` without depending on current-page objects). Replaces the five-line `GetKey` helper that was copy-pasted into every projected grid.
- **`ProjectedBoolOverrides`.** Optimistic toggle state for a boolean column (`Enabled`, `IsActive`, ...). `Get(item)` reads the override or falls through to the projected value; `PatchAsync` / `ToggleAsync` / `PatchManyAsync` PATCH through `IApiClient`, apply the override on success, and report through an optional `ISnackbar`. Call `Clear()` on every reload, otherwise a stale override masks the server value.

## Responsive layout (`Responsive/`)

- **Layout resolution.** A layout parameter set on the component wins; otherwise the layout the user last picked from a grid toolbar (kept in `ClientStore`), then `LyoDataGridOptions.Layout`. `Auto` means cards at or below `CardBreakpoint` and the table above it. The viewport and local storage are only read after the first render, so prerendering emits the table rather than flashing cards.
- **`AddLyoDataGrid`.** Host defaults for every grid and responsive table: `Layout` (`Auto` / `Table` / `Cards`, default `Auto`), `AllowLayoutToggle` (default true; false pins every grid to `Layout`), and `CardBreakpoint` (default `Sm`, raise to `Md` to card-ify tablets). Also takes an options instance, or `AddLyoDataGridFromConfiguration` for the `DataGrid` section. Optional — grids fall back to the same defaults when it is never called.
- **`LyoDataGridCards`.** The card renderer behind the grids: title column as the heading, remaining visible columns as label/value rows, row menu behind an overflow button, and a checkbox per card when the grid has selection.
- **`LyoGridCardColumn` / `ProjectedColumnRegistry.GetCardColumns`.** Turns declared columns into card columns, honouring column visibility and reusing each column's cell fragment. `CardTitle` on a column names the heading explicitly; otherwise the first `Name`-like label wins, falling back to the first non-identifier column.
- **`LyoResponsiveTable`.** Wrapper for hand-rolled `MudSimpleTable` / `MudTable` markup that cannot use the grids: `TableContent` for the existing table, `CardContent` for the narrow layout, and the same layout resolution. Works in hosts that register neither `AddLyoDataGrid` nor `ClientStore`.
- **`LyoRecordCard` / `LyoRecordField`.** The card primitives to fill `CardContent` with, so hand-rolled tables match the grid cards. `LyoRecordCard` takes `Title`, `Subtitle`, `TitleContent` (chips, status icons), `Actions`, and `Selected`; `LyoRecordField` is one label/value row that takes text or markup and can drop itself with `HideWhenEmpty`.
- **`LyoViewportWatcher`.** Wraps MudBlazor's `IBrowserViewportService` subscription for components that switch layouts. Start it from `OnAfterRenderAsync(firstRender: true)`; `IsAtOrBelow(breakpoint)` returns false until the first report, and the watcher stays inert when the host never registered MudBlazor services.

## Hover popover (`Popover/`)

`LyoHoverPopover` is the sticky stand-in for `MudTooltip`: hover opens it, and it stays up when the pointer moves onto the surface, so readers can scroll, select, and copy. A click on the anchor pins it until `Escape` or the close button. Overflow text from grid cells and filter chips goes through it, so any clipped description works the same way.

```razor
<LyoHoverPopover Title="Description" Text="@row.Description">
    <LyoTruncatedText Text="@row.Description"/>
</LyoHoverPopover>

@* Markup instead of text — links and buttons work because the popover stays open *@
<LyoHoverPopover>
    <ChildContent><MudIcon Icon="@Icons.Material.Filled.Info"/></ChildContent>
    <PopoverContent><LyoKeyedValueTable Items="@row.Parameters"/></PopoverContent>
</LyoHoverPopover>
```

`OpenDelayMs` (150) is how long the pointer must dwell before open; `CloseDelayMs` (300) is the grace window to reach the popover. `MaxWidth` / `MaxHeight` cap the content box and overflow scrolls. Use `Fixed` inside scrollers that would clip the popover, `Pinnable="false"` when the anchor already handles clicks, and `Disabled` to show only the anchor. The host layout needs `MudPopoverProvider`, which MudBlazor already requires for menus and selects.

## Dialogs (`Dialog/`)

- **`DialogService.ConfirmDeleteAsync(subject, consequences, title, confirmText)`.** The standard destructive-action prompt: red confirm button, `subject` in the question, optional `consequences` line. Returns `true` only when the user confirms.
- **`DialogService.ConfirmAsync(title, message, confirmText)`.** Neutral yes/no confirmation for non-destructive actions.
- Both replace hand-rolled `ShowMessageBoxAsync` calls so wording, button order, and the cancel-means-false contract stay consistent across features.

## Parameter editors (`ParamTable/`)

- **`LyoParameterEntry`.** Pairs an `ILyoParameterDefinition` with the value being edited. Seeds `Value` from the definition, or from the type's default JSON when the parameter is required or boolean; optional parameters without a stored default stay unset so a dropdown can be left empty. Exposes `Label` (with the required marker), `IsEmpty()`, `SubmitValue` (blank and `null` collapse to `null`), and `ShouldSubmit` (submit when set, required, or boolean). Statics `From(definitions, order)`, `RequiredSatisfied(entries)`, and `SiblingMap(entries)` build the list, validate it, and expose sibling values for expression-based defaults.
- **`LyoParameterValueGrid`.** Renders a list of `LyoParameterEntry` as editable rows via `LyoParameterValueField`. Used by the run dialogs, which previously each carried their own `ParamModel` and validation copy. Optional parameters pass `AllowUnset` so enum/options dropdowns are clearable. Pass `FormatterContext` so formatter-typed values get autocomplete and a live preview.
- **`LyoParameterEditRowSync.SaveAsync`.** Diffs an edited `LyoParameterEditRow` list against the server state and issues the resulting create / update / delete calls over a CRUD route. The Job and Reporting parameter views share this instead of duplicating the save loop.
- **`LyoKeyedValueTable`.** Read-only table over any `ILyoParameterValue` list, with configurable columns and sorting, for run and generation detail views.
- **`LyoParameterEditor` / `LyoParameterEditRow` / `LyoParameterValueField`.** The editable definition list (table or cards) with its row model and value input.
- **Default kind toggle.** `ShowDefaultKind` puts a Literal / Expression choice above the Default field, for definition parameters. Expression swaps the typed editor for the formatter template editor bound to `DefaultTemplate` and shows what the template resolves to, checked against the declared `Type` — so an invalid default is visible while authoring rather than on the first run. Off for schedule and trigger overrides, which supply values rather than declare defaults.

## Value editor plug-ins and setup

Another package can host a richer editor for one `LyoTypeEditorKind` without this assembly taking a reference to it. Register an `ILyoValueEditorDescriptor` and `LyoTypeValueInput` mounts the component via `DynamicComponent`; if nothing is registered the built-in editors run, so registration stays optional.

That is how the formatter template editor reaches parameter values: `Lyo.Formatter.Web.Components` references this package, so the dependency can only run that way round.

```csharp
builder.Services.AddLyoParameterEditor(o => {
    o.Layout = LyoParameterLayout.Cards; // Table is the default
    o.AllowLayoutToggle = true; // Header toggle, persisted per user
});

// Formatter-typed parameters get the template editor, with host keys for autocomplete.
builder.Services.AddLyoFormatterValueEditor(c => c.Add("client", sample));
```

`AddLyoParameterEditor` also accepts an options instance or `IConfiguration` (`AddLyoParameterEditorFromConfiguration`). `LyoSampleShape.FromType(type)` builds a nested placeholder dictionary from a CLR type when the real object is not available yet — a job definition that has never run, for instance — so the paths still autocomplete.

## Workbench shell

- `LyoWorkbenchBase` and `LyoWorkbenchStatus` live in [`Lyo.Web.Primitives`](../../../Core/Web/Lyo.Web.Primitives/README.md) (`Lyo.Web.Primitives` namespace). Inherit in the `.razor` file (`@inherits LyoWorkbenchBase`) rather than in the code-behind, so the partial class can still declare interfaces without a base-class conflict.

## Query builder UI

- **Query node editor (`QueryNodeEditor/`).** Visual builder for `Lyo.Query.Models` filter trees: `QueryNodeEditor`, `QueryNodeEditorPanel`, `QueryNodeEditorItem`, and `QueryFilterComponent` for the per-condition row. Comparison operators are constrained per `LyoTypeInfo.Category` via `Extensions.GetAvailableComparisonOperators`. `In` / `NotIn` are flagged through `IsMultiValueComparisonOperator`.
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

`LyoFileUpload` wraps `MudFileUpload` with optional drag-and-drop, progress chips, and temp-file streaming via `Lyo.IO.Temp`. Internal state lives in `LyoFileUploadState`. `ClientFileDisplayMode` chooses list vs chip layout. For long file names in tight layouts:

- **`ChipFileNameMaxLength`.** Short label inside each completed chip (`prefix…ext`). Full name in a tooltip when truncated.
- **`ChipMaxWidthCss`.** Optional CSS `max-width` on the chip (e.g. `100%` or `min(100%,12rem)`) so chips stay inside narrow columns. Combine with `ChipFileNameMaxLength`.

Completed chips always render the shortened display name, so truncation stays consistent in both chip layouts.

## Inputs

- **`LyoCheckSelect` / `LyoChipInput`.** Live in [`Lyo.Web.Primitives`](../../../Core/Web/Lyo.Web.Primitives/README.md) (`Lyo.Web.Primitives.CheckSelect` / `Lyo.Web.Primitives.ChipInput`).
- **`UniqueValueSelector/UniqueValueSelector`.** Selector that loads distinct values for a column via the `SpUniqueValueCount` shape (used by grid filters).
- **`ParamOptions/LyoParameterOptionsSelect`.** MudSelect from definition parameter `Options` JSON (static items or root `/Query`) or pipe `AllowedValues`, with `{{SiblingKey}}` live input binding. Formatter types keep custom `{...}` values (free text, not a closed token list).
- **`ParamOptions/LyoParameterOptionsEditor`.** Edits static key/label lists or root `QueryReq` via `QueryRootForm` for the `Options` column.
- **`ParamTable/LyoParameterEditRow`.** Shared editable parameter row (Key, Type, JSON Value, Required, Encrypt, Options, DefaultKind, DefaultTemplate) used by job and report hosts. `FromMasked` strips the API `***` mask.
- **`ParamTable/LyoParameterEditor`.** Parameter list in either layout — a dense table or one `LyoParameterCard` per parameter — over the same row internals (`LyoParameterKeyField`, `LyoParameterFlags`, `LyoParameterDetails`), so editing is identical either way. Flags: `ShowRequired`, `ShowEncrypt`, `ShowEnabled`, `ShowOptionsEditor`, `ShowDefaultKind`, `AllowReorder`, `InheritFrom`, `FormatterContext`. Reorder works by drag handle or up/down buttons. The layout comes from `AddLyoParameterEditor` and, when `AllowLayoutToggle` is on, a header toggle persisted per user through `ClientStore`. Viewports at or below `LyoDataGridOptions.CardBreakpoint` fall back to cards on their own, because the table does not fit; the toggle still overrides that.
- **`ParamTable/LyoParameterValueField`.** Value editor: password when encrypting at run time, Options/AllowedValues select, or `LyoTypeValueInput`. Forwards `FormatterContext` to formatter-typed values.
- **`LyoType/LyoTypeSelect`.** Ordered catalog picker (capitalized labels, Custom… / enum FullName). List and array are a Shape control beside the type. Pass `ShowFullNameField=false` to put the CLR FullName in a parent expander.
- **`LyoType/LyoTypeValueInput`.** JSON-backed value editor from `LyoTypeInfo.EditorKind` (Mud scalars with Guid/Uri parse errors, enum member dropdown when the CLR type is loaded, chip collections with numeric/bool/GUID guards, free-text SmartFormat templates, `JsonEditor` for JSON/binary/unknown).
- **`LyoType/LyoTypeChip`.** Capitalized type chip colored by `LyoTypeCategory` with FullName tooltip.
- **`Validation/LyoValidationWrapper`.** Wraps a field with validation message rendering.
- **`LyoDataGrid` / `LyoDataGridProjected`.** Data grids with filtering, bulk actions, and export menu items via [`Lyo.Web.Components.Export`](../Lyo.Web.Components.Export/README.md).
- **`JsonViewDialog` + `LyoElementRoot`.** Utility dialog and the element-id root used by every component. Optional `Path` and `Chips` render in the header (grid Dev request/response).
- **`Dialog/LyoDialog`.** Shared popup chrome (scrollable body, save/close). Header always has a raw-JSON debug button (`DebugData`). Click opens `JsonViewDialog`. Use `LyoDialogPresets` for size.

## Identifier workbench (`Identifiers/`)

- **`IdWorkbench`.** Interactive generator for GUID / KSUID / ULID / NanoID / Snowflake identifiers (V3 / V4 / V5 / V6 / V7 / COMB Postgres + SQL Server). Uses `Lyo.Common.Core.Identifiers`.
- **`IdResultPanel` + `IdEntry`.** Result display.

## Other top-level pieces

- **`ClientStore`.** `Blazored.LocalStorage`-backed key/value store used by the grid and form state binders.
- **`LyoElementRoot`.** Wraps a component and computes its DOM id from `ElementId` plus a default, normalizing the segment through `ElementIdSegmentNormalizer`. `GridRootElementId` and `ComponentTypeElementId` provide well-known prefixes (`DataGrid`, and others).
- **`IJsInterop` / `ILyoTimeZone` / `LyoBrowserTimeZone`.** Live in [`Lyo.Web.Primitives`](../../../Core/Web/Lyo.Web.Primitives/README.md).
- **`LyoTimestamp`.** UTC to browser-zone stamp (`LyoTimestampKind`: Absolute, Relative, TimeUntil, TimeSince). Absolute text uses a short zone abbrev (EST/EDT, or UTC±offset if the OS has no short name). Relative / until / since text has no zone (`in 2h 15m`). Tooltip is always the absolute local time plus abbrev. Relative kinds use `RelativeWindow` (±, default 24 hours). Outside that window they fall back to absolute.
- **`LyoResultErrorFormatter`.** Lives in [`Lyo.Web.Primitives`](../../../Core/Web/Lyo.Web.Primitives/README.md).

## Models (`Models/`)

- **`FilterPropertyDefinition`.** Queryable column metadata. `Type` is `LyoTypeInfo` (default `string`) and drives operators plus value widgets. `FromEnum` defaults to `LyoTypeInfo.Enum`.
- **`TimestampFormatEnum`.** Display format used by grid cells.
- **`BlazorUserInfo`.** Current-user info shared via cascading values.
- **`LocalBrowserFile` / `LocalBrowserFilePath`.** Uploaded browser files for the file upload component.

## Internal helpers (`Extensions`)

Shared visual helpers sit on internal `Extensions`: `GetStatusColor(string)`, `GetStatusIcon(string)`, `GetIcon(FileTypeFlags)`, `GetAvailableComparisonOperators(LyoTypeInfo)`, and the `IsMultiValueComparisonOperator` extension on `ComparisonOperatorEnum`. Status badges and operator menus on the grid, query editor, and form call these.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.DataTable.Models` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `Lyo.Web.Primitives` (direct, lyo)
- `Blazored.LocalStorage` `4.5.0` (direct, third-party)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)