# Lyo.Comic.Api

ASP.NET Core **Minimal API** composition for the comic domain: **series, volumes, chapters, pages, characters**, plus **tags, ratings, comments, favorites**, **binary file upload/download** with optional **compression and envelope encryption**, and **enriched reads** that batch-load related data through **`Lyo.Api`** query services.

## Responsibilities

| Area | What this project does |
|------|-------------------------|
| **DI** | **`AddComicApi`** wires Postgres stores, **`ComicEnrichmentService`**, local cache, **`AddLyoQueryServices`** / CRUD services per DbContext, **`ComicLyoMapper`**, and a **keyed** comic file storage pipeline. |
| **Routes** | **`MapComicApi`** registers enriched route groups under a prefix (default **`/api/comic`**) and **`MapFilesEndpoints`** at **`/files`**. |
| **CRUD + query** | **`BuildComicApiEndpoints`** registers standard **`Lyo.Api`** endpoint builders for each entity; series/volume/chapter **plain GET** is omitted where an enriched GET exists. |
| **Files** | GUID-addressed storage via **`IFileStorageService`** keyed as **`comic-files`**: metadata in Postgres, bytes on disk, optional **two-key** encryption. |

## Registration

```csharp
using Lyo.Comic.Api;

builder.Services.AddComicApi(builder.Configuration);

var app = builder.Build();
app.MapComicApi();              // default prefix /api/comic + /files
app.BuildComicApiEndpoints();   // Lyo.Api CRUD + Query for comic entities
```

Call **`AddComicApi`** before building the host. Map routes after **`app.Build()`** (or equivalent) alongside the rest of your pipeline.

## Configuration

### Postgres

Uses the same configuration patterns as other Lyo Postgres apps: **`AddPostgresComicStoreFromConfiguration`**, tag/comment/rating/favorite stores, and **`AddFileMetadataStoreDbContextFactoryFromConfiguration`**. See each package’s README for connection keys and migrations.

### Comic file storage (`AddComicFileStorage`)

| Key | Purpose |
|-----|---------|
| **`ComicFileStorage:RootDirectoryPath`** | Root directory for **`LocalFileStorageService`** (default **`./comic-files`**). |
| **`ComicFileEncryption:Encrypt`** | When true, files are stored through the **keyed** two-key encryption stack (default **true**). |
| **`ComicFileEncryption:Compress`** | Passed to **`SaveFromStreamAsync`** (default **false**). |
| **`ComicFileEncryption:KeyId`** | Logical key id in **`LocalKeyStore`** (default **`comic-images`**). |
| **`ComicFileEncryption:KeySecret`** | Passphrase material for **`AddKeyFromString`** (default **`change-me-in-production`** — **override in production**). |

The keyed **`IKeyStore`** / **`LocalKeyStore`** / encryption services isolate comic file crypto from any other keyed encryption in the same container (**`FileStorageKey`** = **`"comic-files"`**).

### Upload policy

**`ComicFileUploadOptions`** (**`Encrypt`**, **`Compress`**, **`KeyId`**) is registered as a **keyed singleton** so **`UploadFile`** reads the same flags the API was configured with.

## HTTP surface (overview)

- **`/api/comic/...`** — **`MapSeriesEndpoints`**, **`MapVolumeEndpoints`**, **`MapChapterEndpoints`** provide enriched GET/list flows; CRUD/query paths from **`BuildComicApiEndpoints`** follow **`{prefix}/series`**, **`.../volumes`**, **`.../chapters`**, **`.../pages`**, **`.../characters`**.
- **`/files`** — **`GET /{id}`** (bytes + content type), **`POST /batch`** (base64 payloads per id), **`POST /upload`** (**`IFormFile`**, optional query **`seriesId`**, **`volumeId`**, **`chapterId`** — omit **`Guid.Empty`** / omit params you do not use). Scope selects the path prefix: **`chapterId`** (full prefix **`{seriesId}/{volumeId-or-nil}/{chapterId}`**), else **`volumeId`** (**`{seriesId}/{volumeId}`**), else **`seriesId`** (**`{seriesId}`**). Optional ids are validated against loaded entities. Omit all three → default local file-storage shard layout. **`DELETE /{id}`**.

## Enrichment (`ComicEnrichmentService`)

Aggregates **tags, ratings, comments, favorites** (and related counts) for comic aggregates without N+1 queries: uses **`IQueryService<TContext>`** with **`QueryReqBuilder`** so list projections stay compatible with **`Lyo.Api`** query caching, and **chunked IN clauses** (size **400**) for large id sets. Favorite totals use store-level aggregation where available.

## Related projects

- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md)
- [`Lyo.Cache`](../../../Core/Cache/Lyo.Cache/README.md)
- [`Lyo.Comment.Postgres`](../../../Features/Comment/Lyo.Comment.Postgres/README.md)
- [`Lyo.Favorite.Postgres`](../../../Features/Favorite/Lyo.Favorite.Postgres/README.md)
- [`Lyo.FileStorage`](../../../Data/FileStorage/Lyo.FileStorage/README.md)
- [`Lyo.Rating.Postgres`](../../../Features/Rating/Lyo.Rating.Postgres/README.md)
- [`Lyo.Tag.Postgres`](../../../Features/Tag/Lyo.Tag.Postgres/README.md)
- [`Lyo.Api.Models`](../../../Integration/Api/Lyo.Api.Models/README.md)
- [`Lyo.Comic.Api.Models`](../Lyo.Comic.Api.Models/README.md)
- [`Lyo.Comic.Postgres`](../../../Features/Comic/Lyo.Comic.Postgres/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.FileMetadataStore.Postgres`](../../../Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md)
- [`Lyo.Keystore`](../../../Security/Encryption/Lyo.Keystore/README.md)
