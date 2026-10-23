# Lyo.Story.Web.Components

Blazor / MudBlazor story builder. Drop `StoryManagement` on a host page to list graphs for a tenant and optional game, then author the open graph. The graph is a tested layout
drawn as SVG: boxes are 220 by 72 with a 96 column gap and a 20 row gap. Choice arrows follow the last `gotoNode` effect. The host must already register `IStoryClient`. This
package has no `Add*` registration.

## Features

- **Library.** `StoryManagement` lists graph headers, then creates, opens, duplicates, and deletes.
- **Workbench.** `StoryBuilderWorkbench` has Visual, Request JSON, and Play tabs. Save stays disabled while checks fail. The open graph autosaves in `ClientStore`.
- **Graph.** `StoryGraphView` draws the layout from `StoryGraphLayout` through `Lyo.Diagram`. Click a node to select it.
- **Editors.** Node, choice, effect list, variable, action, and reaction editors. Conditions use the where-clause builder with `var.*`, `chose.*`, and `visited.*` fields.

## Examples

### Host page

```razor
@using Lyo.Story.Web.Components

<StoryManagement StoryClient="StoryClient" BaseRoute="Story" TenantId="tenantId" GameKey="heist" />
```

## Parameters

| Parameter     | Notes                                                                  |
|---------------|------------------------------------------------------------------------|
| `StoryClient` | `IStoryClient`. Required. The client already carries its route prefix. |
| `BaseRoute`   | Route prefix shown to the host. Defaults to `"Story"`.                 |
| `TenantId`    | Tenant whose stories are listed. Null lists stories with no tenant.    |
| `GameKey`     | Optional game filter.                                                  |

## Component catalog

| Component               | Role                                                                                             |
|-------------------------|--------------------------------------------------------------------------------------------------|
| `StoryManagement`       | Library of graph headers. Create, open, duplicate, delete. Opens the workbench.                  |
| `StoryBuilderWorkbench` | Tabs: Visual, Request JSON, Play.                                                                |
| `StoryGraphView`        | SVG of the computed layout. Click a node to select it.                                           |
| `StoryNodeEditor`       | Key, title, scene, presentation (none / text / cutscene), body, media, start, ending.            |
| `StoryChoiceEditor`     | Prompt, order, `visibleWhen` and `enabledWhen`.                                                  |
| `StoryEffectListEditor` | Set, unset, increment, goto, end playthrough, invoke action. Reorder.                            |
| `StoryVariableEditor`   | Boolean, integer, string, default.                                                               |
| `StoryActionEditor`     | Named action and its effect list.                                                                |
| `StoryReactionEditor`   | Trigger, scope node or variable, once, when, effects.                                            |
| `StoryPlayView`         | Start, current node, enabled choices, Advance, variable bag, event log, causes for one variable. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Diagram` (direct, lyo)
- `Lyo.Story` (direct, lyo)
- `Lyo.Story.Client` (direct, lyo)
- `Lyo.Story.Models` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagram.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
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
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)