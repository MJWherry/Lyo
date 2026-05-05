# Lyo.Comic

Domain contracts for a **serialized fiction catalog**: series (**`ComicSeries`**, **`ComicAlternateTitle`**), hierarchical organization (**`ComicVolume`**, **`ComicChapter`**, **`ComicPage`**), cast (**`ComicCharacter`**), query DTO (**`ComicSeriesQuery`**), enums **`ComicType`/`ComicStatus`**, plus the persistence façade **`IComicStore`**.

## `IComicStore` responsibilities

Everything is expressed as explicit async methods (guid ids, slug lookup for public URLs):

1. **Series** — **`SaveSeriesAsync`** upserts canonical series row + **`ComicAlternateTitle`** projections; **`GetSeriesByIdAsync`** / **`GetSeriesBySlugAsync`** hydrate alternates; **`SearchSeriesAsync`** receives **`ComicSeriesQuery`** filters; **`DeleteSeriesAsync`** cascades dependent graph per implementation.
2. **Volumes / chapters / pages** — CRUD primitives with deterministic ordering assumptions documented on the interface (e.g. chapters ordered by number + language, pages ascending by page number within a chapter).
3. **Characters** — attach cast members to series, list alphabetically (`GetCharactersBySeriesAsync`), delete standalone.

Consumers (HTTP APIs, importers, background enrichment) orchestrate transactional boundaries **above** the store— **`IComicStore`** intentionally maps 1:1 to persistence operations so tests can substitute in-memory mocks.

### What is deliberately *not* here

- Authorization / tenancy — belong in ASP.NET policies or gateways.
- File/blob storage (`ComicPage` might reference binaries) — compose with **`Lyo.FileMetadataStore`** + **`Lyo.FileStorage`** in your app layer.
- Search relevance scoring beyond what **`ComicSeriesQuery`** expresses — push to Postgres full text or Elasticsearch outside this abstraction if needed.

## Layering map

| Assembly | Responsibility |
|----------|----------------|
| **`Lyo.Comic`** *(this)* | POCOs + **`IComicStore`**. |
| **`Lyo.Comic.Postgres`** | EF **`ComicDbContext`**, migrations, **`PostgresComicStore`**, DI extensions. |
| **`Lyo.Comic.Web.Components`** | Blazor grid/detail experiences. |
| **`Apps/Comic/Lyo.Comic.Api*`** | Reference ASP.NET minimal API exposing Lyo mapper + enrichment services. |

## Testing strategy

Implement **`IComicStore`** as an in-memory double for unit tests validating slug uniqueness rules, cascading deletes—without spinning Postgres—then rely on **`Lyo.Comic.Postgres.Tests`** integration for mapping correctness.

## See also

- [`Lyo.Comic.Postgres`](../Comic.Postgres/README.md) — concrete store + EF schema.
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md) — shared identifiers such as **`EntityRef`** reused in enrichment flows (`Comic.Api` binds HTTP ↔ domain).
