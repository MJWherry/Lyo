# Lyo.ShortUrl

Core abstractions for URL shortening: an `IShortUrlService` contract, a `ShortUrlServiceBase` that handles validation / metrics / error-code mapping, a default
`ShortUrlService` that **generates** short codes (no storage), a fluent `UrlShortenBuilder`, and DTOs for shorten / expand / statistics results.

## Surface

### `IShortUrlService`

- `Task<UrlShortenResult> ShortenAsync(string longUrl, string? customAlias = null, DateTime? expirationDate = null, CancellationToken ct = default)`
- `Task<UrlShortenResult> ShortenAsync(UrlShortenBuilder builder, CancellationToken ct = default)`
- `Task<UrlExpandResult> ExpandAsync(string shortUrl, CancellationToken ct = default)`
- `Task<UrlStatisticsResult> GetStatisticsAsync(string shortUrl, CancellationToken ct = default)`
- `Task<bool> DeleteAsync(string shortUrl, CancellationToken ct = default)`
- `Task<UrlShortenResult> UpdateAsync(string shortUrl, string newLongUrl, CancellationToken ct = default)`
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`

### `ShortUrlServiceBase`

Reusable base class for custom implementations (e.g. Bitly, your own storage). It:

- Validates inputs (`longUrl` non-empty; `expirationDate` strictly in the future; HTTP → HTTPS rewrite when `Options.EnforceHttps == true`).
- Wraps `ShortenAsync` / `ExpandAsync` in metrics timers + counters keyed off `MetricNames` (override `CreateMetricNamesDictionary()` to rebrand).
- Maps `OperationCanceledException` → `SHORTURL_OPERATION_CANCELLED`, other exceptions → `SHORTURL_*_FAILED` codes.
- Provides default `NotSupportedException` throws for `GetStatisticsAsync`, `DeleteAsync`, `UpdateAsync`, `TestConnectionAsync`, `ShortenCoreAsync`, and `ExpandCoreAsync` so
  partial implementations only override what they support.

### `ShortUrlService` (in-box)

Concrete subclass focused on **identifier generation**, suitable for stateless code-issuing services or as a building block when you bring your own storage:

- `ShortenCoreAsync` validates the custom alias against `Options.AllowCustomAliases` / `MinAliasLength` / `MaxAliasLength` and delegates to `IShortUrlGenerator.Generate` when
  none is supplied, then returns `UrlShortenResult.FromSuccess(...)` with `BaseUrl/{id}` (or just `{id}` when `BaseUrl` is empty).
- `ExpandCoreAsync`, `GetStatisticsAsync`, and `UpdateAsync` return an error result with
  `SHORTURL_EXPAND_FAILED` / `SHORTURL_GET_STATISTICS_FAILED` / `SHORTURL_UPDATE_FAILED` — they explicitly require a storage-backed implementation.
- `DeleteAsync` throws `NotSupportedException` for the same reason.
- `TestConnectionAsync` always returns `true` (no backend to probe).

### `IShortUrlGenerator` / `ShortUrlGenerator`

- `string Generate(int? length = null)` — returns a base-62 string (`a-zA-Z0-9`) of the requested length. The default `ShortUrlGenerator` uses `RandomNumberGenerator` and an
  8-character default length.

### `UrlShortenBuilder`

Fluent helper that validates as you go:

- `SetLongUrl(string longUrl, bool enforceHttps = false)` — runs through `UriHelpers.GetValidWebUri` (throws `InvalidFormatException` on invalid URLs); when `enforceHttps`,
  HTTP becomes HTTPS.
- `SetCustomAlias(string? alias)` — must match `^[a-zA-Z0-9\-]+$`; passing `null`/whitespace clears the alias.
- `SetExpirationDate(DateTime? date)` — must be in the future (or `null`).
- `Clear()` — resets the builder.
- `Build()` returns `(LongUrl, CustomAlias, ExpirationDate)`; throws if `LongUrl` was never set.
- `UrlShortenBuilder.New()` — convenience factory.

### Error codes (`ShortUrlErrorCodes`)

`SHORTURL_SHORTEN_FAILED`, `SHORTURL_EXPAND_FAILED`, `SHORTURL_GET_STATISTICS_FAILED`, `SHORTURL_DELETE_FAILED`, `SHORTURL_UPDATE_FAILED`,
`SHORTURL_OPERATION_CANCELLED`, `SHORTURL_URL_NOT_FOUND`, `SHORTURL_URL_EXPIRED`, `SHORTURL_INVALID_URL`, `SHORTURL_ALIAS_ALREADY_EXISTS`,
`SHORTURL_CUSTOM_ALIAS_NOT_ALLOWED`, `SHORTURL_INVALID_ALIAS_LENGTH`.

### `ShortUrlServiceOptions`

| Member                  | Default             | Notes                                                                            |
|-------------------------|---------------------|----------------------------------------------------------------------------------|
| `BaseUrl`               | empty               | Prepended to generated ids (e.g. `https://short.ly`).                            |
| `DefaultExpirationDays` | `null`              | Reserved for storage-backed implementations.                                     |
| `MaxAliasLength`        | `50`                | Enforced by `ShortUrlService.ShortenCoreAsync`.                                  |
| `MinAliasLength`        | `3`                 | Enforced by `ShortUrlService.ShortenCoreAsync`.                                  |
| `AllowCustomAliases`    | `true`              | When `false`, custom aliases are rejected with `CUSTOM_ALIAS_NOT_ALLOWED`.       |
| `EnableMetrics`         | `false`             | Gates the `IMetrics` integration in `ShortUrlServiceBase`.                       |
| `EnforceHttps`          | `false`             | When `true`, HTTP URLs are rewritten to HTTPS before hitting `ShortenCoreAsync`. |
| `SectionName` *(const)* | `"ShortUrlOptions"` | Default appsettings section for `AddShortUrlFromConfiguration`.                  |

## DI registration (`Extensions`)

| Entry point                                                                                                                    | What it does                                                                                               |
|--------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| `services.AddShortUrlGenerator()`                                                                                              | Registers singleton `IShortUrlGenerator` → `ShortUrlGenerator`.                                            |
| `services.AddShortUrl(Action<ShortUrlServiceOptions>? configure = null)` *(plus an `(options)` overload)*                      | Registers `ShortUrlServiceOptions`, the generator, and singleton `IShortUrlService` → `ShortUrlService`.   |
| `services.AddShortUrlFromConfiguration(IConfiguration configuration, string sectionName = ShortUrlServiceOptions.SectionName)` | Same as `AddShortUrl(...)` but binds options from the configuration section (default `"ShortUrlOptions"`). |
| `services.AddShortUrlService<TService, TOptions>(Action<TOptions>? configure = null)`                                          | Generic registration for a custom `IShortUrlService` + `TOptions : ShortUrlServiceOptions, new()` pair.    |
| `services.AddShortUrlService<TService>(ShortUrlServiceOptions options)`                                                        | Same with a pre-built options instance.                                                                    |

## Quick start

```csharp
using Lyo.ShortUrl;
using Lyo.ShortUrl.Models;

services.AddShortUrl(o => {
    o.BaseUrl = "https://short.ly";
    o.AllowCustomAliases = true;
    o.MaxAliasLength = 50;
});

var builder = UrlShortenBuilder.New()
    .SetLongUrl("https://example.com/long-url")
    .SetCustomAlias("my-alias")
    .SetExpirationDate(DateTime.UtcNow.AddDays(30));

var result = await shortUrlService.ShortenAsync(builder, ct);
// result.IsSuccess, result.ShortUrl, result.LongUrl, result.Alias, result.Errors

// Expand / stats / update / delete require a storage-backed IShortUrlService
// implementation; the in-box ShortUrlService surfaces NotSupported errors there.
```

### Configuration (`appsettings.json`)

```json
{
  "ShortUrlOptions": {
    "BaseUrl": "https://short.ly",
    "DefaultExpirationDays": 30,
    "MaxAliasLength": 50,
    "MinAliasLength": 3,
    "AllowCustomAliases": true,
    "EnableMetrics": false,
    "EnforceHttps": false
  }
}
```

## Dependencies

*(Synchronized from `Lyo.ShortUrl.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                                 | Version |
|---------------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
