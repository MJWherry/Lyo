# Lyo.Config.Api

HTTP host for central **app** configuration backed by PostgreSQL and [`Lyo.Config`](../../../Features/Config/Lyo.Config/README.md). Microservices resolve merged config per deployment identity and poll using **ETags** or an optional **`version`** query mirror.

Resolution contracts (**`ConfigResolveConditionalResult`**) live in **[`Lyo.Config.Api.Models`](../Lyo.Config.Api.Models/README.md)**. The HTTP typed client and **`AddConfigApiClientFromConfiguration`** live in **`Lyo.Config.Api.Client`** ([readme](../Lyo.Config.Api.Client/README.md)). Route slug → **`EntityRef`** mapping uses **`AppConfigEntity`** from **`Lyo.Config`**. Polling plus **`IOptionsMonitor<T>`** is **[`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting/README.md)**.

## Examples

### Host embedding (DI + middleware pipeline)

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

### Examples (curl)

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

## Host embedding (DI + middleware pipeline)

`Lyo.Config.Api` is structured so it can run standalone (see `Program.cs`) **or** be embedded into another host that already owns the WebApplication pipeline. The full surface is
two extensions plus one middleware, all in `Lyo.Config.Api`:

| Member | Defined in | Purpose |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`services.AddConfigApi(IConfiguration)`** | [`Extensions.cs`](./Extensions.cs) | Registers `IConfigStore` via `AddPostgresConfigStoreFromConfiguration`, binds `ConfigApiSecurityOptions` (section `ConfigApiSecurity`) and `ConfigApiHostingOptions` (section `ConfigApiHosting`). |
| **`app.MapConfigApiEndpoints(prefix = "/api/config")`** | [`Extensions.cs`](./Extensions.cs) | Returns a `RouteGroupBuilder` and mounts the `manage/*` and `{appKind}/{appId}` route groups under `prefix`. The default prefix is `/api/config`; pass another value to relocate the whole API. |
| **`UseMiddleware<RequireConfigApiKeyMiddleware>()`** | [`Security/RequireConfigApiKeyMiddleware.cs`](./Security/RequireConfigApiKeyMiddleware.cs) | Path-scoped API-key gate (see below). **Must run before `MapConfigApiEndpoints`** in the request pipeline. |

Composition in the standalone `Program.cs` (and the pattern any embedding host should follow):

## `RequireConfigApiKeyMiddleware` ordering and behavior

- Registered as a **pipeline middleware** (not an authorization filter). It checks `HttpContext.Request.Path.StartsWithSegments("/api/config", …)` and **silently passes through any request outside that prefix**. If you relocate the API by passing a non-default prefix to `MapConfigApiEndpoints`, the middleware will not match it — `/api/config` is the hard-coded path check.
- When `ConfigApiSecurity.RequireApiKey == false`, the middleware short-circuits to `_next` without inspecting headers. Toggling `RequireApiKey` to `true` requires the host to configure a non-empty `ApiKey`; otherwise matching requests get **`500 Internal Server Error`** with `{ "detail": "API key enforcement is enabled but no server key has been configured." }`.
- When enabled, the middleware accepts the secret via **`X-Api-Key: <value>`** *or* **`Authorization: Bearer <value>`** (other schemes are rejected). Comparison uses `CryptographicOperations.FixedTimeEquals` over UTF-8 bytes; missing / empty / mismatching credentials produce **`401 Unauthorized`** with no body.
- Place this middleware **after** any TLS termination / proxy header middleware and **before** any logging that might leak request bodies, since it always returns before the endpoint runs on rejection.

## Security options (`ConfigApiSecurityOptions`, section `ConfigApiSecurity`)

| Key | Type | Default | Purpose |
| ------------------------------------- | -------- | ------- | ---------------------------------------------------------------------------------------- |
| **`ConfigApiSecurity:RequireApiKey`** | `bool` | `false` | Master switch. When `false`, all routes are anonymous and the middleware is a no-op. |
| **`ConfigApiSecurity:ApiKey`** | `string` | `""` | Shared secret compared in constant time. Must be non-empty when `RequireApiKey == true`. |

> **Note:** there is no authorization policy or scopes/role check inside this project — only the constant-time secret comparison above. If you need finer-grained access control,
> register your own auth middleware **before** `RequireConfigApiKeyMiddleware`, or replace it entirely.

## Hosting options (`ConfigApiHostingOptions`, section `ConfigApiHosting`)

| Key | Type | Default | Purpose |
| ------------------------------------------------------- | ------ | ------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| **`ConfigApiHosting:PollIntervalAdvisoryMilliseconds`** | `int?` | `null` | When > 0, emitted on every resolve response as the **`X-Config-Poll-Interval-Ms`** header. Purely advisory — clients are free to ignore it. |

## How routes map to `Lyo.Config`

All API traffic for app config uses a single store entity type **`App`** (`AppConfigEntity.AppEntityType`).

| URL segment | Meaning |
| --------------- | ---------------------------------------------------------------------------------------------------------- |
| **`{appKind}`** | Taxonomy for the process (e.g. `api`, `gateway`, `worker`). Lowercase slug: letters, digits, `-`, `_`, `.` |
| **`{appId}`** | Instance id (e.g. `checkout`, `550e8400-e29b-41d4-a716-446655440000`). Same slug rules after URL decode. |

Persisted compound id:

```text
EntityType = "App"
EntityId = "{appKind}:{appId}" // e.g. gateway:prod-west
```

Definitions you create with **`PUT /manage/definitions`** should use **`forEntityType`: `"App"`**. Bindings must use the same **`App`** + that compound **`forEntityId`**, or use
the manage routes below.

## Runtime: resolve and poll

Base path (default): **`/api/config`**.

## Runtime: resolve and poll — `GET`, `HEAD`, `POST` — `/{appKind}/{appId}`

- **`HEAD`**: same **`ETag`** / **304** behaviour, no body on 200.
- **`POST`**: same body as **`GET`** when you prefer not to put long ids in query strings.

## Management (`/api/config/manage`)

Requires the same auth as the rest of **`/api/config`** when **`ConfigApiSecurity.RequireApiKey`** is true (`X-Api-Key` or `Authorization: Bearer`).

| Method | Path | Notes |
| ------ | -------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| GET | `/definitions` | Lists definitions for **`App`**. |
| PUT | `/definitions` | Body: `ConfigDefinitionRecord` (`forEntityType` should be **`App`**). |
| DELETE | `/definitions/{definitionId}` | |
| PUT | `/bindings` | Body: `ConfigBindingRecord` (`forEntityType` **`App`**, `forEntityId` **`kind:id`** e.g. `gateway:prod-west`). |
| DELETE | `/bindings/{bindingId}` | |
| GET | `/bindings/{bindingId}/revisions` | |
| POST | `/bindings/{bindingId}/revert` | Body: `{ "revision": <int> }` |
| GET | `/apps/{appKind}/{appId}/bindings` | Convenience list for one app identity. |
| GET | `/apps/{appKind}/{appId}/bindings/{key}/revisions` | |
| POST | `/apps/{appKind}/{appId}/bindings/{key}/revert` | Body: `{ "revision": <int> }` |

## Configuration (`appsettings`)

- **`PostgresConfig`**: connection string and migrations for [`Lyo.Config.Postgres`](../../../Features/Config/Lyo.Config.Postgres/README.md).
- **`ConfigApiHosting`**: optional **`PollIntervalAdvisoryMilliseconds`** (see *Host embedding → Hosting options* above).
- **`ConfigApiSecurity`**: **`RequireApiKey`**, **`ApiKey`** (see *Host embedding → Security options* above).

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

Bind **`ConfigApi`** in configuration for **`BaseUrl`**, optional **`ApiKey`**, **`PollInterval`**, etc. ([
`ConfigApiClientOptions`](../Lyo.Config.Api.Client/ConfigApiClientOptions.cs)). More examples: [`Lyo.Config.Api.Client/README.md`](../Lyo.Config.Api.Client/README.md).

## Local run

```bash
dotnet run --project Lyo.Net/Apps/Config/Lyo.Config.Api/Lyo.Config.Api.csproj
```

Development OpenAPI document: **`/openapi/v1.json`** (ASP.NET convention). Scalar UI is mapped when the environment is **Development**.

## See also

- Feature docs: [`Lyo.Config/README.md`](../../../Features/Config/Lyo.Config/README.md)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication` — (direct, lyo)
- `Lyo.Authentication.AspNetCore` — (direct, lyo)
- `Lyo.Authentication.Google` — (direct, lyo)
- `Lyo.Authentication.Keycloak` — (direct, lyo)
- `Lyo.Authentication.OpenIdConnect` — (direct, lyo)
- `Lyo.Authentication.Postgres` — (direct, lyo)
- `Lyo.Config` — (direct, lyo)
- `Lyo.Config.Postgres` — (direct, lyo)
- `Lyo.EntityReference.Models` — (direct, lyo)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` — (direct, microsoft)
- `Scalar.AspNetCore` `2.16.11` — (direct, third-party)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.EntityReference.Postgres` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Postgres` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft)