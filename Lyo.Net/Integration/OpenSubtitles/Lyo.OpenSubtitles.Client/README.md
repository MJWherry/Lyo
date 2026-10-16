# Lyo.OpenSubtitles.Client

Typed HTTP client for the [OpenSubtitles](https://www.opensubtitles.com/) consumer REST API at `https://api.opensubtitles.com/api/v1/`.

**Archetype C (vendor client).** The host keeps subtitle files and any title mapping. This package does not store rows.

`OpenSubtitlesClient` subclasses `LyoHttpClient`. `Api-Key` and `ApplicationUserAgent` (`AppName vVersion`) are set once. Login returns a token and a bare hostname (`api.opensubtitles.com` or `vip-api.opensubtitles.com`). Download and user info send that token and call `https://{hostname}/api/v1/...`. The client does not download the temporary file link, so the API key is not sent to the file host.

## Features

- **Search.** `GET /subtitles`. An IMDb id may include a `tt` prefix; the query sends the numeric id. Languages go out as one comma-separated value.
- **Download.** `POST /download` with `file_id` from `data[].attributes.files[]`. Returns the temporary link and remaining quota.
- **User.** `GET /infos/user` on the login hostname with the bearer token.
- **Guessit.** `GET /utilities/guessit?filename=` on the consumer API.

## Examples

### Register

```csharp
services.AddOpenSubtitlesClientFromConfiguration(builder.Configuration);

services.AddOpenSubtitlesClient(o => {
    o.ApiKey = "your-api-key";
    o.ApplicationUserAgent = "MyApp v1.0";
});
```

### Search and download

```csharp
var session = await client.Auth.LoginAsync(user, password, ct);
var page = await client.Subtitles.SearchAsync(new SubtitleSearchQuery {
    ImdbId = "tt0133093",
    Languages = ["en"]
}, session, ct);
var fileId = page!.Data![0].Attributes!.Files![0].FileId;
var download = await client.Subtitles.DownloadAsync(fileId, session, ct);
// Fetch download.Link yourself. Do not send the API key to that host.
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Http.Client` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.RateLimit` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)