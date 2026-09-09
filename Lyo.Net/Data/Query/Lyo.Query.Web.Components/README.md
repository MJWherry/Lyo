# Lyo.Query.Web.Components

Blazor / MudBlazor UI that edits and runs [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) requests against any Lyo.Api host.

Razor SDK targeting `net10.0`. Needs `MudBlazor` 9.3+ and [`Lyo.Web.Components`](../../../Apps/Web/Lyo.Web.Components/README.md) for the shared JSON editor, filter widgets, and client storage.

## Components

- `QueryBuilderWorkbench.razor` (`QueryBuilderWorkbench.razor.cs`). Full-screen workbench that pairs a query editor with a runner panel. Holds the current `QueryConcreteReq` (`/QueryConcrete`), `ProjectionQueryReq` (`/QueryProject`), and `QueryReq` (root `/Query` From/Joins), plus shared bookkeeping (`Include`, `Select`, `Keys`). Persists state to the browser via `ClientStore.GetQueryWorkbenchStateAsync` / `SetQueryWorkbenchStateAsync` with a 450 ms debounce. Dispose also flushes the final state.
- `QueryBuilderSettings.razor` (`QueryBuilderSettings.razor.cs`). Host / endpoint / run-mode selector inside the workbench.
- `QueryRunPanel.razor` (`QueryRunPanel.razor.cs`). Sends HTTP for the current query, shows the response in a JSON editor, and reports latency / size. Optional auth header on the runner: none, `Authorization: Bearer {token}`, or a custom name/value. Splitter behavior comes from `_content/Lyo.Query.Web.Components/scripts/queryWorkbenchSplitter.js`. Endpoint segments: `QueryConcrete`, `QueryProject`, or `Query` (root).

## State types

- `QueryWorkbenchPersistedState`. `EntityQuery` (`QueryConcreteReq?`), `QueryRequest` (`ProjectionQueryReq`), `RootQuery` (`QueryReq?`), `IncludeAll`, `SelectAll`, `KeysAll`, and `Run` (`QueryWorkbenchRunConfiguration`).
- `QueryWorkbenchRunConfiguration`. `HostEndpoints` (host → list of route templates), `SelectedHost`, `Route`, `RunMode` (`QueryWorkbenchRunMode`), `LeftPanePercent`, `RequestEditorViewMode` (`Lyo.Web.Components.JsonEditor.JsonEditorViewMode`), `AuthMode` (`QueryWorkbenchAuthMode`: None / Bearer / Header), `AuthHeaderName`, `AuthHeaderValue` (kept in browser state), plus `CloneHostEndpoints(...)`.
- `QueryWorkbenchRunMode`. Enum (`Query` → `/QueryConcrete`, `QueryProject`, `RootQuery` → `/Query`) serialized with the bundled `QueryWorkbenchRunModeJsonConverter`.
- `QueryWorkbenchHostNormalization`. Static helpers that normalize at load/persist time.

## Browser scripts

Scripts sit under `wwwroot/scripts/` and are served on the usual Razor class-library path `_content/Lyo.Query.Web.Components/`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
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