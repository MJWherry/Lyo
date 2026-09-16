# Lyo.Http.Client

Generic HTTP transport for Lyo. Vendors (Endato, Typecast, ESPN, Discord, Google Maps) and `Lyo.Http.Client.Flared` sit on `LyoHttpClient`. Lyo API problem-details and Query/QueryProject stay on `Lyo.Api.Client`.

Register one typed client through `IHttpClientFactory` (`AddLyoHttpClient<TClient, TOptions>` returns `IHttpClientBuilder`). Do not singleton that client or call `GetService<HttpClient>()`. Several sites share one `ILyoHttpClientFactory`: `Get` returns the cached client for that key. Tests may construct `new LyoHttpClient(new HttpClient(stubHandler), options)`.

## Features

- **`ILyoHttpClient` / `LyoHttpClient`.** JSON verbs, GET DTO query strings, file upload/download with progress, request compression, `LyoHttpException` on non-success (no problem-details).
- **Pipeline.** Observe → metrics → headers → rate limit (acquire) → timing (delay) → primary `LyoHttpClientHandler` (decompress). Hosts chain Polly via `AddStandardResilienceHandler()`.
- **Options builders.** `WithBaseUrl`, `UseProxy`, `UseTiming`, `UseUserAgent`, `CompressRequests`, `UseAcceptEncodings`, `UseRateLimit`, `UseBearer`, `UseApiKey`, `Accept` / `AcceptJson` / `AcceptLanguage`, and `UseHeader`. `AddLyoHttpClient` and a factory site call the same methods. Names come from `LyoHttpHeaders` and `HttpHeaderInfo`.
- **Site factory.** `AddLyoHttpClientFactory` and `AddLyoHttpClientFactoryFromConfiguration` (section `LyoHttpClientFactory`). Each key has its own client, pipeline, and quota. An unknown key logs a warning and returns the default client. Discord, Twilio, and Flared stay on `AddLyoHttpClient`.
- **`HttpClientPlanBuilder`.** `Build()` serializes (`type` discriminators). `Commit()` seals the open request; `Get`/`Post`/`DownloadUrls`/`Delay` auto-commit. Extract images/links/meta/json-ld/regex/table plus `filterList`/`mapList`.
- **Session.** `ILyoHttpCookieJar` + `ILyoHttpSession` (cookies, pinned UA, items). Secrets stay in a runtime cookie file, not committed plan JSON.

## Examples

### Register a site factory

```csharp
services.AddLocalRateLimit();
services.AddLyoHttpClientFactoryFromConfiguration(configuration, (factory, sp) =>
{
    factory.RegisterDefault(o => o.UseRateLimit(30, TimeSpan.FromSeconds(1)));
    factory.Register("mySite.com", o =>
    {
        o.WithBaseUrl("https://mySite.com/");
        o.UseBearer((sp, ct) => sp.GetRequiredService<ITokenService>().GetToken(ct));
        o.UseApiKey("site-key");
        o.AcceptJson();
        o.CompressRequests(LyoHttpRequestCompressionType.Gzip);
        o.UseRateLimit(5, TimeSpan.FromSeconds(1));
    });
});

var client = provider.GetRequiredService<ILyoHttpClientFactory>().Get("mySite.com");
```

### Register a typed client

```csharp
services.AddLyoHttpClient<EndatoClient, EndatoClientOptions>(configuration)
    .AddStandardResilienceHandler();
```

### Plan: extract then download

```csharp
var plan = HttpClientPlanBuilder.New("gallery")
    .Get("/gallery/{{slug}}")
    .ThenExtractImages("imageUrls")
    .ThenExtractLinks("zipUrls", extensionFilter: [".zip", ".pdf"])
    .Commit()
    .FilterList("imageUrls", "imageUrls", excludeRegex: "sprite|1x1")
    .Commit()
    .DownloadUrls("imageUrls", "{{downloadDir}}/img", "img")
    .Build();

await client.RunAsync(plan, runtime, ct);
```

## Architecture

Diagrams: [`HttpClientArchitecture.drawio`](HttpClientArchitecture.drawio) — **Overview** (three paths as flowcharts), **Request forks** (handler chain → ThroughSolver vs StreamBinary), **Handoff sequence** (numbered hops). Open in diagrams.net or the Draw.io extension.

## Pipeline order

Request flow: `LyoHttpObserveHandler` → `LyoHttpMetricsHandler` → `LyoHttpHeaderHandler` → `LyoHttpRateLimitHandler` (acquire a permit) → `LyoHttpTimingHandler` (delay) → `LyoHttpClientHandler` (or Flared/Clearance replacement) → optional host correlation sits outside this chain when added last → `LyoHttpClient` virtuals/hooks → per-request `before`.

A header already on the request is left as-is. A per-send resolver that returns text replaces the static value for that header. A null or empty token does not set a header. A factory site adds the rate-limit handler only when its quota is enabled. A typed client always includes that handler, and the handler calls `IRateLimitService` only when the quota is enabled.

A later `ConfigurePrimaryHttpMessageHandler` replaces decompression unless the replacement sets `AutomaticDecompression`. Factory clients are long-lived and are not recycled by `IHttpClientFactory`.

## Site factory

`AddLyoHttpClientFactory` runs its callback once, when the singleton is created, and passes the root `IServiceProvider`. `Register` builds that key immediately. Lookup is case-insensitive, and `Get` returns the same instance. A second `Register` of the same key throws. The caller does not dispose the client; disposing the factory does.

`AddLyoHttpClientFactoryFromConfiguration` binds section `LyoHttpClientFactory`. A missing section leaves the factory empty. `BearerToken` and `ApiKey` are static strings. JSON cannot store a token delegate. The callback runs after the bind, so `UseBearer` for that key replaces the static bearer.

With no `RegisterDefault` and no `Default` section, the default is a plain client with the quota off. Every unknown key shares that one instance and logs one warning.

`CompressRequests` sets `RequestCompression` and, when a minimum is passed, `RequestCompressionMinBytes` (default 1024). `UseBearer` sets `Authorization` to `Bearer {token}`. `UseApiKey` sets `X-Api-Key` to the raw secret. `AcceptJson` sets `Accept` to `application/json`.

```json
{
  "LyoHttpClientFactory": {
    "Default": {
      "RateLimit": { "Enabled": true, "PermitLimit": 30, "Window": "00:00:01" }
    },
    "Sites": {
      "mySite.com": {
        "BaseUrl": "https://mySite.com/",
        "RequestCompression": "Gzip",
        "Accept": "application/json",
        "ApiKey": "site-key",
        "BearerToken": "",
        "RateLimit": { "Enabled": true, "PermitLimit": 5, "Window": "00:00:01" }
      }
    }
  }
}
```

## Rate limit and timing

Both are **off** for generic/vendor clients. `Lyo.Http.Client.Flared` turns them on (token bucket 1 permit / 2s, 200–800 ms delay + jitter). The bucket refills continuously from the last spend. A host that enables a quota registers `AddLocalRateLimit` for one process, or `AddRedisRateLimit` when several hosts share it. Neither path registers a store. A missing `IRateLimitService` is ignored while the quota is off. Building a client whose quota is enabled throws when no store is registered. The typed-client policy name is the options type, so Flared does not share another client's bucket. A factory site uses the site key, so two sites do not share a quota. `Whitelist` entries (`GET /health`) skip the quota. `MaxConcurrent` stays per process.

On HTTP 429 the rate-limit handler honors `Retry-After` (capped, jittered). Each attempt takes one permit.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.RateLimit` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `AngleSharp` `1.5.0` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Threading.RateLimiting` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)