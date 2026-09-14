# Lyo.Api.Client

Lyo-specific HTTP client on top of `LyoHttpClient`. Non-success responses throw `ApiException` with RFC 7807 problem-details when the body parses. `QueryProjectAsync` / `QueryConcreteAsync` POST to `{route}/QueryProject` and `{route}/QueryConcrete` using `Lyo.Query.Models` only here.

Vendor clients (Endato, Typecast, ESPN, Discord, Google Maps) now inherit `LyoHttpClient` in `Lyo.Http.Client`. Config.Api.Client stays on `ApiClient`. Resolve via `IHttpClientFactory`.

## Features

- **`IApiClient` / `ApiClient`.** Subclass of `LyoHttpClient`. JSON verbs inherited; `EnsureSuccessAsync` maps problem-details to `ApiException`. User-Agent is `Lyo/{version}` (no UA rotation).
- **`QueryProjectAsync` / `QueryConcreteAsync`.** `POST {route}/QueryProject` and `POST {route}/QueryConcrete`.
- **`ApiRouteBuilder`.** `Build(routePrefix, relativePath)` applies the host's configured mount point, and `WithIncludes(route, includes)` appends one `include` query parameter per navigation to expand.
- **`AddLyoApiClient`.** Factory registration for `IApiClient` plus optional correlation handler. Vendor `AddLyoApiClient<T,T>` is obsolete — use `AddLyoHttpClient`.

## Examples

### Register in DI

```csharp
services.AddLyoApiClient(
    optionsOverride: o => {
        o.BaseUrl = "https://api.example.com/";
        o.EnableAutoResponseDecompression = true;
        o.AcceptEncodings = ["gzip", "br"];
        o.RequestCompression = LyoHttpRequestCompressionType.Gzip;
        o.RequestCompressionMinBytes = 4 * 1024;
    },
    httpClientBuilderOverride: b => b.AddStandardResilienceHandler());
```

### Register a vendor client

```csharp
// Vendor packages call AddLyoHttpClient and return IHttpClientBuilder.
services.AddEndatoClientFromConfiguration(configuration)
    .AddStandardResilienceHandler();
```

## `IApiClient` methods

**Serialization**

- Effective `JsonSerializerOptions` come from `GetSerializerOptions()`. Use the same instance for ad-hoc serializers in your worker to avoid schema drift.

**GET**

- `GetAsAsync<TResult>(uri, beforeRequest, ct)` returns deserialized JSON.
- `GetAsAsync<TRequest, TResult>(uri, query, enumerableDelimiter, …)` serializes `TRequest` properties as query parameters so GET DTOs match `Lyo.Api` flattened query endpoints.

**Bodies and verbs**

- **`PostAsAsync` / `PutAsAsync` / `PatchAsAsync` / `DeleteAsAsync`** map to JSON content (generic + non-generic overloads).
- `PostAsBinaryAsync` for raw byte returns (exports, generated PDFs, etc.).

**Files**

- **`GetFileAsync` / `GetFileWithTypeAsync`** buffer the payload as the `HttpClient` already decoded it. Use `AddLyoApiClient` / `LyoHttpClientHandler` so gzip/br/deflate transport encoding is stripped. A stored `.gz` without `Content-Encoding` is left as-is.
- `GetFileStreamAsync` returns **`Stream` + filename + length** without forcing memory spikes. Dispose the stream to release the response.
- `PostFileAsAsync` overloads stream/byte[]/path + `FileTypeInfo` for MIME + extension hints.

**Customization hook**

Each method accepts optional `Action<HttpRequestMessage>` to append auth headers (`Authorization: Bearer …`), correlation ids, `Accept` overrides, or tracing headers.

Non-success status codes throw `ApiException` wrapping contextual payload extraction (see class for available properties). `ApiException` derives from
`Lyo.Exceptions.Models.HttpException`, so callers can handle it through the shared HTTP hierarchy: `StatusCode` and `ErrorCode` (populated from the first parsed
`LyoProblemDetails` error code) come from the base type, and `IsTransient` is `true` for 408/429/502/503/504, which lets `Lyo.Resilience` retry pipelines pick it up
automatically.

## Options ([`ApiClientOptions`](ApiClientOptions.cs))

Bind from `ApiClientOptions.SectionName = "ApiClient"`. Clients for a specific integration (Discord, Endato, ESPN, Typecast, …) subclass the options type and shadow `SectionName`
so transport flags land under that integration's own section.

| Property | Default | Description |
| --------------------------------- | -------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BaseUrl` | `null` | When set, becomes `HttpClient.BaseAddress` (trailing `/` enforced); relative URIs resolve against it. |
| `EnsureStatusCode` | `true` | Calls `EnsureSuccessStatusCode` after each response. Set `false` when the server returns problem-details bodies on non-success codes that the caller wants to inspect. |
| `AcceptEncodings` | `["gzip","deflate","br"]`* | Sent as `Accept-Encoding`. *On `netstandard2.0` the default drops `br` (Brotli is not built in there). Duplicates are removed and normalized to lowercase. |
| `EnableAutoResponseDecompression` | `true` | Enables `LyoHttpClientHandler.AutomaticDecompression` for `gzip`/`deflate`/`br` when the client uses that primary handler (`AddLyoApiClient` / `CreateHttpClient`). Replacing the primary handler drops decompression unless the replacement sets it. |
| `RequestCompression` | `None` | `LyoHttpRequestCompressionType` for outgoing JSON bodies: `None`, `Gzip`, `Deflate`, `Brotli`. Sets `Content-Encoding` accordingly. |
| `RequestCompressionMinBytes` | `1024` | Minimum serialized payload size before compression applies (skips CPU on tiny bodies). |

Request compression only works if the host also registers `AddRequestDecompression` (ASP.NET Core 7+) and can decode the compressed body.

## Compression and performance

- Adds `Accept-Encoding` headers from `AcceptEncodings` (duplicates removed, case normalized).
- Uses `LyoHttpClientHandler` as the `IHttpClientFactory` primary handler (`UseLyoHttpClientHandler` / `UseLyoHttpClientHandler<TOptions>`). Other typed clients (Config, etc.) should call the same helper instead of copying `AutomaticDecompression` setup.
- JSON methods still sniff gzip/deflate magic bytes and strip a BOM. File/binary methods do not: they return whatever the handler already decoded.
- A later `ConfigurePrimaryHttpMessageHandler` replaces decompression. Subclass `LyoHttpClientHandler` or set `AutomaticDecompression` on the replacement. Do not add a second decompressing `DelegatingHandler`.
- Returns the underlying `IHttpClientBuilder` so callers can chain resilience, message handlers, or named-client overrides.

## Register in DI

The default `clientName` is `nameof(IApiClient)` for named `HttpClientFactory` resolution. Bind from configuration with the standard `services.Configure<ApiClientOptions>(config.GetSection(ApiClientOptions.SectionName))` if you prefer the section route.

## Vendor client registration (`AddLyoApiClient<TClient, TOptions>`)

Vendor clients (Endato, Typecast, ESPN Fantasy Football, Discord, Google Maps) inherit `LyoHttpClient` and register with `AddLyoHttpClient<TClient, TOptions>`, which returns `IHttpClientBuilder`.

The obsolete `AddLyoApiClient<TClient, TOptions>` overloads on this package forward to that factory path so older hosts still compile. Do not use singleton + `GetService<HttpClient>()`.

## Typical integration tests

Stand the API up under `WebApplicationFactory`, talk to it through `IApiClient`, then assert `ApiException.StatusCode` and ProblemDetails payloads with types from [`Lyo.Api.Models`](../Lyo.Api.Models/README.md).

## Related

- [`Lyo.Api.Models`](../Lyo.Api.Models/README.md). Payloads + error contracts.
- [`Lyo.Api`](../Lyo.Api/README.md). Server endpoints this client calls.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Diagnostic` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)