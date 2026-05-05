# Lyo.Config.Api

HTTP host for central **app** configuration backed by PostgreSQL and [`Lyo.Config`](../../Features/Config/Lyo.Config/). Microservices resolve merged config per deployment identity and poll using **ETags** or an optional **`version`** query mirror.

Resolution contracts (**`ConfigResolveConditionalResult`**) live in **[`Lyo.Config.Api.Models`](../Lyo.Config.Api.Models/README.md)**. The HTTP typed client and **`AddConfigApiClientFromConfiguration`** live in **`Lyo.Config.Api.Client`** ([readme](../Lyo.Config.Api.Client/README.md)). Route slug → **`EntityRef`** mapping uses **`AppConfigEntity`** from **`Lyo.Config`**. Polling plus **`IOptionsMonitor<T>`** is **[`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting/README.md)**.

## How routes map to `Lyo.Config`

All API traffic for app config uses a single store entity type **`App`** (`AppConfigEntity.AppEntityType`).

| URL segment | Meaning |
|-------------|---------|
| **`{appKind}`** | Taxonomy for the process (e.g. `api`, `gateway`, `worker`). Lowercase slug: letters, digits, `-`, `_`, `.` |
| **`{appId}`** | Instance id (e.g. `checkout`, `550e8400-e29b-41d4-a716-446655440000`). Same slug rules after URL decode. |

Persisted compound id:

```text
EntityType = "App"
EntityId   = "{appKind}:{appId}"    // e.g. gateway:prod-west
```

Definitions you create with **`PUT /manage/definitions`** should use **`forEntityType`: `"App"`**. Bindings must use the same **`App`** + that compound **`forEntityId`**, or use the manage routes below.

## Runtime: resolve and poll

Base path (default): **`/api/config`**.

### `GET`, `HEAD`, `POST` — `/{appKind}/{appId}`

Returns the resolved **`ResolvedConfigRecord`** JSON (camelCase, same options as `ConfigJsonSerializerOptions.Default`).

- **`HEAD`**: same **`ETag`** / **304** behaviour, no body on 200.
- **`POST`**: same body as **`GET`** when you prefer not to put long ids in query strings.

**Conditional requests (no body when unchanged):**

1. **`If-None-Match: "<sha256-hex>"`** (or the full quoted value from **`ETag`**)
2. **`?version=<sha256-hex>`** — same comparison as the fingerprint inside the quoted **`ETag`** (strip quotes / weak prefix if present)

Omit both to always load the latest snapshot.

**Response headers**

- **`ETag`**: opaque strong tag (quoted hex digest of a canonical JSON fingerprint).
- **`Cache-Control`**: `private, max-age=0`
- **`X-Config-Poll-Interval-Ms`**: optional advisory from [`ConfigApiHosting`](./ConfigApiHostingOptions.cs) (`appsettings` section **`ConfigApiHosting`**).

**Errors**

- **`409`** Problem Details when required config keys are missing (`LoadConfigAsync` / `ValidateRequired`).

### Examples (curl)

Assume the app listens on `http://localhost:5088` and API key is off.

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

## Management (`/api/config/manage`)

Requires the same auth as the rest of **`/api/config`** when **`ConfigApiSecurity.RequireApiKey`** is true (`X-Api-Key` or `Authorization: Bearer`).

| Method | Path                                               | Notes                                                                                                          |
|--------|----------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
| GET    | `/definitions`                                     | Lists definitions for **`App`**.                                                                               |
| PUT    | `/definitions`                                     | Body: `ConfigDefinitionRecord` (`forEntityType` should be **`App`**).                                          |
| DELETE | `/definitions/{definitionId}`                      |                                                                                                                |
| PUT    | `/bindings`                                        | Body: `ConfigBindingRecord` (`forEntityType` **`App`**, `forEntityId` **`kind:id`** e.g. `gateway:prod-west`). |
| DELETE | `/bindings/{bindingId}`                            |                                                                                                                |
| GET    | `/bindings/{bindingId}/revisions`                  |                                                                                                                |
| POST   | `/bindings/{bindingId}/revert`                     | Body: `{ "revision": <int> }`                                                                                  |
| GET    | `/apps/{appKind}/{appId}/bindings`                 | Convenience list for one app identity.                                                                         |
| GET    | `/apps/{appKind}/{appId}/bindings/{key}/revisions` |                                                                                                                |
| POST   | `/apps/{appKind}/{appId}/bindings/{key}/revert`    | Body: `{ "revision": <int> }`                                                                                  |

## Configuration (`appsettings`)

- **`PostgresConfig`**: connection string and migrations (`Lyo.Config.Postgres`).
- **`ConfigApiHosting`**: optional **`PollIntervalAdvisoryMilliseconds`**.
- **`ConfigApiSecurity`**: **`RequireApiKey`**, **`ApiKey`**.

See [`appsettings.json`](./appsettings.json).

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

Bind **`ConfigApi`** in configuration for **`BaseUrl`**, optional **`ApiKey`**, **`PollInterval`**, etc. ([`ConfigApiClientOptions`](../Lyo.Config.Api.Client/ConfigApiClientOptions.cs)). More examples: [`Lyo.Config.Api.Client/README.md`](../Lyo.Config.Api.Client/README.md).

## Local run

```bash
dotnet run --project Lyo.Net/Apps/Config/Lyo.Config.Api/Lyo.Config.Api.csproj
```

Development OpenAPI document: **`/openapi/v1.json`** (ASP.NET convention). Scalar UI is mapped when the environment is **Development**.

## See also

- Feature docs: [`Lyo.Config/README.md`](../../Features/Config/Lyo.Config/README.md)
