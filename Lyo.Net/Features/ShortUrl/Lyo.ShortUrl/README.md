# Lyo.ShortUrl

Core library for URL shortening with support for multiple providers and custom implementations.

## Features

- **Provider-agnostic** – Implement `IShortUrlService` for any URL shortener (Bitly, TinyURL, custom)
- **Fluent builders** – `UrlShortenBuilder` for constructing shorten requests with validation
- **Expiration support** – Optional expiration dates for shortened URLs
- **Metrics & logging** – Built-in integration with Lyo.Metrics and ILogger
- **Dependency injection** – First-class DI support

## Quick Start

```csharp
using Lyo.ShortUrl;
using Lyo.ShortUrl.Models;

// Register implementation (e.g. Bitly, custom)
services.AddSingleton<IShortUrlService, MyShortUrlService>();

// Shorten a URL
var builder = UrlShortenBuilder.New()
    .SetLongUrl("https://example.com/long-url")
    .SetCustomAlias("my-alias")  // optional
    .SetExpirationDate(DateTime.UtcNow.AddDays(30));

var result = await shortUrlService.ShortenAsync(builder, cancellationToken);

// Expand, stats, update, delete, test connection
var expanded = await shortUrlService.ExpandAsync(shortUrl, ct);
var stats = await shortUrlService.GetStatisticsAsync(shortUrl, ct);
var updated = await shortUrlService.UpdateAsync(shortUrl, newLongUrl, ct);
var deleted = await shortUrlService.DeleteAsync(shortUrl, ct);
var connected = await shortUrlService.TestConnectionAsync(ct);
```

## API

- `ShortenAsync(longUrl | builder)` – Shorten a URL (optional custom alias, expiration)
- `ExpandAsync(shortUrl)` – Expand short URL to original
- `GetStatisticsAsync(shortUrl)` – Get clicks, creation date, etc.
- `UpdateAsync(shortUrl, newLongUrl)` – Update destination URL
- `DeleteAsync(shortUrl)` – Delete a short URL
- `TestConnectionAsync()` – Test connection to the shortener provider

### UrlShortenBuilder

- `SetLongUrl(url)` – Required long URL
- `SetCustomAlias(alias)` – Optional custom slug
- `SetExpirationDate(date)` – Optional expiration

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.ShortUrl.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Metrics`

## Public API (generated)

Top-level `public` types in `*.cs` (*11*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `IShortUrlGenerator`
- `IShortUrlService`
- `Metrics`
- `ShortUrlErrorCodes`
- `ShortUrlGenerator`
- `ShortUrlService`
- `ShortUrlServiceBase`
- `ShortUrlServiceOptions`
- `UrlShortenBuilder`

<!-- LYO_README_SYNC:END -->

