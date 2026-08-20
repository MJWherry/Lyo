# Lyo.Comic

Domain contracts for a serialized fiction catalog: series (`ComicSeries`, `ComicAlternateTitle`), hierarchy (`ComicVolume`, `ComicChapter`, `ComicPage`), cast (`ComicCharacter`), query DTO (`ComicSeriesQuery`), enums `ComicType`/`ComicStatus`, and the persistence facade `IComicStore`.

## `IComicStore` responsibilities

- **Series.** `SaveSeriesAsync` upserts canonical series row + `ComicAlternateTitle` projections; `GetSeriesByIdAsync` / `GetSeriesBySlugAsync` hydrate alternates; `SearchSeriesAsync` receives `ComicSeriesQuery` filters; `DeleteSeriesAsync` cascades dependent graph per implementation.
- **Volumes / chapters / pages.** CRUD primitives with deterministic ordering assumptions documented on the interface (e.g. chapters ordered by number + language, pages ascending by page number within a chapter).
- **Characters.** Attach cast members to series, list alphabetically (`GetCharactersBySeriesAsync`), delete standalone.

## What is deliberately not here

- Authorization / tenancy. Belong in ASP.NET policies or gateways.
- File/blob storage (`ComicPage` might reference binaries). Compose with `Lyo.FileMetadataStore` + `Lyo.FileStorage` in your app layer.
- Search relevance scoring beyond what `ComicSeriesQuery` expresses. Push to Postgres full text or Elasticsearch outside this abstraction if needed.

## Layering map

| Assembly | Responsibility |
| --------------------------- | -------------------------------------------------------------------------------------------------------- |
| `Lyo.Comic` (this) | POCOs + `IComicStore` (`netstandard2.0;net10.0`). |
| `Lyo.Comic.Postgres` | EF `ComicDbContext`, migrations, `PostgresComicStore`, DI extensions. |
| `Lyo.Comic.Web.Components` | Reusable Blazor browse/search/reader components (cards, grid/list layouts, MangaFire-style tap-to-page). |
| `Lyo-Comic` (separate repo) | ASP.NET minimal API + client + DTO assemblies that consume this store over HTTP. |

## Testing strategy

Implement `IComicStore` as an in-memory double for unit tests validating slug uniqueness rules and cascading deletes without spinning up Postgres. The Postgres mapping is covered indirectly via host application integration tests; no dedicated `Lyo.Comic.Postgres.Tests` project ships in this repo today.

## See also

- [`Lyo.Comic.Postgres`](../Lyo.Comic.Postgres/README.md). Concrete `PostgresComicStore` + EF schema.
- [`Lyo.Comic.Web.Components`](../Lyo.Comic.Web.Components/README.md). Reusable Blazor browse/search/reader components built on this domain.
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md). Shared utilities used by enrichment flows (`Comic.Api` binds HTTP ↔ domain).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)