# Lyo.Story

Domain contracts for a quest-style play graph. The system of record is nodes, choices, named actions, and trigger/condition/response reactions. Presentation (text, cutscene, or none) is optional on a node. Scenes are optional groups, not the play cursor. Conditions persist as Lyo.Query `WhereClause` trees (`var.*` / `chose.*` / `visited.*` field prefixes) and are evaluated by `StoryPlayEngine` against the play bag — not `WhereClauseEvaluator`. Consequence tracking is the play log (`CausedByEventId`), not an authoring table. Persistence facade is `IStoryStore`.

## Features

- **Play graph.** `Story` with nodes, optional scenes, declared variables, named actions, choices, and ambient reactions.
- **Builders.** `StoryBuilder` with `WhenAll` / `WhenOneOf` and `.Do` response lists. `StorySamples.TheHeist()` is the canonical sample.
- **Engine.** `StoryPlayEngine` is pure: start, available moves, take choice, advance, get causes. No EF.
- **Query conditions.** Reuses `WhereClause` / `GroupClause` (And = All, Or = OneOf). Story sugar (`Flag`, `Chose`, `Visited`) emits those nodes.
- **Tenancy.** `Story.TenantId` and `Story.GameKey` scope a graph to a tenant and a game. `IStoryStore.ListStoriesAsync(tenantId, gameKey)` lists headers ordered by title; a null tenant lists only stories with no tenant. Slugs stay globally unique.

## Examples

### Author a graph

```csharp
var story = StoryBuilder.New("The Heist")
    .Slug("the-heist")
    .Variable("alarm", StoryVariableType.Boolean, false)
    .StartNode("vault_door", n => n
        .Choice("smash", c => c.Do(d => d.Set("alarm", true).GoesTo("guards"))))
    .Node("guards", n => n.Ending())
    .Build();
```

## What is deliberately not here

- Authorization / tenancy.
- Playing cutscene media. Store `MediaRef` and compose with FileStorage.
- Favorites, tags, ratings. Attach via `EntityRef` and `StoryEntityTypes` at the host.
- Characters, inventory, locations.

## Layering map

| Assembly | Responsibility |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `Lyo.Story` (this) | POCOs, builders, `StoryPlayEngine`, `IStoryStore` (`netstandard2.0;net10.0`). Text MIME defaults use `MimeTypeInfo`. |
| `Lyo.Story.Models` | Req/Res pairs and route constants. No `StoryQuery`. |
| `Lyo.Story.Postgres` | EF `StoryDbContext` schema `story`, `PostgresStoryStore`, `StoryLyoMapper`, `AddLyoCrudServices`. |
| `Lyo.Story.Api` | `AddStoryApi` + `BuildStoryGroup` (Req/Res CRUD, Query, graph, play). |
| `Lyo.Story.Client` | Typed HTTP client for graph and play. Query stays on `IApiClient`. |
| `Lyo.Story.Web.Components` | Blazor story builder: library, computed graph, editors, and play. The host registers `IStoryClient`. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)