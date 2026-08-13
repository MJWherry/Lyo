# Lyo.Comic

Domain contracts for a **serialized fiction catalog**: series (**`ComicSeries`**, **`ComicAlternateTitle`**), hierarchical organization (**`ComicVolume`**, **`ComicChapter`**, **`ComicPage`**), cast (**`ComicCharacter`**), query DTO (**`ComicSeriesQuery`**), enums **`ComicType`/`ComicStatus`**, plus the persistence façade **`IComicStore`**.

## `IComicStore` responsibilities

- **Series** — **`SaveSeriesAsync`** upserts canonical series row + **`ComicAlternateTitle`** projections; **`GetSeriesByIdAsync`** / **`GetSeriesBySlugAsync`** hydrate alternates; **`SearchSeriesAsync`** receives **`ComicSeriesQuery`** filters; **`DeleteSeriesAsync`** cascades dependent graph per implementation.
- **Volumes / chapters / pages** — CRUD primitives with deterministic ordering assumptions documented on the interface (e.g. chapters ordered by number + language, pages ascending by page number within a chapter).
- **Characters** — attach cast members to series, list alphabetically (`GetCharactersBySeriesAsync`), delete standalone.

## `IComicStore` responsibilities — What is deliberately *not* here

- Authorization / tenancy — belong in ASP.NET policies or gateways.
- File/blob storage (`ComicPage` might reference binaries) — compose with **`Lyo.FileMetadataStore`** + **`Lyo.FileStorage`** in your app layer.
- Search relevance scoring beyond what **`ComicSeriesQuery`** expresses — push to Postgres full text or Elasticsearch outside this abstraction if needed.

## Layering map

| Assembly | Responsibility |
| ------------------------------- | -------------------------------------------------------------------------------------------------------- |
| **`Lyo.Comic`** *(this)* | POCOs + **`IComicStore`** (`netstandard2.0;net10.0`). |
| **`Lyo.Comic.Postgres`** | EF **`ComicDbContext`**, migrations, **`PostgresComicStore`**, DI extensions. |
| **`Lyo.Comic.Web.Components`** | Reusable Blazor browse/search/reader components (cards, grid/list layouts, MangaFire-style tap-to-page). |
| **`Apps/Comic/Lyo.Comic.Api*`** | Reference ASP.NET minimal API + client + DTO assemblies exposing this store over HTTP. |

## Testing strategy

Implement **`IComicStore`** as an in-memory double for unit tests validating slug uniqueness rules and cascading deletes without spinning up Postgres. The Postgres mapping is covered indirectly via host application integration tests; no dedicated `Lyo.Comic.Postgres.Tests` project ships in this repo today.

## See also

- [`Lyo.Comic.Postgres`](../Lyo.Comic.Postgres/README.md) — concrete `PostgresComicStore` + EF schema.
- [`Lyo.Comic.Web.Components`](../Lyo.Comic.Web.Components/README.md) — reusable Blazor browse/search/reader components built on this domain.
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md) — shared utilities used by enrichment flows (`Comic.Api` binds HTTP ↔ domain).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)