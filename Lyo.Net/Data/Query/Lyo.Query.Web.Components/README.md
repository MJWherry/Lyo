# Lyo.Query.Web.Components

Reusable Blazor / MudBlazor components for editing and running
[`Lyo.Query.Models`](../Lyo.Query.Models/README.md) requests against any Lyo.Api host.

Razor SDK targeting `net10.0`. Requires `MudBlazor` 9.3+ and
[`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) for the
shared JSON editor, filter widgets, and client storage.

## Components

- `QueryBuilderWorkbench.razor` (`QueryBuilderWorkbench.razor.cs`) — the
  full-screen workbench that pairs a query editor with a runner panel. Holds the
  current `QueryReq` (for `/Query`) and `ProjectionQueryReq` (for `/QueryProject`)
  plus the shared bookkeeping (`Include`, `Select`, `Keys`). Auto-persists state to
  the browser via `ClientStore.GetQueryWorkbenchStateAsync` /
  `SetQueryWorkbenchStateAsync` with a 450 ms debounce; final state is also flushed
  on dispose.
- `QueryBuilderSettings.razor` (`QueryBuilderSettings.razor.cs`) — host /
  endpoint / run-mode selector embedded in the workbench.
- `QueryRunPanel.razor` (`QueryRunPanel.razor.cs`) — issues HTTP requests for the
  current query, renders the response in a JSON editor, and surfaces latency / size
  metrics. Loads its splitter behavior from
  `_content/Lyo.Query.Web.Components/scripts/queryWorkbenchSplitter.js`.

`QueryBuilderWorkbench` exposes a `Title` parameter and an optional `Routes`
(`Dictionary<string, List<string>>`) seed for host → route templates when no
persisted state is found.

## State types

- `QueryWorkbenchPersistedState` — `EntityQuery` (`QueryReq?`),
  `QueryRequest` (`ProjectionQueryReq`), `IncludeAll`, `SelectAll`, `KeysAll`, and
  `Run` (`QueryWorkbenchRunConfiguration`).
- `QueryWorkbenchRunConfiguration` — `HostEndpoints` (host → list of route
  templates), `SelectedHost`, `Route`, `RunMode` (`QueryWorkbenchRunMode.Query`
  or `QueryProject`), `LeftPanePercent`, `RequestEditorViewMode`
  (`Lyo.Web.Components.JsonEditor.JsonEditorViewMode`), plus
  `CloneHostEndpoints(...)`.
- `QueryWorkbenchRunMode` — enum (`Query`, `QueryProject`) serialized with the
  bundled `QueryWorkbenchRunModeJsonConverter`.
- `QueryWorkbenchHostNormalization` — static normalization helpers used at
  load/persist time.

## Static assets

Browser scripts live under `wwwroot/scripts/` and are served through the standard
Razor class-library path `_content/Lyo.Query.Web.Components/`.

## Related projects

- [`Lyo.Query.Models`](../Lyo.Query.Models/README.md),
  [`Lyo.Query`](../Lyo.Query/README.md).
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) —
  JSON editor, filter widgets, client store.
- [`Lyo.Api.Models`](../../../Integration/Api/Lyo.Api.Models/README.md) — request /
  response shapes used by the runner panel.
