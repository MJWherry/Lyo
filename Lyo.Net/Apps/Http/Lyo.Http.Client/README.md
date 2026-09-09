# Lyo.Http.Client

Generic HTTP transport for Lyo. Vendors (Endato, Typecast, ESPN, Discord, Google Maps) and `Lyo.Http.Client.Flared` sit on `LyoHttpClient`. Lyo API problem-details and Query/QueryProject stay on `Lyo.Api.Client`.

Register through `IHttpClientFactory` (`AddLyoHttpClient<TClient, TOptions>` returns `IHttpClientBuilder`). Do not singleton the client or call `GetService<HttpClient>()`. Tests may construct `new LyoHttpClient(new HttpClient(stubHandler), options)`.

## Features

- **`ILyoHttpClient` / `LyoHttpClient`.** JSON verbs, GET DTO query strings, file upload/download with progress, request compression, `LyoHttpException` on non-success (no problem-details).
- **Pipeline.** Observe → metrics → rate limit (acquire) → timing (delay) → primary `LyoHttpClientHandler` (decompress). Hosts chain Polly via `AddStandardResilienceHandler()`.
- **`HttpClientPlanBuilder`.** The only fluent API. `Build()` serializes (`type` discriminators). `Commit()` seals the open request; `Get`/`Post`/`DownloadUrls`/`Delay` auto-commit. Extract images/links/meta/json-ld/regex/table plus `filterList`/`mapList`.
- **Session.** `ILyoHttpCookieJar` + `ILyoHttpSession` (cookies, pinned UA, items). Secrets stay in a runtime cookie file, not committed plan JSON.

## Examples

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

Request flow: `LyoHttpObserveHandler` → `LyoHttpMetricsHandler` → `LyoHttpRateLimitHandler` (acquire a permit) → `LyoHttpTimingHandler` (delay) → `LyoHttpClientHandler` (or Flared/Clearance replacement) → optional host correlation sits outside this chain when added last → `LyoHttpClient` virtuals/hooks → per-request `before`.

A later `ConfigurePrimaryHttpMessageHandler` replaces decompression unless the replacement sets `AutomaticDecompression`.

## Rate limit and timing

Both are **off** for generic/vendor clients. `Lyo.Http.Client.Flared` turns them on (token bucket 1/2s, 200–800 ms delay + jitter). Limiters are per named client / options type so Flared does not share Endato's bucket.

On HTTP 429 the rate-limit handler honors `Retry-After` (capped, jittered) then throws `RateLimitExceededException`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `AngleSharp` `1.5.0` (direct, third-party)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Threading.RateLimiting` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)