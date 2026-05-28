# Lyo.Api.Client

HTTP client tailored for **Lyo-shaped minimal APIs**: JSON in/out, gzip/brotli/deflate handling, **query-string encoding** for GET DTOs, file upload helpers, and *
*`System.Text.Json`** parity with server options when you wire them.

Implements **`IDisposable`** (**`ApiClient`** disposes underlying resources—resolve via **`IHttpClientFactory`** so lifetimes stay correct in DI).

## Surface (`IApiClient`)

**Serialization**

- **`GetSerializerOptions()`** exposes effective **`JsonSerializerOptions`**—use the same instance for ad-hoc serializers in your worker to avoid schema drift.

**GET**

- **`GetAsAsync<TResult>(uri, beforeRequest, ct)`** basic JSON GET.
- **`GetAsAsync<TRequest, TResult>(uri, query, enumerableDelimiter, …)`** serializes `TRequest` properties as query parameters (helps mirror `Lyo.Api` endpoints that accept
  flattened DTO queries).

**Bodies & verbs**

- **`PostAsAsync` / `PutAsAsync` / `PatchAsAsync` / `DeleteAsAsync`** map to JSON content (generic + non-generic overloads).
- **`PostAsBinaryAsync`** for raw byte returns (exports, generated PDFs, etc.).

**Files**

- **`GetFileAsync` / `GetFileWithTypeAsync`** buffer entire payload.
- **`GetFileStreamAsync`** returns **`Stream` + filename + length** without forcing memory spikes—**caller disposes** underlying **`HttpResponseMessage`** per XML contract.
- **`PostFileAsAsync`** overloads stream/byte[]/path + **`FileTypeInfo`** for MIME + extension hints.

**Customization hook**

Each method accepts optional **`Action<HttpRequestMessage>`** to append auth headers (`Authorization: Bearer …`), correlation ids, `Accept` overrides, or tracing headers.

Throws **`ApiException`** wrapping non-success status codes with contextual payload extraction (see class for available properties).

## Options ([`ApiClientOptions`](ApiClientOptions.cs))

Configuration section: `ApiClientOptions.SectionName = "ApiClient"`. Integration-specific clients (Discord, Endato, ESPN, Typecast, …) subclass this type and shadow `SectionName`
so all transport flags bind under their own section.

| Property                          | Default                    | Description                                                                                                                                                            |
|-----------------------------------|----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `BaseUrl`                         | `null`                     | When set, becomes `HttpClient.BaseAddress` (trailing `/` enforced); relative URIs resolve against it.                                                                  |
| `EnsureStatusCode`                | `true`                     | Calls `EnsureSuccessStatusCode` after each response. Set `false` when the server returns problem-details bodies on non-success codes that the caller wants to inspect. |
| `AcceptEncodings`                 | `["gzip","deflate","br"]`* | Sent as `Accept-Encoding`. *On `netstandard2.0` the default drops `br` (Brotli is not built in there). Duplicates are removed and normalized to lowercase.             |
| `EnableAutoResponseDecompression` | `true`                     | Enables `HttpClientHandler.AutomaticDecompression` for `gzip`/`deflate`/`br` when `ApiClient` creates its own handler.                                                 |
| `RequestCompression`              | `None`                     | `ApiRequestCompressionType` for outgoing JSON bodies: `None`, `Gzip`, `Deflate`, `Brotli`. Sets `Content-Encoding` accordingly.                                        |
| `RequestCompressionMinBytes`      | `1024`                     | Minimum serialized payload size before compression kicks in (avoids spending CPU on tiny bodies).                                                                      |

Pair request compression with a host that registers `AddRequestDecompression` (ASP.NET Core 7+) so the server can decode the body.

## Compression & performance

`AddLyoApiClient` (in [`ServiceCollectionExtensions`](ServiceCollectionExtensions.cs)) wires an `HttpClientFactory` registration that:

- Adds `Accept-Encoding` headers from **`AcceptEncodings`** (duplicates removed, case normalized).
- Sets **`HttpClientHandler.AutomaticDecompression`** when **`EnableAutoResponseDecompression`** is `true` (maps `gzip`/`deflate`/`br` where the target framework supports Brotli).
- Returns the underlying `IHttpClientBuilder` so callers can chain resilience, message handlers, or named-client overrides.

For high-throughput ingestion, prefer **streaming** reads + **chunked uploads** instead of buffering large files as `byte[]`.

## DI registration

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

`clientName` defaults to **`nameof(IApiClient)`** for named `HttpClientFactory` resolution. Bind from configuration with the standard
`services.Configure<ApiClientOptions>(config.GetSection(ApiClientOptions.SectionName))` if you prefer the section route.

## Typical integration tests

Spin `WebApplicationFactory` for your API host, call through **`IApiClient`**, assert **`ApiException.StatusCode`** ProblemDetails bodies using shared models from [
`Lyo.Api.Models`](../Lyo.Api.Models/README.md).

## Related

- [`Lyo.Api.Models`](../Lyo.Api.Models/README.md) — payloads + error contracts.
- [`Lyo.Api`](../Lyo.Api/README.md) — authoritative server behavior you are mirroring client-side.
