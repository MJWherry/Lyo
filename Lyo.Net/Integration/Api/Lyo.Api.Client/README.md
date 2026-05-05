# Lyo.Api.Client

HTTP client tailored for **Lyo-shaped minimal APIs**: JSON in/out, gzip/brotli/deflate handling, **query-string encoding** for GET DTOs, file upload helpers, and **`System.Text.Json`** parity with server options when you wire them.

Implements **`IDisposable`** (**`ApiClient`** disposes underlying resources—resolve via **`IHttpClientFactory`** so lifetimes stay correct in DI).

## Surface (`IApiClient`)

**Serialization**

- **`GetSerializerOptions()`** exposes effective **`JsonSerializerOptions`**—use the same instance for ad-hoc serializers in your worker to avoid schema drift.

**GET**

- **`GetAsAsync<TResult>(uri, beforeRequest, ct)`** basic JSON GET.
- **`GetAsAsync<TRequest, TResult>(uri, query, enumerableDelimiter, …)`** serializes `TRequest` properties as query parameters (helps mirror `Lyo.Api` endpoints that accept flattened DTO queries).

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

## Compression & performance

`AddLyoApiClient` registers:

- **`Accept-Encoding`** headers from **`ApiClientOptions.AcceptEncodings`** (duplicates removed, case normalized).
- **`HttpClientHandler.AutomaticDecompression`** when **`EnableAutoResponseDecompression`** true (maps gzip/deflate/brotli where target framework supports Brotli).

For high-throughput ingestion, prefer **streaming** reads + **chunked uploads** instead of buffering large files as `byte[]`.

## DI registration

```csharp
services.AddLyoApiClient(
    optionsOverride: o => {
        o.EnableAutoResponseDecompression = true;
        o.AcceptEncodings = ["gzip", "br"];
    },
    httpClientBuilderOverride: b => b.AddStandardResilienceHandler());
```

`clientName` defaults to **`nameof(IApiClient)`** for named `HttpClientFactory` resolution.

## Typical integration tests

Spin `WebApplicationFactory` for your API host, call through **`IApiClient`**, assert **`ApiException.StatusCode`** ProblemDetails bodies using shared models from [`Lyo.Api.Models`](../Lyo.Api.Models/README.md).

## Related

- [`Lyo.Api.Models`](../Lyo.Api.Models/README.md) — payloads + error contracts.
- [`Lyo.Api`](../Lyo.Api/README.md) — authoritative server behavior you are mirroring client-side.
