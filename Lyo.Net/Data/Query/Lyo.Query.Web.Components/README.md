# Lyo.Query.Web.Components

Reusable Blazor / MudBlazor components for editing and running [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) requests against any Lyo.Api host.

Razor SDK targeting `net10.0`. Requires `MudBlazor` 9.3+ and [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md) for the shared JSON editor, filter widgets, and client storage.

## Components

- `QueryBuilderWorkbench.razor` (`QueryBuilderWorkbench.razor.cs`) — the full-screen workbench that pairs a query editor with a runner panel. Holds the current `QueryConcreteReq` (`/QueryConcrete`), `ProjectionQueryReq` (`/QueryProject`), and `QueryReq` (root `/Query` From/Joins), plus shared bookkeeping (`Include`, `Select`, `Keys`). Auto-persists state to the browser via `ClientStore.GetQueryWorkbenchStateAsync` / `SetQueryWorkbenchStateAsync` with a 450 ms debounce; final state is also flushed on dispose.
- `QueryBuilderSettings.razor` (`QueryBuilderSettings.razor.cs`) — host / endpoint / run-mode selector embedded in the workbench.
- `QueryRunPanel.razor` (`QueryRunPanel.razor.cs`) — issues HTTP requests for the current query, renders the response in a JSON editor, and surfaces latency / size metrics. Loads its splitter behavior from `_content/Lyo.Query.Web.Components/scripts/queryWorkbenchSplitter.js`. Endpoint segments: `QueryConcrete`, `QueryProject`, or `Query` (root).

## State types

- `QueryWorkbenchPersistedState` — `EntityQuery` (`QueryConcreteReq?`), `QueryRequest` (`ProjectionQueryReq`), `RootQuery` (`QueryReq?`), `IncludeAll`, `SelectAll`, `KeysAll`, and `Run` (`QueryWorkbenchRunConfiguration`).
- `QueryWorkbenchRunConfiguration` — `HostEndpoints` (host → list of route templates), `SelectedHost`, `Route`, `RunMode` (`QueryWorkbenchRunMode`), `LeftPanePercent`, `RequestEditorViewMode` (`Lyo.Web.Components.JsonEditor.JsonEditorViewMode`), plus `CloneHostEndpoints(...)`.
- `QueryWorkbenchRunMode` — enum (`Query` → `/QueryConcrete`, `QueryProject`, `RootQuery` → `/Query`) serialized with the bundled `QueryWorkbenchRunModeJsonConverter`.
- `QueryWorkbenchHostNormalization` — static normalization helpers used at load/persist time.

## Static assets

Browser scripts live under `wwwroot/scripts/` and are served through the standard Razor class-library path `_content/Lyo.Query.Web.Components/`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Query.Models` — (direct, lyo)
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