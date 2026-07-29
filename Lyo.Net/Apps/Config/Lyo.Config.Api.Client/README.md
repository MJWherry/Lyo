# Lyo.Config.Api.Client

Typed HTTP client for the central [`Lyo.Config.Api`](../Lyo.Config.Api/README.md) — **conditional** app-config reads with **`If-None-Match`** / **`?version`** polling, an
optional **`X-Api-Key`** header, and a single DI extension. The client deliberately exposes only the resolve route (`/api/config/{appKind}/{appId}`); the management API (
`/manage/...`) is intended for operator tools, not service callers.

References [`Lyo.Config.Api.Models`](../Lyo.Config.Api.Models/README.md) for `ConfigResolveConditionalResult` / `ConfigResolveOutcome`, and `Lyo.Config` for `AppConfigEntity`
slug validation and `ResolvedConfigRecord`. For a polling host that publishes config via `IOptionsMonitor<T>`, see
[`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting/README.md).

## Features

- Binds `ConfigApiClientOptions` from the section named by `configSectionName` (default **`"ConfigApi"`** via `ConfigApiClientOptions.SectionName`); the section is optional.
- Registers `IOptions<ConfigApiClientOptions>` (only when not already present).
- Calls `services.AddHttpClient<IConfigApiClient, ConfigApiClient>(...)` and returns the `IHttpClientBuilder` so callers can chain `.AddHttpMessageHandler<…>()` / `.AddPolicyHandler(…)` or replace the primary handler.
- Wires the typed client:
- `BaseAddress = "{BaseUrl}/"` when `BaseUrl` is non-empty.
- Adds the `X-Api-Key: <ApiKey>` default request header when `ApiKey` is non-empty.
- Appends `gzip` / `deflate` / `br` to `Accept-Encoding` based on `AcceptEncodings`.
- Enables `HttpClientHandler.AutomaticDecompression` from `AcceptEncodings` when `EnableAutoResponseDecompression == true`.

## Examples

### Register services

```csharp
using Lyo.Config.Api.Client;

services.AddConfigApiClientFromConfiguration(configuration);
// Optional override:
// services.AddConfigApiClientFromConfiguration(configuration, configSectionName: "MyConfigApi");
```

### `appsettings`

```json
{
  "ConfigApi": {
    "BaseUrl": "https://config.internal.example/",
    "ApiKey": "optional-shared-secret",
    "PollInterval": "00:01:30",
    "EnsureStatusCode": true,
    "AcceptEncodings": ["gzip", "br"],
    "EnableAutoResponseDecompression": true
  }
}
```

## Registration

- Binds `ConfigApiClientOptions` from the section named by `configSectionName` (default **`"ConfigApi"`** via `ConfigApiClientOptions.SectionName`); the section is optional.
- Registers `IOptions<ConfigApiClientOptions>` (only when not already present).
- Calls `services.AddHttpClient<IConfigApiClient, ConfigApiClient>(...)` and returns the `IHttpClientBuilder` so callers can chain `.AddHttpMessageHandler<…>()` / `.AddPolicyHandler(…)` or replace the primary handler.
- Wires the typed client:
- `BaseAddress = "{BaseUrl}/"` when `BaseUrl` is non-empty.
- Adds the `X-Api-Key: <ApiKey>` default request header when `ApiKey` is non-empty.
- Appends `gzip` / `deflate` / `br` to `Accept-Encoding` based on `AcceptEncodings`.
- Enables `HttpClientHandler.AutomaticDecompression` from `AcceptEncodings` when `EnableAutoResponseDecompression == true`.

## `ConfigApiClientOptions`

Section: **`ConfigApi`** by default. Inherits from `Lyo.Api.Client.ApiClientOptions`.

| Property | Default | Purpose |
| ------------------------------------- | -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`BaseUrl`** | `null` | Base URL of the Config API host. Trimmed and suffixed with `/`. |
| **`ApiKey`** | `null` | When set, forwarded as **`X-Api-Key`**. The server only enforces it if `ConfigApiSecurity.RequireApiKey == true` (see Config.Api README). |
| **`PollInterval`** (`TimeSpan?`) | `null` | **Advisory only.** Not consumed by `ConfigApiClient`; bring your own scheduler, or pass an explicit `TimeSpan` to `ConfigPolling.PollUntilChangedAsync`. |
| **`EnsureStatusCode`** (`bool`) | `true` | When `true`, non-success responses (other than `304 Not Modified`) call `EnsureSuccessStatusCode()` and throw `HttpRequestException`. When `false`, `ResolveForAppAsync` returns a `ConfigResolveConditionalResult` with `Outcome = Failed` and a populated `Failure` so the caller can inspect the status code without an exception. |
| **`AcceptEncodings`** | `["gzip", "deflate", "br"]` | Advertised encodings; only `gzip` / `deflate` / `br` are honored (and `br` requires a non-`netstandard2.0` target). |
| **`EnableAutoResponseDecompression`** | `true` | Toggles the matching `HttpClientHandler.AutomaticDecompression` flags. |
| **`RequestCompression`** | `ApiRequestCompressionType.None` | Inherited request-body compression — unused by the resolve route (GET / HEAD). |
| **`RequestCompressionMinBytes`** | `1024` | Inherited threshold for request-body compression. |

## `IConfigApiClient`

```csharp
public interface IConfigApiClient : IApiClient
{
    Task<ConfigResolveConditionalResult> ResolveForAppAsync(
        string appKind,
        string appId,
        string? ifNoneMatch = null,
        string? version = null,
        bool headOnly = false,
        CancellationToken ct = default);
}
```

The interface inherits from **`Lyo.Api.Client.IApiClient`**, so callers receive the generic CRUD / Query helpers exposed by `ApiClient` on the same instance. Only the
resolve method is added by this client.

## `IConfigApiClient` — `ResolveForAppAsync` parameters

| Parameter | Required | Description |
| ----------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`appKind`** | yes | Process taxonomy slug (e.g. `gateway`, `worker`). Must match `AppConfigEntity` slug rules; otherwise an `ArgumentException` is thrown before the request is sent. URL-encoded into the path. |
| **`appId`** | yes | Instance id slug (e.g. `prod-west`, a GUID). Same slug rules; URL-encoded. |
| **`ifNoneMatch`** | no | Previous `ETag` (quoted or bare hex — the server normalizes weak prefixes / quotes). Sent verbatim as `If-None-Match`. When the server replies `304 Not Modified`, the returned `ConfigResolveConditionalResult.ETag` falls back to this value. |
| **`version`** | no | Alternate fingerprint comparison sent as `?version=<bare-hex>`. Empty / whitespace values are omitted. May be used together with `If-None-Match`; either match yields `304`. |
| **`headOnly`** | no | When `true`, issues `HEAD` instead of `GET` and returns `Outcome = Ok` with `Resolved = null` on success. The server also responds 200 with no body for `HEAD`. |
| **`ct`** | no | Cancellation. Honored on `SendAsync` and JSON deserialization on modern targets. |

## `IConfigApiClient` — `EnsureStatusCode` behavior in detail

- **`HTTP 2xx`** → `ConfigResolveConditionalResult { Outcome = Ok, ETag, Resolved }`. If the server returns `204 No Content` or the client used `headOnly = true`, `Resolved` is `null`.
- **`HTTP 304 Not Modified`** → `ConfigResolveConditionalResult { Outcome = NotModified, ETag = <server etag or ifNoneMatch fallback>, Resolved = null }`. **Always returned — even when `EnsureStatusCode == true`**, because `304` is the expected polling response.
- **`HTTP 4xx/5xx`**:
- When `EnsureStatusCode == true` (default): `response.EnsureSuccessStatusCode()` is called and the resulting `HttpRequestException` propagates to the caller.
- When `EnsureStatusCode == false`: the method returns `ConfigResolveConditionalResult { Outcome = Failed, ETag, Resolved = null, Failure = (StatusCode, ReasonPhrase) }` so callers can branch on the status code without using exceptions for control flow. Use this mode when, for example, you want to treat `409 Conflict` (validation failure on the server) as a recoverable signal rather than a crash.

## `IConfigApiClient` — Slug validation

`appKind` and `appId` are checked with `AppConfigEntity.TryCreate(...)` **before** any HTTP request. Invalid slugs throw `ArgumentException` with the same message the API would return as `400 Bad Request`. There is no separate per-method validation toggle.

## `ConfigPolling.PollUntilChangedAsync`

Helper for long-running pollers that simply want the next changed `ResolvedConfigRecord`:

```csharp
var merged = await ConfigPolling.PollUntilChangedAsync(
    configClient,
    appKind: "api",
    appId: "checkout",
    ifNoneMatch: null,
    delayWhenNotModified: TimeSpan.FromSeconds(15),
    cancellationToken: ct);
```

Loop semantics:

| Server outcome | Action |
| -------------------- | ------------------------------------------------------------------------------------------ |
| `Ok` with `Resolved` | Return the payload immediately. |
| `NotModified` | Await `delayWhenNotModified` (cancellation-aware), reuse the latest `ETag`, and loop. |
| `Failed` | Throw `InvalidOperationException` carrying the status code / reason phrase from `Failure`. |

`OperationHelpers.ThrowIfNull` guards against a server returning `Ok` with no body; treat that as a contract bug.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Config.Api.Models` — (direct, lyo)
- `Microsoft.Extensions.Http` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (direct, microsoft)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Config` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft)