# Lyo.Config.Api

HTTP host for central app configuration, backed by PostgreSQL and [`Lyo.Config`](../../../Plugins/Config/Lyo.Config/README.md). Microservices resolve merged config per deployment identity and poll using ETags or an optional `version` query mirror.

Resolution contracts (`ConfigResolveConditionalResult`) live in [`Lyo.Config.Api.Models`](../Lyo.Config.Api.Models/README.md). The HTTP typed client and `AddConfigApiClientFromConfiguration` live in `Lyo.Config.Api.Client` ([readme](../Lyo.Config.Api.Client/README.md)). Route slug to `EntityRef` mapping uses `AppConfigEntity` from `Lyo.Config`. Polling plus `IOptionsMonitor<T>` is [`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting/README.md).

## Examples

### Embed the host (DI + middleware pipeline)

```csharp
using Lyo.Config.Api;
using Lyo.Config.Api.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConfigApi(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<RequireConfigApiKeyMiddleware>(); // BEFORE MapConfigApiEndpoints
app.MapConfigApiEndpoints(); // default prefix /api/config
app.Run();
```

### Samples (curl)

```bash
# Latest snapshot
curl -sS "http://localhost:5088/api/config/gateway/prod-west"

# Lightweight metadata only
curl -sSI "http://localhost:5088/api/config/gateway/prod-west"

# Poll with previous ETag (quoted)
ETAG='"A1B2C3..."'
curl -sSI -H "If-None-Match: $ETAG" "http://localhost:5088/api/config/gateway/prod-west"

# Same using version= (bare hex, no quotes)
curl -sSI "http://localhost:5088/api/config/gateway/prod-west?version=A1B2C3D4..."
```

## Embed the host (DI + middleware pipeline)

`Lyo.Config.Api` can run standalone (see [`Lyo.Config.Api.Host`](../Lyo.Config.Api.Host/README.md)) or be embedded into another host that already owns the WebApplication pipeline. Two extensions plus one middleware, all in `Lyo.Config.Api`, are the registration surface:

| Member | Defined in | Purpose |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `services.AddConfigApi(IConfiguration)` | [`Extensions.cs`](./Extensions.cs) | Registers `IConfigStore` via `AddPostgresConfigStoreFromConfiguration`, local cache, Lyo.Query, config query services, keyed `IEncryptionService` (`config-values`), and binds `ConfigApiSecurityOptions` / `ConfigApiHostingOptions`. |
| `app.MapConfigApiEndpoints(prefix = "/api/config", requireAuthentication = true)` | [`Extensions.cs`](./Extensions.cs) | Maps query/get/export at `Config/Definition` and `Config/Binding`, then mounts manage + resolve routes under `prefix`. The default prefix is `/api/config`. Pass `requireAuthentication: false` on TestApi so the Gateway workbench can call manage routes without scopes. |
| `UseMiddleware<RequireConfigApiKeyMiddleware>()` | [`Security/RequireConfigApiKeyMiddleware.cs`](./Security/RequireConfigApiKeyMiddleware.cs) | Path-scoped API-key gate (see below). Must run before `MapConfigApiEndpoints` in the request pipeline. |

How the standalone host is composed (and the pattern any embedding host should follow):

## `RequireConfigApiKeyMiddleware` ordering and behavior

- Registered as a **pipeline middleware** (not an authorization filter). It checks `HttpContext.Request.Path.StartsWithSegments("/api/config", …)` and **silently passes through any request outside that prefix**. If you relocate the API by passing a non-default prefix to `MapConfigApiEndpoints`, the middleware will not match it. `/api/config` is the hard-coded path check.
- When `ConfigApiSecurity.RequireApiKey == false`, the middleware short-circuits to `_next` without inspecting headers. Toggling `RequireApiKey` to `true` requires the host to configure a non-empty `ApiKey`; otherwise matching requests get `500 Internal Server Error` with `{ "detail": "API key enforcement is enabled but no server key has been configured." }`.
- When enabled, the middleware accepts the secret via `X-Api-Key: <value>` or `Authorization: Bearer <value>` (other schemes are rejected). Comparison uses `CryptographicOperations.FixedTimeEquals` over UTF-8 bytes; missing / empty / mismatching credentials produce `401 Unauthorized` with no body.
- Place this middleware **after** any TLS termination / proxy header middleware and **before** any logging that might leak request bodies, since it always returns before the endpoint runs on rejection.

## Security options (`ConfigApiSecurityOptions`, section `ConfigApiSecurity`)

| Key | Type | Default | Purpose |
| --------------------------------- | -------- | ------- | ---------------------------------------------------------------------------------------- |
| `ConfigApiSecurity:RequireApiKey` | `bool` | `false` | Master switch. When `false`, all routes are anonymous and the middleware is a no-op. |
| `ConfigApiSecurity:ApiKey` | `string` | `""` | Shared secret compared in constant time. Must be non-empty when `RequireApiKey == true`. |

> **Note:** this project has no authorization policy or scopes/role check, only the constant-time secret comparison above. If you need finer-grained access control,
> register your own auth middleware **before** `RequireConfigApiKeyMiddleware`, or replace it entirely.

## Hosting options (`ConfigApiHostingOptions`, section `ConfigApiHosting`)

| Key | Type | Default | Purpose |
| --------------------------------------------------- | ------ | ------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `ConfigApiHosting:PollIntervalAdvisoryMilliseconds` | `int?` | `null` | When > 0, emitted on every resolve response as the `X-Config-Poll-Interval-Ms` header. Purely advisory, clients are free to ignore it. |

## How routes map to `Lyo.Config`

A single store entity type `App` (`AppConfigEntity.AppEntityType`) is used for all API traffic for app config.

| URL segment | Meaning |
| ----------- | ---------------------------------------------------------------------------------------------------------- |
| `{appKind}` | Taxonomy for the process (e.g. `api`, `gateway`, `worker`). Lowercase slug: letters, digits, `-`, `_`, `.` |
| `{appId}` | Instance id (e.g. `checkout`, `550e8400-e29b-41d4-a716-446655440000`). Same slug rules after URL decode. |

The persisted compound id:

```text
EntityType = "App"
EntityId = "{appKind}:{appId}" // e.g. gateway:prod-west
```

Definitions you create with `PUT /manage/definitions` should use `forEntityType`: `"App"`. Bindings must use the same `App` + that compound `forEntityId`, or use
the manage routes below.

## At runtime: resolve and poll

Base path (default): `/api/config`.

## At runtime: resolve and poll. `GET`, `HEAD`, `POST`. `/{appKind}/{appId}`

- `HEAD`: same `ETag` / **304** behaviour, no body on 200.
- `POST`: same body as `GET` when you prefer not to put long ids in query strings.

## Management (`/api/config/manage`)

The same auth as the rest of `/api/config` is required when `ConfigApiSecurity.RequireApiKey` is true (`X-Api-Key` or `Authorization: Bearer`).

| Method | Path | Notes |
| ------ | -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| GET | `/definitions` | Lists definitions. Optional `?subjectEntityType=` (defaults to `App`). |
| PUT | `/definitions` | Body: `ConfigDefinitionRecord`. `IsEncrypted` requires a registered encryption service on this host. Returns the saved record (with Id). Appends a definition revision. Toggling Encrypted rewrites stored defaults and all binding revisions for that definition. |
| GET | `/definitions/{definitionId}/revisions` | Definition metadata history, newest first. |
| POST | `/definitions/{definitionId}/revert` | Body: `{ "revision": <int> }`. Copies the snapshot and appends a new revision. |
| DELETE | `/definitions/{definitionId}` | |
| PUT | `/bindings` | Body: `ConfigBindingRecord`. Returns the saved record. Encryption follows the definition flag on this host. |
| DELETE | `/bindings/{bindingId}` | |
| GET | `/bindings/{bindingId}/revisions` | |
| POST | `/bindings/{bindingId}/revert` | Body: `{ "revision": <int> }` |
| GET | `/apps/{appKind}/{appId}/bindings` | Convenience list for one app identity. |
| GET | `/apps/{appKind}/{appId}/bindings/{key}/revisions` | |
| POST | `/apps/{appKind}/{appId}/bindings/{key}/revert` | Body: `{ "revision": <int> }` |

## Query routes and encryption

Lyo.Query get/query/export at `Config/Definition` and `Config/Binding` (no generic CRUD writes) are also mapped by `MapConfigApiEndpoints`. Those queries deny `DefaultValueJson` and `EncryptedDefaultValue`. Value encryption uses a keyed `IEncryptionService` named `config-values` on this host and TestApi only. Clients send plaintext plus `IsEncrypted`; the API encrypts at rest. TestGateway does not register encryption — its workbench uses `ConfigApiStore` over HTTP so encryption still happens in TestApi.

## Configuration (`appsettings`)

- `PostgresConfig`: connection string and migrations for [`Lyo.Config.Postgres`](../../../Plugins/Config/Lyo.Config.Postgres/README.md).
- `ConfigApiHosting`: optional `PollIntervalAdvisoryMilliseconds` (see *Host embedding → Hosting options* above).
- `ConfigApiSecurity`: `RequireApiKey`, `ApiKey` (see *Host embedding → Security options* above).

## C# consumer (`Lyo.Config.Api.Client`)

Register the typed client:

```csharp
using Lyo.Config.Api.Client;

services.AddConfigApiClientFromConfiguration(configuration);
// Alternate section binding:
// services.AddConfigApiClientFromConfiguration(configuration, configSectionName: "MyConfigApi");

var resolved = await configClient.ResolveForAppAsync(
    appKind: "gateway",
    appId: "prod-west",
    ifNoneMatch: lastEtag,
    version: null,
    headOnly: false,
    cancellationToken: ct);

// Background poll
var merged = await ConfigPolling.PollUntilChangedAsync(
    configClient,
    appKind: "api",
    appId: "checkout",
    ifNoneMatch: null,
    delayWhenNotModified: TimeSpan.FromSeconds(15),
    cancellationToken: ct);
```

Bind `ConfigApi` in configuration for `BaseUrl`, optional `ApiKey`, `PollInterval`, and similar ([
`ConfigApiClientOptions`](../Lyo.Config.Api.Client/ConfigApiClientOptions.cs)). More examples: [`Lyo.Config.Api.Client/README.md`](../Lyo.Config.Api.Client/README.md).

## Run locally

```bash
dotnet run --project Lyo.Net/Apps/Config/Lyo.Config.Api/Lyo.Config.Api.csproj
```

`/openapi/v1.json` is the Development OpenAPI document (ASP.NET convention). Scalar UI is mapped only when the environment is **Development**.

## See also

- Feature docs: [`Lyo.Config/README.md`](../../../Plugins/Config/Lyo.Config/README.md)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Authentication` (direct, lyo)
- `Lyo.Authentication.AspNetCore` (direct, lyo)
- `Lyo.Authentication.Google` (direct, lyo)
- `Lyo.Authentication.Keycloak` (direct, lyo)
- `Lyo.Authentication.OpenIdConnect` (direct, lyo)
- `Lyo.Authentication.Postgres` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Config` (direct, lyo)
- `Lyo.Config.Postgres` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)