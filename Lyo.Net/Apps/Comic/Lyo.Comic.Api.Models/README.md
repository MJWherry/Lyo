# Lyo.Comic.Api.Models

Request and response DTOs shared between [`Lyo.Comic.Api`](../Lyo.Comic.Api/README.md) and [`Lyo.Comic.Api.Client`](../Lyo.Comic.Api.Client/README.md). Targets
**`netstandard2.0`** and **`net10.0`** so the same contracts can be referenced from any host (server, Blazor, MAUI, console).

The package brings in the comic domain types via [`Lyo.Comic`](../../../Features/Comic/Lyo.Comic/README.md) (for `ComicType` / `ComicStatus` enums and `ComicSeriesQuery`) and
[`Lyo.Api.Client`](../../../Integration/Api/Lyo.Api.Client/README.md) (for the generic CRUD / Query envelopes inherited via `IComicApiClient`).

## Layout

```text
Request/
├── ComicSeriesReq.cs         (also: ComicAlternateTitleReq)
├── ComicVolumeReq.cs
├── ComicChapterReq.cs
├── ComicCharacterReq.cs
├── ComicPageReq.cs
└── CrossDomainReqs.cs        (AddTagReq, AddRatingReq, AddCommentReq, AddFavoriteReq, RemoveFavoriteReq)

Response/
├── ComicSeriesRes.cs         (also: ComicAlternateTitleRes)
├── ComicVolumeRes.cs
├── ComicChapterRes.cs
├── ComicCharacterRes.cs
├── ComicPageRes.cs
└── ComicFileBatchRes.cs      (FilesBatchReq, FileBatchEntry)
```

> **Note:** there is no `Search/` directory. The search filter used by `POST /api/comic/series/search` is **`Lyo.Comic.ComicSeriesQuery`**, defined in
> [`Lyo.Comic`](../../../Features/Comic/Lyo.Comic/README.md) and re-used by both the API and the client.

## DTO categories

### `Request/` — write models

| Category                   | Types                                                                                                                   | Used by                                                                                                                                              |
|----------------------------|-------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Entity create / update** | `ComicSeriesReq` (+ `ComicAlternateTitleReq`), `ComicVolumeReq`, `ComicChapterReq`, `ComicCharacterReq`, `ComicPageReq` | `BuildComicApiEndpoints` Create / Update / Upsert routes (`POST /api/comic/{entity}[/Update\|/Upsert]`).                                             |
| **Series-only sugar**      | `ComicSeriesReq.Tags : IReadOnlyList<AddTagReq>?`                                                                       | Applied **only** on initial create / upsert-create; ignored on update / upsert-update paths.                                                         |
| **Cross-domain writes**    | `AddTagReq`, `AddRatingReq`, `AddCommentReq`, `AddFavoriteReq`, `RemoveFavoriteReq`                                     | `series/volumes/chapters/{id}/{tags,ratings,comments,favorites}` POST/DELETE handlers in `SeriesEndpoints` / `VolumeEndpoints` / `ChapterEndpoints`. |

`AddTagReq` carries `Name`, `TagType` (defaults to `"tag"`), and an optional `Slug`. `AddRatingReq`, `AddCommentReq`, `AddFavoriteReq`, and `RemoveFavoriteReq` all carry
**`FromEntityType` / `FromEntityId`** so the API can record who made the assertion — the server **never trusts** an authenticated identity here (see the *Limitations* section of
the Comic.Api README).

### `Response/` — enriched read models

| Category                 | Types                                                                                                | Notes                                                                                                                                                                     |
|--------------------------|------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Enriched entities**    | `ComicSeriesRes`, `ComicVolumeRes`, `ComicChapterRes`                                                | Combine the comic domain fields with cross-domain counts: `Tags`, `AverageRating`, `RatingCount`, `CommentCount`, `FavoriteCount`, `IsFavorited` (nullable).              |
| **Cover / image URLs**   | `CoverImageUrl`, `ImageUrl` (computed `init`-only getters)                                           | Resolve to `/files/{guid}` when the underlying `*Ref` is a parseable GUID; otherwise `null`. **Relative paths** — the client prefixes them with the configured `BaseUrl`. |
| **Plain entities**       | `ComicCharacterRes`, `ComicPageRes`                                                                  | No async enrichment; mirrors the domain fields plus computed image URL.                                                                                                   |
| **Alternate titles**     | `ComicAlternateTitleRes` (nested in `ComicSeriesRes.AlternateTitles`)                                | Mirrors `ComicAlternateTitleReq`, plus an `Id`.                                                                                                                           |
| **File batch contracts** | `FilesBatchReq(IReadOnlyList<Guid> Ids)`, `FileBatchEntry(Guid Id, string ContentType, string Data)` | Body and response for `POST /files/batch`. `Data` is **base64-encoded** file bytes; missing IDs are dropped server-side.                                                  |

`IsFavorited` is intentionally **nullable** — it is `null` when the caller is anonymous **or** when the server-side endpoint omits a caller reference. The current
`Lyo.Comic.Api` endpoints always pass `callerRef: null` to `ComicEnrichmentService`, so today `IsFavorited` is always `null` in practice. See the *Limitations* section of the
[`Lyo.Comic.Api` README](../Lyo.Comic.Api/README.md) for how a host can fix that.

## Targeting

Both `Request/*Req` and `Response/*Res` types are sealed records / sealed classes, declared in `Lyo.Comic.Api.Models.Request` / `Lyo.Comic.Api.Models.Response`. They are
serialization-friendly (parameterless ctors or positional records, public `init` / setters) and contain no business logic beyond the URL-deriving `CoverImageUrl` / `ImageUrl`
computed properties.

## Related projects

- [`Lyo.Api.Client`](../../../Integration/Api/Lyo.Api.Client/README.md)
- [`Lyo.Comic`](../../../Features/Comic/Lyo.Comic/README.md)
- [`Lyo.Comic.Api`](../Lyo.Comic.Api/README.md)
- [`Lyo.Comic.Api.Client`](../Lyo.Comic.Api.Client/README.md)
