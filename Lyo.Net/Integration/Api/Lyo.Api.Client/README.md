# Lyo.Api.Client

HTTP client for Lyo minimal APIs: JSON in/out, gzip/brotli/deflate, query-string encoding for GET DTOs, file upload helpers, and `System.Text.Json` parity with server options when you wire them.

`ApiClient` implements `IDisposable` and disposes its resources. Resolve it via `IHttpClientFactory` so lifetimes stay correct in DI.

## Examples

### DI registration

```csharp
services.AddLyoApiClient(
    optionsOverride: o => {
        o.BaseUrl = "https://api.example.com/";
        o.EnableAutoResponseDecompression = true;
        o.AcceptEncodings = ["gzip", "br"];
        o.RequestCompression = ApiRequestCompressionType.Gzip;
        o.RequestCompressionMinBytes = 4 * 1024;
    },
    httpClientBuilderOverride: b => b.AddStandardResilienceHandler());
```

## `IApiClient` methods

**Serialization**

- `GetSerializerOptions()` exposes effective `JsonSerializerOptions`. Use the same instance for ad-hoc serializers in your worker to avoid schema drift.

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

Throws `ApiException` wrapping non-success status codes with contextual payload extraction (see class for available properties). `ApiException` derives from
`Lyo.Exceptions.Models.HttpException`, so callers can handle it through the shared HTTP hierarchy: `StatusCode` and `ErrorCode` (populated from the first parsed
`LyoProblemDetails` error code) come from the base type, and `IsTransient` is `true` for 408/429/502/503/504, which lets `Lyo.Resilience` retry pipelines pick it up
automatically.

## Options ([`ApiClientOptions`](ApiClientOptions.cs))

Configuration section: `ApiClientOptions.SectionName = "ApiClient"`. Integration-specific clients (Discord, Endato, ESPN, Typecast, …) subclass this type and shadow `SectionName`
so all transport flags bind under their own section.

| Property | Default | Description |
| --------------------------------- | -------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BaseUrl` | `null` | When set, becomes `HttpClient.BaseAddress` (trailing `/` enforced); relative URIs resolve against it. |
| `EnsureStatusCode` | `true` | Calls `EnsureSuccessStatusCode` after each response. Set `false` when the server returns problem-details bodies on non-success codes that the caller wants to inspect. |
| `AcceptEncodings` | `["gzip","deflate","br"]`* | Sent as `Accept-Encoding`. *On `netstandard2.0` the default drops `br` (Brotli is not built in there). Duplicates are removed and normalized to lowercase. |
| `EnableAutoResponseDecompression` | `true` | Enables `LyoHttpClientHandler.AutomaticDecompression` for `gzip`/`deflate`/`br` when the client uses that primary handler (`AddLyoApiClient` / `CreateHttpClient`). Replacing the primary handler drops decompression unless the replacement sets it. |
| `RequestCompression` | `None` | `ApiRequestCompressionType` for outgoing JSON bodies: `None`, `Gzip`, `Deflate`, `Brotli`. Sets `Content-Encoding` accordingly. |
| `RequestCompressionMinBytes` | `1024` | Minimum serialized payload size before compression applies (skips CPU on tiny bodies). |

Pair request compression with a host that registers `AddRequestDecompression` (ASP.NET Core 7+) so the server can decode the body.

## Compression and performance

- Adds `Accept-Encoding` headers from `AcceptEncodings` (duplicates removed, case normalized).
- Uses `LyoHttpClientHandler` as the `IHttpClientFactory` primary handler (`UseLyoHttpClientHandler` / `UseLyoHttpClientHandler<TOptions>`). Other typed clients (Config, etc.) should call the same helper instead of copying `AutomaticDecompression` setup.
- JSON methods still sniff gzip/deflate magic bytes and strip a BOM. File/binary methods do not: they return whatever the handler already decoded.
- A later `ConfigurePrimaryHttpMessageHandler` replaces decompression. Subclass `LyoHttpClientHandler` or set `AutomaticDecompression` on the replacement. Do not add a second decompressing `DelegatingHandler`.
- Returns the underlying `IHttpClientBuilder` so callers can chain resilience, message handlers, or named-client overrides.

## DI registration

`clientName` defaults to `nameof(IApiClient)` for named `HttpClientFactory` resolution. Bind from configuration with the standard `services.Configure<ApiClientOptions>(config.GetSection(ApiClientOptions.SectionName))` if you prefer the section route.

## Typical integration tests

Host the API with `WebApplicationFactory`, call through `IApiClient`, and assert `ApiException.StatusCode` plus ProblemDetails bodies using models from [`Lyo.Api.Models`](../Lyo.Api.Models/README.md).

## Related

- [`Lyo.Api.Models`](../Lyo.Api.Models/README.md). Payloads + error contracts.
- [`Lyo.Api`](../Lyo.Api/README.md). Server endpoints this client calls.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Common` (direct, lyo)
- `Lyo.Diagnostic` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)