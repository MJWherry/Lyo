# Lyo.Trakt.Client

Typed HTTP client for [Trakt](https://trakt.tv/) catalog search and OAuth.

**Archetype C (vendor client).** Catalog calls use `https://api.trakt.tv` with `trakt-api-version: 2` and `trakt-api-key`. OAuth calls use `https://auth.trakt.tv`. The client
returns access and refresh tokens and does not store a user bearer. Refresh tokens are single-use.

Device-token polling returns pending, expired, denied, and slow-down as `TraktDevicePoll.Status`. List calls return `TraktPage<T>` from the `X-Pagination-*` headers. Trending and
search rows are wrappers. `GET /shows/{id}/seasons/{season}` returns episodes. Show status stays the API string, including `returning series`.

## Features

- **Search.** `GET /search/movie` and `GET /search/{idType}/{id}`. IMDb lookup keeps the `tt` prefix.
- **Movies.** `GET /movies/{id}` and `GET /movies/trending` (`watchers` plus `movie`).
- **Seasons.** `GET /shows/{id}/seasons/{season}` returns an episode array.
- **OAuth.** Authorization-code exchange and device-token polling on `https://auth.trakt.tv`.

## Examples

### Register

```csharp
services.AddTraktClientFromConfiguration(builder.Configuration);

services.AddTraktClient(o => {
    o.ClientId = "your-client-id";
    o.ClientSecret = "your-client-secret";
    o.RedirectUri = "https://app.example/callback";
});
```

### Search and exchange a code

```csharp
var page = await client.Search.SearchMoviesAsync("weapons", ct);
var movie = page.Items[0].Movie;
var token = await client.Auth.ExchangeCodeAsync(code, ct);
// Store token.RefreshToken. The next refresh must send that value.
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