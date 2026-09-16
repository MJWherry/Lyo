# lyo-web-components

MUI-based React component library for Lyo Next.js apps. Mirrors `Lyo.Web.Components` (MudBlazor): theme, query builders, DataGrid wired to Lyo Query, change-tracking forms, and editors.

Peer React 19 + `@emotion/react` / `@emotion/styled`. Depends on `lyo-query` and `lyo-api-client`.

## Setup (Next.js App Router)

```tsx
// app/layout.tsx
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { LyoProvider } from "lyo-web-components";
import "lyo-web-components/styles.css";

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html>
      <body>
        <AppRouterCacheProvider>
          <LyoProvider>{children}</LyoProvider>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
```

`LyoProvider` reads CSS variables (`--accent`, `--bg`, `--ink`, `--danger`, `--ok`) and `data-theme` so it matches existing comic/portfolio tokens. Pass `theme` to override.

Do **not** point the grid at TestApi from the browser. Use a BFF:

```tsx
"use client";
import {
  LyoDataGrid,
  LyoDataGridProjected,
  createBffQueryClient,
  defaultPersonGridColumns,
  LyoDataGridFeatureFlags,
} from "lyo-web-components";
import { filterProperty } from "lyo-query";

const client = createBffQueryClient({
  projectPath: "/api/person/grid?mode=project",
  concretePath: "/api/person/grid?mode=concrete",
  queryPath: "/api/person/grid?mode=query",
  exportPath: "/api/person/export",
});

export function PersonGridDemo() {
  return (
    <LyoDataGrid
      mode="project"
      apiClient={client}
      gridKey="PersonGridDemo"
      route="person"
      columns={defaultPersonGridColumns()}
      keySelector={(row) => [row.Id ?? row.id]}
      quickSearchProperties={["FirstName", "LastName"]}
      filterPropertyDefinitions={[
        filterProperty("FirstName"),
        filterProperty("LastName"),
        filterProperty("SourceEntityType"),
      ]}
      features={
        LyoDataGridFeatureFlags.Filterable |
        LyoDataGridFeatureFlags.Searchable |
        LyoDataGridFeatureFlags.AutoRefresh |
        LyoDataGridFeatureFlags.BulkMenu |
        LyoDataGridFeatureFlags.BulkExport
      }
    />
  );
}
```

How the grid maps to Lyo Query:

- Search → OR of `Contains` across `quickSearchProperties` (or `quickSearch` columns). Every column whose last path segment is `Id` is added, so pasting an id finds its row. The placeholder lists the searched columns (`Search Name, Id`).
- Column filters → `ConditionClause` chips ANDed into `whereClause`
- Sort → `SortBy[]`, using a column's `sortKey` when it declares one
- Page → `Start` / `Amount`
- Projected grid → `POST {route}/QueryProject` with `Select` from visible columns. Hiding a column does not reload; showing one reloads only when it needs a field the last request did not load.
- Root Query → `POST /Query` with `From` + alias.property `Select`
- Concrete → `POST {route}/QueryConcrete`

Columns:

- `createLyoIdColumn` marks an identifier column (never the card heading). With `related: { route, select, client? }` the grid QueryProjects the related rows by `Keys` after each page and passes the match to `cell`. Without a `cell` the value renders as a compact `LyoIdField`. `related.client` points the lookup at a different API than the grid's.
- `timestamp` renders `LyoTimestamp` (`timestampLatest` for collections, `timestampRelativeWindowMs` for "5 minutes ago" text). `format`, `maxDisplayLength` (ellipsis with a tooltip), and `align` cover plain text cells.

Toolbar and layout:

- The Bulk menu shows what an action touches (`(12 items)`, `(first 2,000 of 5,000)`, `(too many items)`) and offers Deselect (N), Export, Patch (`LyoBulkPatchDialog`, posted to `patchRoute` or `route`), and Delete. `maxBulkSize` (default 2000) caps selections; unselected exports take the first `maxBulkSize` rows.
- Auto refresh picks an interval from `autoRefreshIntervalsSeconds` and stops itself when a load fails. Stale responses from an earlier request are dropped.
- `layout` is `Auto`, `Table`, or `Cards`. `Auto` switches to cards at or below `cardBreakpoint` (default `sm`). The user's toggle is saved per `gridKey`. Cards use the `cardTitle` column, or the first name-like column, as the heading, and get a Sort menu.
- `loadingContent` replaces the empty body while the first page loads.

## Query builders

`WhereClauseBuilder`, `QueryBuilder`, and `QueryWorkbench` live here. `lyo-query-components` re-exports them for existing imports.

- `QueryBuilder` tabs render `QueryConcreteForm`, `QueryProjectionForm`, and `QueryRootForm`, which are also exported on their own. The forms cover Keys (one chip per key set, `"a", 2` for composite keys), ComputedFields, the zip-sibling-collections switch, and root joins with Type, As, and On pairs. Each form shows a `QueryScoreChip`: the request's query score from `scoreQueryRequest` in `lyo-query` (a port of the server's `QueryRequestScorer`), with the breakdown in a tooltip.
- `QueryNodeEditor` is a where-clause tree: select a node to edit it, add groups, conditions, and sub clauses, set a Description, and drag a node onto a group to move it. Field names must be letters and dots. Values use typed inputs from the field definition (bool, number, date, date-time, time). The tree operations are exported as pure helpers (`addChildAt`, `moveInto`, `setDescriptionAt`, …; `removeWhereNodeAt` for removal).
- `QueryFilterComponent` takes `loadUniqueValues(field)` to fetch value counts for fields whose definitions have none.

Breaking: `QueryNodeEditor` used to be an alias of `QueryFilterComponent`. Use `QueryFilterComponent` for the single-filter row.

## Shared primitives

Ports of `Lyo.Web.Primitives` and the shared parts of `Lyo.Web.Components`. Each component sits on a pure helper that is exported too, so hosts can reuse the formatting without the UI.

| Area | Components | Helpers |
| --- | --- | --- |
| Date display | `LyoTimestamp` (Absolute / Relative / TimeUntil / TimeSince), `LyoDuration`, `useBrowserTimeZone`, `useNow` | `toUtcMs`, `latestUtcMs`, `formatRelative`, `formatKind`, `formatAbsolute`, `timeZoneLabel`, `formatDuration`, `formatClock`, `durationColor` |
| Ids and text | `LyoIdField` (compact or text field, copy, expand), `LyoTruncatedText`, `LyoHighlighter`, `useCopyToClipboard` | `abbreviateId`, `idFieldCanToggle`, `splitHighlight`, `truncateText` |
| Chips | `LyoChip` (spec, icon key, or `hue`), `LyoChips` factory, `LyoStatusChip` (domain palettes, then the shared vocabulary) | `chipOf`, `chipFromBool`, `chipFromEnum`, `LyoChipHues`, `resolveDefaultStatus`, `createStatusPaletteResolver` |
| Toolbar | `LyoToolbar` (start/middle/end, or `rows` with collapsible extras and toolbar-wide `disabled`), `LyoToolbarRow`, `LyoToolbarGroup`, `LyoToolbarButton`, `LyoToolbarIconButton`, `LyoToolbarMenu`, `LyoToolbarField`, `LyoToolbarSeparator` | |
| Page layout | `LyoPageHeader`, `LyoBreadcrumbs`, `LyoSection`, `LyoTabSet` (syncs `?tab=` and a stored preference) | `slugTab`, `resolveActiveTab` |
| State and output | `LyoEmptyState`, `LyoSkeletonPanel`, `LyoResultBoundary`, `LyoCodeBlock`, `LyoLogViewer` | `resolveResultPhase`, `formatResultErrors`, `withLineNumbers`, `filterLogEntries`, `formatLogEntries` |
| Responsive | `LyoResponsiveTable`, `LyoRecordCard`, `LyoRecordField`, `useViewport` | `breakpointOf`, `isAtOrBelow` |
| Overlays | `LyoHoverPopover` (hover delay, pin on click), `LyoExpandableCard`, `LyoDialog` `titleChips` + `debugData` | |

Timestamps parse naive ISO strings as UTC (the API default) and render in the browser zone with a short label (`EDT`, `CEST`, `UTC+5:45`). The first render uses UTC so server and client markup match, then switches to the browser zone after mount.

## JSON viewer and editor

- `JsonTreeView` is virtualized (`@tanstack/react-virtual`) and colours values from the theme: keys bold `text.primary`, strings `success.main`, numbers `primary.main`, booleans `warning.main`, null `text.secondary`, and containers a muted `{…} n` / `[…] n` badge. Every row has a copy button on hover. Pass `editable` and `onChange` for inline editing: double-click a key to rename it or a value to edit it, and use the row buttons to add a child, set null, or remove. `enumOptions(path)` turns the value editor into a select.
- `useJsonTreeState(value)` holds the expanded set and search state (matches, `n/total`, next/previous), so a custom toolbar can drive the tree. `JsonTreeSearch` is the stock search box.
- `JsonEditor` switches between Raw (CodeMirror) and Tree. The toolbar has search, expand/collapse all, zoom, a tree-edit toggle, and Format JSON. It shows an alert on parse errors. `value` may be JSON text or any value. Edits arrive through `onChange` (text) and `onValueChange` (parsed).
- `useJsonViewDialog().show(value, title, { path, chips })` opens the tree in a dialog with a path chip, status chips, and Copy JSON.
- `WhereClauseViewDialog` has Readable (`printWhere` from `lyo-query`) and JSON tabs.
- DataGrid Dev menu: View Request shows the request with the endpoint chip. View Response adds elapsed, HTTP status, count / total, score, and has-more chips (`responseDevChips`). Each row has a Dev menu with View Json.

## Fuzzy filters

`lyo-query` exports `DEFAULT_FUZZY_THRESHOLD` (0.3), `normalizeThreshold` (clamps to 0–1, blank means the server default), `conditionEquals` (Threshold included), `comparisonDescription` (`=`, `≥`, `Not Contains`), and `printWhere` (appends ` threshold=n` for fuzzy conditions). Grid filter chips use `filterChipText`, which shows the display name and operator description, compacts lists to four values, and adds the threshold. Chips longer than 48 characters show the full line on hover and in a view dialog. Adding a filter equal to an existing one is a no-op.

## LyoType

The catalog mirrors the server `LyoTypeInfo`: 29 rows (scalars, temporal, JSON documents, `List<T>`, `T[]`, `byte[]`) with category, editor kind, aliases, and default JSON.

- `lyoTypeFromName` resolves a full name, short name, CLR `Type.Name`, or alias (case-insensitive). It also builds `List<T>` / `T[]` rows for any known element, and matches assembly-qualified `List` names from any runtime version. Anything else is `LyoTypes.Unknown`. `registerLyoType` adds package-owned rows, and repeat calls with the same full name are no-ops.
- `lyoTypeUi` helpers match the Blazor `LyoTypeUi`: category hues, `typeChip`, `pickerGroups`, `composeFullName` (value / list / array), `shouldResetValue`, `chipValidation`, `validateTypedJson`, and `valuePreview`.
- `LyoTypeChip` draws the category chip. `LyoTypeSelect` is a grouped picker with a Shape select (Value / List / Array) for wrappable types and a CLR FullName field for Custom… and Enum.
- `LyoTypeValueInput` edits a JSON payload by type:
  - switch for bool, numeric fields, and date, time, or datetime (UTC) inputs;
  - GUID and URI fields with validation;
  - an enum select when `enumNames` is given;
  - validated chips for lists;
  - the JSON tree for documents, binary, and unknown types.

  `allowUnset` keeps blanks as `null`. `isEncrypted` masks scalar text.
- `LyoValueEditorProvider editors={{ Formatter: MyTemplateEditor }}` replaces the stock editor for one editor kind. Plugged editors receive `LyoValueEditorProps`.
- `lyo-query` exports `LyoTypeCategory` and `operatorsForCategory` (Identifier filters like text; Json, Collection, Binary, and Unknown allow every operator).
- `ChipInput` accepts `validationPattern` and `validationMessage` and rejects chips that fail the pattern.

## Parameters

- `paramOptions` ports the server `ParameterOptions` model:
  - `parseParameterOptions` accepts camelCase or PascalCase, and string or numeric kinds. `serializeParameterOptions` writes the server shape.
  - `createDefaultOptionsJson(kind)` returns the starter document for a kind.
  - `optionsFromAllowedValues` and `allowedValuesFromOptions` convert between Options and AllowedValues.
  - `bindQueryOptions` and `bindSprocParameters` fill `{{Key}}` placeholders from sibling values and report missing ones.
  - `readKeyValue` reads a picker key and label from a row: `Key`/`Value` columns, then select paths, then explicit fields.
  - `pruneSelection` drops selected keys that are no longer offered.
- `LyoParameterOptionsSelect` loads Static, Query (`apiClient.query`), or Sproc (`resolveOptions`) options. It reloads when sibling values change, prunes stale keys, and supports `required` and multi-select (JSON array in `listKind`).
- `LyoParameterOptionsEditor` edits the Options document:
  - Static: a key/label grid.
  - Root query: entity, alias, amount, route, Select chips, computed fields, and a where builder.
  - Sproc: name, key and label columns, and an argument grid.
- `editRow` has the row and entry models:
  - `editRowFromMasked` treats `***` as an encrypted value. `rowForSave` never sends the mask.
  - `addParameterRow`, `moveParameterRow`, `deleteParameterRow`, `setRowType` (resets values that no longer fit), `setRowKey` (inherits type and options), and `setRowOptions` (keeps AllowedValues in sync).
  - `editRowErrors` reports blank keys, duplicate keys, and values that don't match the type.
  - `entriesFrom`, `entryIsEmpty`, `entryShouldSubmit`, `requiredSatisfied`, and `siblingMap` back run dialogs.
  - `saveParameterRows(rows, loadedIds, { remove, create, update })` saves in the order delete, create, update.
- Components:
  - `LyoParameterEditor` has table and card layouts. The layout is remembered per browser, and narrow screens use cards. It supports add, delete, drag and arrow reorder, and a details panel per row.
  - `LyoParameterDetails` edits the type, CLR name, options kind and editor, description, and the literal or expression default. Pass `describeExpression` to caption expression defaults.
  - `LyoParameterCard`, `LyoParameterFlags`, `LyoParameterKeyField`, and `LyoParameterValueField` are the building blocks. The value field switches between a password field, an options select, and `LyoTypeValueInput`.
  - `LyoParameterValueGrid` is the run-dialog grid. `LyoKeyedValueTable` is a read-only key/value list.

## Forms, validation, and bulk patch

- `LyoFormInput` draws a typed editor from `kind`: `bool`, `integer`, `number`, `enum`, `flags`, `date`, `datetime`, `time`, `guid`, `string`, or `richtext`. When `kind` is omitted it is inferred from the value. Invalid text (a bad GUID, a non-integer) shows an error and is not committed.
- Inside `LyoFormGrid`, each field sizes itself from `fieldSize` (`Small`, `Medium`, `Large`, or `Full`), or from its kind when the size is `Auto`.
- `LyoForm` tracks changes with `propertyChange`, which compares dates by instant and arrays and objects by value. So re-creating an equal value no longer counts as a change.
- `LyoValidationWrapper` runs `validator(value)` whenever the value changes and shows the returned messages under the field with an error border. Set `showSnackbar` to also show them as snackbars. `ref.validate()` and `ref.reset()` are available.
- `LyoBulkPatchDialog` lets the user pick a property and a typed value (`LyoTypeValueInput`), then calls `client.bulkPatch(route, keys, { [property]: value })`. The route defaults to the grid route. `bulkPatchSummary` produces the "Patched n rows, m failed" snackbar text.

## Editors and tools

- `LyoRichTextEditor` (Tiptap) has font and size selects, bold / italic / underline / strike, lists, blockquote, a code block with a language select, and undo / redo. Override the choices with `fonts`, `sizes`, and `codeLanguages` (defaults `LYO_RTE_FONTS`, `LYO_RTE_SIZES`, `LYO_RTE_CODE_LANGUAGES`). `readOnly` hides the toolbar.
- `LyoTextDiffViewer` shows Split or Unified line diffs with +/− counts, ignore-whitespace and ignore-case options, search with next/previous (Enter, Shift+Enter), and a draggable split (double-click resets it). Pass `onOriginalChange` / `onModifiedChange` to get an Edit toggle. The diff and search logic is exported (`diffText`, `diffStats`, `searchHits`, `highlightSegments`, `stepHit`). Ignore-whitespace drops all whitespace when comparing lines, as in Blazor.
- `DataTablePreview` is virtualized, with a sticky header, optional row numbers, and object cells shown as JSON. Set `height` and `rowHeight` to size it.
- `IdWorkbench` generates UUID v1/v3/v4/v5/v6/v7 (v3 and v5 take a namespace, DNS / URL / OID / X500 or a custom GUID, and a name), KSUID, ULID, NanoID (custom alphabet and size), Snowflake (machine id 0–1023), and auto-increment ranges. Time-based ids show their timestamp under the value. The generators are exported from `idOptions` (`generateGuids`, `generateNanoIds`, `autoIncrementIds`, …).
- `UniqueValueSelector` has Select all and Clear all, which act on the values currently shown. `valueFormatter` sets the label for each value, and local search matches that label.

## Package catalog

These components read the generated catalog assets (`index.json` and `packages/{id}.json`, the same files as `Lyo.Web.Components/wwwroot/catalog/`) from `catalogBasePath`. Serve or copy that folder and pass its URL. Pass a stable `fetchFn` to add headers.

- `LyoPackageCatalog` is the browser. It has search (name, id, tagline, topic), area chips, a package list, and Docs and Dependencies tabs.
- `PackageDoc` renders one package: description, features, examples, benchmarks, sections, and links. `CatalogSectionList` renders just the sections. Markdown is shown as plain text, as in Blazor.
- `LyoDependencyGraph` lists direct dependencies grouped by kind, with Lyo packages highlighted.
- The loaders and parsers are exported (`fetchCatalogIndex`, `fetchCatalogPackage`, `parseCatalogIndex`, `parseCatalogPackage`, `filterCatalogPackages`, `groupDependencies`). The parsers accept camelCase or PascalCase property names.

### Breaking changes

- `LyoChip` is now a component instead of the raw MUI `Chip`. MUI `Chip` props still work; the default size is `small`.
- `LyoFormInput` takes `value: unknown` and `onChange(next: unknown)`. Like the Blazor component, it is read-only unless `allowEditing` is set.
