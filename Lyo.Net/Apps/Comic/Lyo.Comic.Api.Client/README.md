# Lyo.Comic.Api.Client

Typed **`HttpClient`** for the [`Lyo.Comic.Api`](../Lyo.Comic.Api/README.md) service. Wraps the upload / download / batch / tag endpoints behind **`IComicApiClient`** and a single
**`AddComicApiClientFromConfiguration`** DI extension that binds **`ComicApiClientOptions`** from configuration.

## Features

- Binds `ComicApiClientOptions` from the section named by `configSectionName` (defaults to **`"ComicApi"`**, see `ComicApiClientOptions.SectionName`). The section is optional — if it does not exist the defaults from `ApiClientOptions` are used.
- Registers `IOptions<ComicApiClientOptions>` and the resolved options instance as singletons.
- Adds a singleton **`JsonSerializerOptions`** built by **`LyoJsonSerializerOptions.Create()`** (only when not already registered).
- Registers the typed client via `services.AddHttpClient<IComicApiClient, ComicApiClient>()`:
- Sets `HttpClient.BaseAddress = "{BaseUrl}/"` when `BaseUrl` is non-empty.
- Appends every supported entry from `AcceptEncodings` to `Accept-Encoding` (case-insensitive de-dup; only `gzip` / `deflate` on `netstandard2.0`, plus `br` on modern targets).
- Configures the primary `HttpClientHandler` with `AutomaticDecompression` flags derived from `AcceptEncodings` when `EnableAutoResponseDecompression == true`.

## Examples

### Register services

```csharp
using Lyo.Comic.Api.Client;

builder.Services.AddComicApiClientFromConfiguration(builder.Configuration);
// Optional: pass a non-default config section name as the third argument.
```

### Configuration example

```json
{
  "ComicApi": {
    "BaseUrl": "https://comics.example.com",
    "AcceptEncodings": ["gzip", "br"],
    "EnableAutoResponseDecompression": true
  }
}
```

## Registration

- Binds `ComicApiClientOptions` from the section named by `configSectionName` (defaults to **`"ComicApi"`**, see `ComicApiClientOptions.SectionName`). The section is optional — if it does not exist the defaults from `ApiClientOptions` are used.
- Registers `IOptions<ComicApiClientOptions>` and the resolved options instance as singletons.
- Adds a singleton **`JsonSerializerOptions`** built by **`LyoJsonSerializerOptions.Create()`** (only when not already registered).
- Registers the typed client via `services.AddHttpClient<IComicApiClient, ComicApiClient>()`:
- Sets `HttpClient.BaseAddress = "{BaseUrl}/"` when `BaseUrl` is non-empty.
- Appends every supported entry from `AcceptEncodings` to `Accept-Encoding` (case-insensitive de-dup; only `gzip` / `deflate` on `netstandard2.0`, plus `br` on modern targets).
- Configures the primary `HttpClientHandler` with `AutomaticDecompression` flags derived from `AcceptEncodings` when `EnableAutoResponseDecompression == true`.

## `ComicApiClientOptions`

Defined in [`ComicApiClientOptions.cs`](./ComicApiClientOptions.cs). Extends `Lyo.Api.Client.ApiClientOptions` and overrides the default configuration section name:

| Property | Source | Default | Purpose |
| ------------------------------------- | --------------------------------------- | --------------------------- | -------------------------------------------------------------------------------------------------- |
| **`SectionName`** (`const`) | `ComicApiClientOptions` (`new` keyword) | **`"ComicApi"`** | Default IConfiguration section used by `AddComicApiClientFromConfiguration`. |
| **`BaseUrl`** | `ApiClientOptions` | `null` | Base URL of the Comic API host (e.g. `https://comics.example.com`). Trimmed and suffixed with `/`. |
| **`AcceptEncodings`** | `ApiClientOptions` | `["gzip", "deflate", "br"]` | Encodings advertised in `Accept-Encoding` and used to seed `AutomaticDecompression`. |
| **`EnableAutoResponseDecompression`** | `ApiClientOptions` | `true` | When false, the primary `HttpClientHandler` is left at framework defaults. |

Additional properties on `ApiClientOptions` (auth tokens, timeouts, etc.) are inherited as-is.

## `IComicApiClient`

`IComicApiClient` extends **`Lyo.Api.Client.IApiClient`** (so callers get the generic CRUD / Query helpers exposed by `ApiClient`) and adds the comic-specific surface below. The concrete `ComicApiClient` resolves paths relative to the configured `BaseAddress`.

## `IComicApiClient` — Files

| Member | HTTP | Returns | Notes |
| ------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------- | ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `GetFileAsync(Guid id, CancellationToken)` | `GET files/{id}` | `byte[]` | Calls `HttpClient.GetByteArrayAsync` directly. No JSON deserialization. |
| `GetFilesBatchAsync(IReadOnlyList<Guid> ids, CancellationToken)` | `POST files/batch` | `IReadOnlyList<FileBatchEntry>` | Body: `FilesBatchReq`. Each entry carries the file's base64 `Data` + `ContentType`. Missing IDs are silently omitted server-side. |
| `UploadFileAsync(Stream data, string fileName, Guid? seriesId, Guid? volumeId, Guid? chapterId, CancellationToken)` | `POST files/upload?seriesId&volumeId&chapterId` | `FileStoreResult?` | Sends multipart `IFormFile`. Any `Guid?` argument that is `null` or `Guid.Empty` is omitted from the query string. |
| `DeleteFileAsync(Guid id, CancellationToken)` | `DELETE files/{id}` | `bool` | Returns `true` on 200, `false` on 404 (see API contract). |
| `GetFileUrl(Guid id)` | — | `string` | Builds `"{BaseUrl.TrimEnd('/')}/files/{id}"`. Suitable as an `<img src>`; does **not** include any auth header. |

## `IComicApiClient` — Series tags

| Member | HTTP |
| -------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `GetAllSeriesTagsAsync(CancellationToken)` | `GET api/comic/series/tags` |
| `GetSeriesTagsAsync(Guid seriesId, CancellationToken)` | `GET api/comic/series/{seriesId}/tags` |
| `AddSeriesTagAsync(Guid, string tag, string tagType = "tag", string? slug = null, CancellationToken)` | `POST api/comic/series/{seriesId}/tags` (`AddTagReq`) |
| `RemoveSeriesTagAsync(Guid, string tag, string tagType = "tag", string? slug = null, CancellationToken)` | `DELETE api/comic/series/{seriesId}/tags/{tag}?tagType=&slug=` |

## `IComicApiClient` — Volume tags

| Member | HTTP |
| ---------------------- | --------------------------------------------------------------- |
| `GetVolumeTagsAsync` | `GET api/comic/volumes/{volumeId}/tags` |
| `AddVolumeTagAsync` | `POST api/comic/volumes/{volumeId}/tags` |
| `RemoveVolumeTagAsync` | `DELETE api/comic/volumes/{volumeId}/tags/{tag}?tagType=&slug=` |

## `IComicApiClient` — Chapter tags

| Member | HTTP |
| ----------------------- | ----------------------------------------------------------------- |
| `GetChapterTagsAsync` | `GET api/comic/chapters/{chapterId}/tags` |
| `AddChapterTagAsync` | `POST api/comic/chapters/{chapterId}/tags` |
| `RemoveChapterTagAsync` | `DELETE api/comic/chapters/{chapterId}/tags/{tag}?tagType=&slug=` |

## `IComicApiClient` — Inherited from `IApiClient`

CRUD, `POST /QueryConcrete`, bulk, upsert, history, and export helpers from `Lyo.Api.Client.ApiClient` are available on the same instance — see [`Lyo.Api.Client`](../../../Integration/Api/Lyo.Api.Client/README.md). They are the canonical way to call the `BuildComicApiEndpoints` routes for series / volumes / chapters / pages / characters (ratings / comments / favorites do **not** yet have dedicated members on `IComicApiClient`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Comic.Api.Models` — (direct, lyo)
- `Lyo.FileMetadataStore` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (direct, microsoft)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Comic` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)