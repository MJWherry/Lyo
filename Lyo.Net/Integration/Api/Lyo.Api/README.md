# Lyo.Api

Core API library for building RESTful minimal APIs with Entity Framework Core. Provides a fluent `ApiEndpointBuilder` to generate CRUD endpoints with caching, **`ILyoMapper`**-based DTO mapping, validation, and per-endpoint authorization.

## Features

### Endpoint Builders

| Builder | Route style | Use case |
|---------|-------------|----------|
| **CreateBuilder** | Structured REST under `baseRoute`: `{baseRoute}/Query`, `{baseRoute}/QueryProject`, `{baseRoute}` + `GetDefaultEndpoint<TKey>()` for GET/DELETE by key (e.g. `{baseRoute}/{id:guid}`), etc. | Typed `TRequest` / `TResponse`, one registration per entity, **`ILyoMapper`** for DTO mapping, full CRUD control |
| **MapDynamicCrudEndpoints** | Dynamic segment `{entityType}` (entity CLR **name**): `{baseRoute}/{entityType}/Query`, `{baseRoute}/{entityType}/{id}`, …; also `GET {baseRoute}/Metadata` and `GET {baseRoute}/{entityType}/Metadata` | All (or filtered) EF model types on a single `DbContext`; JSON bodies deserialize to entities; no DTO layer |
| **CreateReadOnlyBuilder** | Same URL shape as **CreateBuilder** | `TRequest` is fixed to `object`; use `.WithReadOnlyEndpoints()` or `WithCrud(ApiFeatureFlag.ReadOnly, …)` so only **Query**, **QueryProject**, and **Get** are emitted (`ReadOnly` = `Query` \| `Get`) |

**Entry points:** `WebApplication` extension methods in [`ApiEndpointBuilderExtensions`](ApiEndpoint/ApiEndpointBuilderExtensions.cs) (`CreateBuilder`, `CreateReadOnlyBuilder`) and [`DynamicCrudEndpointBuilder`](ApiEndpoint/Dynamic/DynamicCrudEndpointBuilder.cs) (`MapDynamicCrudEndpoints`). `baseRoute` should not end with `/`; it is combined with route templates the same way as in the builders above.

**Typed vs dynamic:** **QueryHistory** (`{baseRoute}/QueryHistory`) exists only on the **CreateBuilder** pipeline when you configure `WithQueryHistory`. **MapDynamicCrudEndpoints** does not map temporal history endpoints. Dynamic CRUD uses `JsonNode` / runtime types for create/update/patch/upsert bodies; typed CRUD uses `TRequest` / `TResponse` throughout.

Full route list for the typed builder: [Endpoints](#endpoints) below.

### Query Engine

- **WhereClause** – Filter tree for `QueryReq` / `ProjectionQueryReq`, serialized as **`whereClause`**. JSON type discriminators are **`condition`** (leaf: field + comparison + value) and **`group`** (branch: AND/OR + children). Nested groups are unlimited depth. See [`Lyo.Query.Models` README](../../../Data/Query/Lyo.Query.Models/README.md) for the DTO shape.
- **SubQuery / two-phase** – Optional **`subClause`** on a node (or **`WhereClauseBuilder`** helpers such as **`AddSubClause`**) runs the root filter in the database and the nested clause in-memory on the filtered rows—useful for collection fields that are not efficiently expressible in SQL
- **16 comparators** – Equals, NotEquals, Contains, NotContains, StartsWith, EndsWith, NotStartsWith, NotEndsWith, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, In,
  NotIn, Regex, NotRegex
- **Include** – Multi-hop navigation (`contactaddresses.address` traverses two tables)
- **Keys** – Fetch by primary key(s): single `[[id]]` or composite `[[tenantId, id]]`
- **SortBy** – Multi-sort with direction and priority
- **TotalCountMode** – `Exact`, `None`, or `HasMore` (pagination optimization)
- **IncludeFilterMode** – `Full` (all related) or `MatchedOnly` (only items matching the filter)
- **Works on `IQueryable<T>`** – Not just EF Core; in-memory, custom providers, unit tests

### Projection (QueryProject) & SQL-Level Query Generation

`POST …/Query` returns **`QueryRes<T>`** (full entities or include-shaped graphs).  
`POST …/QueryProject` returns **`ProjectedQueryRes<T>`** (sparse rows: dictionaries, scalars, or zipped collection shapes). Only **`ProjectedQueryRes<T>`** carries projection metadata—see **`entityTypes`** below.

- **Select** – Sparse field selection; only requested fields returned
- **Nested paths** – `JobRuns.CreatedBy`, `contactaddresses.address.city`
- **Wildcards** – `Collection.*` (entire nested objects), `*` (root entity flattened)
- **Collection scalar projection** – `JobRuns.CreatedBy` → array of scalar arrays per row
- **MatchedOnly** – When filtering on nested fields, include only matched items in collections
- **SQL-level projection** – When possible, projects in the database (no full entity load); falls back to load-then-project for wildcards/subqueries. Both paths participate in [query result caching](#query-result-caching) the same way as **`POST …/Query`**.
- **Derived includes** – Select paths auto-include required navigation properties
- **Computed fields** – Optional **`ComputedFields`** on the request: SmartFormat templates add named columns from other projected values (requires **`IFormatterService`** and **`ApiFeatureFlag.ProjectionComputedFields`** on the endpoint). Placeholders use dotted paths that must be loadable from **`Select`**; the server may append missing paths for template dependencies and can strip those dependency leaves from the JSON response when they were only needed for formatting (see sibling-merge / auto-derived path behavior in projection services).
- **`entityTypes` (success only)** – On successful **`QueryProject`** responses, **`entityTypes`** is a sorted, distinct list of CLR **class** names for: the root entity type, and every navigation target (including collection **element** types) touched by **`Select`** and by computed-field **template** placeholders. Value types and strings are not listed. On failure, **`entityTypes`** is omitted or null. Regular **`Query`** does not include this property.

### CRUD & Bulk

- **Bulk with individual fallback** – Batch first; on failure, retry items individually; partial success returned
- **Property-level Patch** – Update only specified properties; optional **per-request property allowlists** via `PatchPropertyAuthorization` (typed and dynamic CRUD)
- **Delete by Keys or Query** – `DeleteRequest` supports `Keys` (primary keys) or `Query` (a **`WhereClause`**)
- **Upsert** – Create or update by key; supports `UpsertInheritCreate` / `UpsertInheritUpdate`

### Cross-Cutting

- **Caching** – Query and QueryProject result caching (FusionCache / Lyo.Cache), with optional typed UTF-8 payload entries when **`QueryOptions.CacheQueryResultsAsUtf8Payload`** is enabled; auto-invalidation on Create/Update/Patch/Delete/Upsert. Details: [Query result caching](#query-result-caching).
- **Authorization** – Builder-level and per-endpoint via `RequireAuthorization`, `AllowAnonymous`, `EndpointAuth`
- **CrudConfiguration** – Before/After hooks, per-operation auth; optional **patch property authorization** (policy-based allowlists or custom rules; disallowed keys return 403)
- **ApiFeatureFlag** – Fine-grained control over which endpoints are generated
- **ILyoMapper** – Request/response mapping abstraction (Mapster, AutoMapper, or hand-written). When source and destination types are identical, CRUD services **skip** the mapper and cast (`MapOrCast`).
- **Request validation** – Paging bounds (`Start` / `Amount` vs `QueryOptions`), bulk batch size vs `BulkOperationOptions`, **`PatchRequest`** property names and convertible values vs `TDbModel`, and query/projection **path validation** for filters, includes, and `Select` (invalid paths return structured API errors; see [Validation](#validation))
- **Export** – CSV, XLSX, JSON with optional SmartFormat column templates
- **QueryHistory** – Temporal/history query support
- **Errors** – Failures return **`LyoProblemDetails`** (RFC 7807–style problem details, including trace/span context when available)
- **OpenTelemetry** – `api.crud.duration`, `api.crud.requests`, structured logging, trace/span IDs in errors

### Blazor Components (Lyo.Web.Components)

- **LyoDataGrid** – Server-side grid with Query API; search, filter, sort, bulk export/patch/delete, auto-refresh
- **LyoDataGridProjected** – Projected variant with `LyoProjectedColumn`; uses QueryProject for sparse fields
- **BeforeQuery** – Hook to add includes, filters, etc. before grid loads

## Setup

Register services before building endpoints:

```csharp
// Required: query services, cache, ILyoMapper, DbContext factory
builder.Services.AddLocalCache();           // or AddFusionCache()
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoCrudServices<MyDbContext>();
builder.Services.AddDbContextFactory<MyDbContext>(...);
// Register ILyoMapper — any implementation (Mapster/AutoMapper/custom). Many samples use a thin Mapster adapter.
builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();

// Optional: export (required for WithExport)
builder.Services.WithExportService<MyDbContext>();
// Optional: PostgreSQL set-returning functions (ISprocService), Lyo.Diff helpers
// builder.Services.AddPostgresSprocService<MyDbContext>();
// builder.Services.AddLyoDiffServices();
```

### `ApiClientOptions` and integration HTTP clients

[`Lyo.Api.Client.ApiClientOptions`](../Lyo.Api.Client/ApiClientOptions.cs) is the shared configuration base for [`ApiClient`](../Lyo.Api.Client/ApiClient.cs) and for integration-specific option types (for example `LyoDiscordClientOptions`, `TypecastClientOptions`) so **`BaseUrl`**, **`EnsureStatusCode`**, **`AcceptEncodings`**, response decompression, and request compression bind under each integration’s configuration section with the same JSON shape as the top-level **`ApiClient`** section.

## Validation

Endpoints validate requests before running CRUD/query logic (failures surface as **`LyoProblemDetails`** with stable error codes):

| Area | What is checked |
|------|-----------------|
| **Paging** | `Start` / `Amount` against **`QueryOptions`** (defaults and max page size; export uses max export size where applicable) |
| **Bulk** | Number of items in bulk requests vs **`BulkOperationOptions.MaxAmount`** |
| **Patch** | JSON property keys exist and are writable on **`TDbModel`**; values convert to the property type (`PatchRequestPropertyValidator`) |
| **Query / QueryProject** | Filter **field** paths, **`Include`** segments, and projection **`Select`** paths against the EF model (cached **`QueryPathValidationCache`** + **`ProjectedQueryModelValidator`** for projected queries) |

Authorization and **patch property allowlists** run after patch key/value validation.

## Quick Start

```csharp
using Lyo.Api.ApiEndpoint;

app.CreateBuilder<MyDbContext, MyEntity, MyRequest, MyResponse, Guid>("/api/items", "Items")
    .WithCrud(crud => crud
        .WithFlags(ApiFeatureFlag.FullCrud)
        .CreateAuth(EndpointAuth.RequireRole("Editor")))
    .Build();
```

You can still pass a `CrudConfiguration<...>` record: `.WithCrud(ApiFeatureFlag.FullCrud, new CrudConfiguration<...> { ... })`.

## Endpoints

Routes emitted by **CreateBuilder** / **CreateReadOnlyBuilder** when the matching `With*` / `ApiFeatureFlag` configuration is enabled (17 HTTP endpoints when everything below is turned on).

| Method | Route | Description |
|--------|-------|-------------|
| POST | `{baseRoute}/Query` | Query with filters, includes, sort, pagination |
| POST | `{baseRoute}/QueryProject` | Projected query (`Select`); SQL-level projection when possible; optional computed fields when `ProjectionComputedFields` is set |
| POST | `{baseRoute}/QueryHistory` | Temporal / history query (`WithQueryHistory` only) |
| POST | `{baseRoute}/Export` | Export to CSV / XLSX / JSON (`IExportService` required) |
| GET | `{baseRoute}` + `GetDefaultEndpoint<TKey>()` | Get single entity (`?include=…`); route suffix is `/{id:guid}`, `/{id:int}`, `/{id}`, … depending on `TKey` |
| POST | `{baseRoute}` | Create |
| POST | `{baseRoute}/Bulk` | Bulk create |
| POST | `{baseRoute}/Update` | Full update |
| POST | `{baseRoute}/Bulk/Update` | Bulk update |
| PATCH | `{baseRoute}` | Property-level partial update |
| PATCH | `{baseRoute}/Bulk` | Bulk patch |
| POST | `{baseRoute}/Upsert` | Upsert |
| POST | `{baseRoute}/Bulk/Upsert` | Bulk upsert |
| DELETE | `{baseRoute}` + `GetDefaultEndpoint<TKey>()` | Delete by primary key |
| DELETE | `{baseRoute}` | Delete by body (`DeleteRequest` with `Keys` or `Query`) |
| DELETE | `{baseRoute}/Bulk` | Bulk delete |
| GET | `{baseRoute}/Metadata` | OpenAPI-style metadata for this group (`WithMetadata` / `ApiFeatureFlag.Metadata`) |

## ApiFeatureFlag

| Flag                  | Endpoints                                 |
|-----------------------|-------------------------------------------|
| `Query`               | Query, QueryProject                        |
| `Get`                 | Get                                       |
| `Create`              | Create                                    |
| `CreateBulk`          | Bulk create                               |
| `Update`              | Update                                    |
| `UpdateBulk`          | Bulk update                               |
| `Patch`               | Patch                                     |
| `PatchBulk`           | Bulk patch                                |
| `Delete`              | Delete                                    |
| `DeleteBulk`          | Bulk delete                               |
| `Upsert`              | Upsert                                    |
| `UpsertBulk`          | Bulk upsert                               |
| `Export`              | Export                                    |
| `Metadata`            | Metadata endpoint                         |
| `ProjectionComputedFields` | Enables **`ComputedFields`** on **`ProjectionQueryReq`** for **`/QueryProject`** (requires `Query` + registered **`IFormatterService`**) |
| `ReadOnly`            | Query, QueryProject, Get (`Query` \| `Get`) |
| `BasicCrud`           | Query, Get, Create, Update, Patch, Delete |
| `FullCrud`            | BasicCrud + Upsert                        |
| `BulkOperations`      | All bulk variants                         |
| `All`                 | All **standard** CRUD/export operations (`Query` … `Export`). Does **not** include **`Metadata`** or **`ProjectionComputedFields`**—add those flags explicitly when needed |
| `UpsertInheritCreate` | Upsert uses Create hooks                  |
| `UpsertInheritUpdate` | Upsert uses Update hooks                  |
| `PatchInheritsUpdate` | Patch uses Update hooks                   |

## Dynamic Endpoint Builder (MapDynamicCrudEndpoints)

Register CRUD endpoints for all entities in a DbContext with **dynamic routes** `{baseRoute}/{entityType}/…` (e.g. `POST /api/Job/Person/Query`, `GET /api/Job/Person/{id}` when `BaseRoute = "api/Job"`). `entityType` is the entity type’s CLR **name**. Uses entity-as-request-response (no DTOs) and infers primary key and default order from the EF model. **QueryHistory** is not mapped here—use the typed **CreateBuilder** if you need temporal queries.

For **per-entity routes with custom DTOs**, use `CreateBuilder` (see Quick Start).

### Fluent config (recommended)

```csharp
app.MapDynamicCrudEndpoints<JobContext>(c => c
    .WithDefaults(d => {
        d.BaseRoute = "api/Job";
        d.Features = ApiFeatureFlag.All | ApiFeatureFlag.UpsertInheritCreate | ApiFeatureFlag.UpsertInheritUpdate | ApiFeatureFlag.PatchInheritsUpdate;
    })
    .For<JobDefinition>(e => e
        .ExcludeCreate()
        .ForPatch(p => p.Before((ctx, entity) => entity.ModifiedAt = DateTime.UtcNow)))
    .For<JobRun>(e => e.ExcludeExport())
    .IncludeOnly<JobDefinition, JobRun>());
```

### DynamicEndpointOptions (simple overload)

```csharp
// All entities – single set of routes: /{entityType}/Query, /{entityType}/{id}, etc.
app.MapDynamicCrudEndpoints<PeopleDbContext>();

// With options: exclude xref tables
app.MapDynamicCrudEndpoints<PeopleDbContext>(o => o
    .Exclude<PersonRelationshipEntity>()
    .Exclude<ContactPhoneNumberEntity>());

// Whitelist: only specific types
app.MapDynamicCrudEndpoints<PeopleDbContext>(o => o
    .IncludeOnly<PersonEntity, AddressEntity, PhoneNumberEntity>());

// Custom base path
app.MapDynamicCrudEndpoints<PeopleDbContext>(o => o.BaseRoute = "/api");
```

### DynamicEndpointDefaults (fluent config)

| Property      | Default | Description                                  |
|---------------|---------|----------------------------------------------|
| BaseRoute     | ""      | Route prefix (e.g. "/api")                   |
| Features      | All     | ApiFeatureFlag for each entity               |
| IncludedTypes | []      | When non-empty, only these types (whitelist) |
| ExcludedTypes | []      | Types to exclude (e.g. xref tables)          |

### Per-entity overrides (EntityEndpointConfigBuilder)

| Method          | Description                           |
|-----------------|---------------------------------------|
| `ExcludeCreate` | Exclude Create endpoint               |
| `ExcludeUpdate` | Exclude Update, UpdateBulk            |
| `ExcludePatch`  | Exclude Patch, PatchBulk              |
| `ExcludeDelete` | Exclude Delete, DeleteBulk            |
| `ExcludeUpsert` | Exclude Upsert, UpsertBulk            |
| `ExcludeExport` | Exclude Export                        |
| `ExcludeQuery`  | Exclude Query, QueryProject            |
| `ExcludeGet`    | Exclude Get                           |
| `ForCreate`     | Configure Create hooks (Before/After) |
| `ForPatch`      | Configure Patch hooks                 |
| `ForUpdate`     | Configure Update hooks                |
| `ForDelete`     | Configure Delete hooks, Includes      |
| `ForUpsert`     | Configure Upsert hooks                |
| `ForExport`     | Configure Export (auth, etc.)         |

Routes: `{baseRoute}/{entityType}/Query`, `{baseRoute}/{entityType}/QueryProject`, `{baseRoute}/{entityType}/{id}`, etc. Entity type name is the route segment (e.g.
`JobDefinition`). Unknown `{entityType}` returns 404. Entities with composite keys are skipped.

### Metadata endpoint

For `MapDynamicCrudEndpoints`, `GET {baseRoute}/Metadata` returns all entity types and their structures. `GET {baseRoute}/{entityType}/Metadata` returns metadata for a single
entity:

```json
{
  "entityTypes": [
    {
      "entityType": "PersonEntity",
      "keyPropertyName": "Id",
      "keyType": "Guid",
      "properties": [
        { "name": "Id", "type": "Guid", "nullable": false },
        { "name": "Name", "type": "String", "nullable": true },
        { "name": "CreatedAt", "type": "DateTime", "nullable": false }
      ]
    }
  ]
}
```

For `CreateBuilder`, metadata is opt-in via `.WithMetadata()` or `ApiFeatureFlag.Metadata` and is exposed at `GET {baseRoute}/Metadata`.

By default it returns request/response metadata plus key metadata. Database entity metadata is only included when metadata was configured with `IncludeEntityMetadata = true`, for
example:

```csharp
app.CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>("/api/Job/Definition", "Job")
    .WithMetadata(new() { IncludeEntityMetadata = true })
    .Build();
```

Example response when entity metadata is enabled:

```json
{
  "entity": {
    "typeName": "JobDefinition",
    "properties": [
      { "name": "Id", "type": "Guid", "nullable": false }
    ]
  },
  "request": {
    "typeName": "JobDefinitionReq",
    "properties": [
      { "name": "Name", "type": "String", "nullable": false }
    ]
  },
  "response": {
    "typeName": "JobDefinitionRes",
    "properties": [
      { "name": "Id", "type": "Guid", "nullable": false }
    ]
  },
  "keyPropertyName": "Id",
  "keyType": "Guid"
}
```

## Builders

### CreateBuilder vs CreateReadOnlyBuilder

```csharp
// Full CRUD (request/response types)
app.CreateBuilder<MyDbContext, MyEntity, MyRequest, MyResponse, Guid>("/api/items", "Items")
    .WithCrud(crud => crud.WithFlags(ApiFeatureFlag.FullCrud))
    .Build();

// Read-only (no request type; use object for TRequest)
app.CreateReadOnlyBuilder<MyDbContext, MyEntity, MyResponse>("/api/items", "Items")
    .WithReadOnlyEndpoints()
    .Build();

// Entity-as-both (no DTOs; mapping skipped when TRequest = TDbModel or TDbModel = TResult)
app.CreateBuilder<MyDbContext, MyEntity, MyEntity, MyEntity, Guid>("/api/items", "Items")
    .WithCrud(crud => crud.WithFlags(ApiFeatureFlag.All))
    .Build();
```

### WithCrud vs individual With* methods

Use **`WithCrud(Action<CrudConfigurationBuilder<...>>)`** for hooks and per-operation auth in one place, or **`WithCrud(features, CrudConfiguration<...>)`** with a record initializer.

Alternatively, register operations with **`WithQuery`**, **`WithGet`**, **`WithCreate`**, etc. Each supports either delegate parameters, a config record, or a **fluent builder** (`Action<...EndpointConfigBuilder>`).

```csharp
app.CreateBuilder<...>("/api/items", "Items")
    .WithQuery(q => q.Auth(EndpointAuth.Anonymous()).ComputedFields())
    .WithGet()
    .WithCreate(c => c.Before(ctx => { }).After(ctx => { }).Auth(EndpointAuth.RequireRole("Editor")))
    .WithExport()
    .WithQueryHistory(h => h.TimeRange(e => e.StartTime, e => e.EndTime))
    .Build();
```

**Combined single/bulk helpers** (same hooks and auth wiring for both routes):

| Method | Registers |
|--------|-----------|
| `WithCreateAndBulk(Action<CreateEndpointConfigBuilder<...>>)` | Create + Bulk |
| `WithUpdateAndBulk(Action<UpdateEndpointConfigBuilder<...>>)` | Update + Bulk/Update |
| `WithPatchAndBulk(Action<PatchEndpointConfigBuilder<...>>)` | Patch + Bulk (auth, inherit-from-update, **property authorization**) |
| `WithUpsertAndBulk(Action<UpsertEndpointConfigBuilder<...>>)` | Upsert + Bulk/Upsert (`Auth` for single; **`BulkAuth`** optional override for bulk) |

Patch-only example:

```csharp
.WithPatchAndBulk(p => p
    .Auth(EndpointAuth.RequireAuthorization())
    .PropertyAuthorization(b => b.AllowPropertiesForPolicy("CanEditStatus", "Status")))
```

## CrudConfiguration

### Record initializer

```csharp
using Lyo.Api.ApiEndpoint;
using Lyo.Api.ApiEndpoint.Config;

var config = new CrudConfiguration<MyDbContext, MyEntity, MyRequest> {
    DeleteIncludes = ["RelatedItems"],
    // Lifecycle hooks (context-based: ctx.Entity, ctx.DbContext, ctx.Request, ctx.Services)
    BeforeGet = ctx => { },
    AfterGet = ctx => { },
    BeforeCreate = ctx => { },
    AfterCreate = ctx => { },
    BeforeUpdate = ctx => { },
    AfterUpdate = ctx => { },
    BeforePatch = ctx => { },
    AfterPatch = ctx => { },
    BeforeUpsert = ctx => { },
    AfterUpsert = ctx => { },
    BeforeDelete = ctx => { },
    AfterDelete = ctx => { },
    // Per-endpoint auth (null = builder default)
    QueryAuth = EndpointAuth.Anonymous(),
    GetAuth = EndpointAuth.Anonymous(),
    CreateAuth = EndpointAuth.RequireRole("Editor"),
    UpdateAuth = EndpointAuth.RequireAuthorization("AdminOnly"),
    PatchAuth = EndpointAuth.RequireAuthorization(),
    PatchBulkAuth = EndpointAuth.RequireAuthorization(),
    DeleteAuth = EndpointAuth.RequireAuthorization("AdminOnly"),
    ExportAuth = null,
    // Optional: which JSON property names may be patched (policy union; "*" = all keys when policy passes)
    PatchPropertyAuthorization = PatchPropertyAuthorization.ForPolicies(b => b
        .AllowPropertiesForPolicy("CanEditAll", "*")
        .AllowPropertiesForPolicy("CanEditStatus", "Status"))
};
```

### Fluent `CrudConfigurationBuilder`

Same surface via **`WithCrud(crud => ...)`**: `WithFlags` (required), lifecycle `Before*` / `After*`, `DeleteIncludes`, `Metadata`, per-operation `*Auth` methods, and **`PatchPropertyAuthorization`** — either a built **`PatchPropertyAuthorization`** record or **`PatchPropertyAuthorization(b => b.AllowPropertiesForPolicy(...))`** for policy maps. For a fully custom rule, set **`PatchPropertyAuthorization`** on the record or assign **`PatchPropertyAuthorization.Custom`** in code.

## Authorization

### Builder-level

```csharp
app.CreateBuilder<...>("/api/items", "Items")
    .RequireAuthorization()                    // All endpoints require auth
    .RequireAuthorization("AdminOnly")         // Or specific policy
    .AllowAnonymous()                          // Or allow anonymous
    .WithCrud(crud => crud.WithFlags(ApiFeatureFlag.FullCrud))
    .Build();
```

### Per-endpoint (EndpointAuth)

| Method                                                           | Description                 |
|------------------------------------------------------------------|-----------------------------|
| `EndpointAuth.RequireAuthorization()`                            | Requires authenticated user |
| `EndpointAuth.RequireAuthorization("Policy1", "Policy2")`        | Requires specified policies |
| `EndpointAuth.RequireAuthorization(p => p.RequireRole("Admin"))` | Inline policy               |
| `EndpointAuth.RequireRole("Admin", "Editor")`                    | Role-based                  |
| `EndpointAuth.RequireClaim("scope", "write", "admin")`           | Claim-based                 |
| `EndpointAuth.RequireAuthenticatedUser()`                        | Authenticated user          |
| `EndpointAuth.RequireUserName("admin@example.com")`              | Specific user               |
| `EndpointAuth.Anonymous()`                                       | Anonymous access            |

When `EndpointAuth` is null for an endpoint, builder-level auth is used.

## Query & Request Builders

### QueryReqBuilder

```csharp
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Enums;

var query = QueryReqBuilder.New()
    .AddIncludes("Addresses", "PhoneNumbers")
    .AddQuery(b => b
        .Equals("Status", "Active")
        .AddAnd(inner => inner
            .GreaterThan("Age", 18)
            .Contains("Tags", "verified")))
    .AddSort("CreatedAt", SortDirection.Desc)
    .SetPagination(0, 20)
    .Build();

// Typed via For<T>()
var typed = QueryReqBuilder.New()
    .For<Person>()
    .Include(p => p.Addresses)
    .AddQuery(q => q.AddEquals(p => p.Status, "Active"))
    .Done()
    .Build();
```

Use **`ProjectionQueryReqBuilder`** when building **`ProjectionQueryReq`** (includes **`AddSelects`**, **`SetZipSiblingCollectionSelections`**, etc.).

### QueryRequest Keys (object[][])

`Keys` fetches specific entities by primary key. Each element is a key array:

- **Single-key**: `[[1], [2], [3]]` for ids 1, 2, 3
- **Composite-key**: `[["tenant-a", 1], ["tenant-b", 2]]` for (TenantId, Id)

### Projection (QueryProject)

`POST {baseRoute}/QueryProject` accepts a **`ProjectionQueryReq`** body and returns a **`ProjectedQueryRes<T>`** envelope (not the same shape as **`QueryRes<T>`** from **`/Query`**). The response echoes the request in **`queryRequest`** (including **`Select`** as executed—computed-field dependencies may have been merged server-side). On success, **`entityTypes`** lists CLR entity class names involved in the projection (root + navigations + template paths); see [Projection (QueryProject) & SQL-Level Query Generation](#projection-queryproject--sql-level-query-generation) above.

Use **`Select`** to specify which fields to return. Supports dotted paths and wildcards.

#### Computed fields

Optional **`ComputedFields`**: each entry has **`name`** (output column) and **`template`** (SmartFormat string). Placeholders reference projected paths, e.g. `"{LastName}, {FirstName}"`, `"{contactaddresses.address.city}"`, or a single bare dotted path as the whole template. Enable the feature with **`ApiFeatureFlag.ProjectionComputedFields`** and register **`IFormatterService`**. Template placeholders contribute to **`entityTypes`** the same way **`Select`** paths do.

Example:

```json
{
  "Start": 0,
  "Amount": 20,
  "Select": ["Id", "Name", "contactaddresses.address.city"],
  "ComputedFields": [
    { "name": "Label", "template": "{Name} — {contactaddresses.address.city}" }
  ]
}
```

#### Example success envelope (shape)

```json
{
  "queryRequest": { "select": ["Id", "Name"], "computedFields": [], "include": [], "sortBy": [], "options": { "totalCountMode": "Exact", "includeFilterMode": "Full" } },
  "isSuccess": true,
  "items": [{ "id": "…", "name": "Alice" }],
  "start": 0,
  "amount": 1,
  "total": 42,
  "hasMore": false,
  "queryScore": 0,
  "error": null,
  "entityTypes": ["PersonEntity", "ContactAddressEntity", "AddressEntity"]
}
```

**Select fields** – Return only specified fields:

```json
{
  "Start": 0,
  "Amount": 10,
  "Select": ["Id", "Name", "Email"],
  "whereClause": { "$type": "condition", "field": "Status", "comparison": "Equals", "value": "Active" }
}
```

Response: `[{ "Id": "...", "Name": "Alice", "Email": "alice@example.com" }, ...]`

**Nested paths** – Project scalar values from collections:

```json
{
  "Select": ["JobRuns.CreatedBy"],
  "whereClause": { "$type": "condition", "field": "Id", "comparison": "Equals", "value": "..." }
}
```

Response: `[["user-1", "user-2"], ...]` (array of scalar arrays per row)

**Sibling fields on the same collection** – When several `Select` paths share one collection prefix (e.g. `DocketCharges.Code` and `DocketCharges.Number`), the API can either zip them into a single array of objects under that prefix (`DocketCharges: [{ "Code": "...", "Number": "..." }, ...]`) or keep one column per path (parallel arrays). Control this with `options.zipSiblingCollectionSelections`: omit or `true` to zip (default), `false` for parallel columns. SQL projection, computed fields, MatchedOnly includes, and caching still apply; only the final row shape changes.

**Wildcard** – Project entire nested objects:

```json
{
  "Select": ["JobRuns.*"],
  "whereClause": { "$type": "condition", "field": "Id", "comparison": "Equals", "value": "..." }
}
```

Response: `[[{ "Id": "...", "CreatedBy": "user-1", "State": "..." }, ...], ...]`

**Root wildcard** – Flatten root entity to a single object:

```json
{"Select": ["*"], "Start": 0, "Amount": 1}
```

Response: `[{ "Id": "...", "Name": "...", "CreatedAt": "..." }, ...]` (no `*` key; properties at root)

**IncludeFilterMode: MatchedOnly** – When filtering on nested fields (e.g. `contactemailaddresses.emailaddress.email`), include only matched items in collections. Example for **`/QueryProject`** (includes are derived from `Select` / filter paths—do not rely on `Include` here):

```json
{
  "options": { "includeFilterMode": "MatchedOnly" },
  "select": ["contactemailaddresses.emailaddress.email"],
  "whereClause": {
    "$type": "group",
    "operator": "Or",
    "children": [
      { "$type": "condition", "field": "contactemailaddresses.emailaddress.email", "comparison": "EndsWith", "value": "@gmail.com" },
      { "$type": "condition", "field": "contactemailaddresses.emailaddress.email", "comparison": "EndsWith", "value": "@yahoo.com" }
    ]
  }
}
```

For **`/Query`**, you can add an `"include"` array alongside `"whereClause"` as usual.

Returns only emails matching `@gmail.com` or `@yahoo.com`; excludes `@charter.net` etc.

### QueryRequestOptions

| Property          | Default | Description                                                               |
|-------------------|---------|---------------------------------------------------------------------------|
| TotalCountMode    | Exact   | `Exact`, `None`, or `HasMore` (pagination optimization)                   |
| IncludeFilterMode | Full    | `Full` = all related items; `MatchedOnly` = only items matching the **whereClause** filter |

**`ProjectionQueryReq`** uses **`ProjectedQueryRequestOptions`**, which adds:

| Property                       | Default | Description |
|--------------------------------|---------|-------------|
| ZipSiblingCollectionSelections | `true`  | When `true`, sibling `Select` paths under the same collection are zipped into one array of objects; when `false`, each path stays a separate column (parallel arrays). |

### WhereClauseBuilder

```csharp
// Simple conditions
var node = WhereClauseBuilder.And()
    .Equals("Status", "Active")
    .GreaterThan("Age", 18)
    .Build();

// Nested AND/OR
var node = WhereClauseBuilder.And()
    .AddOr(or => or.Equals("Status", "Active").Equals("Status", "Pending"))
    .AddAnd(and => and.Contains("Tags", "verified").In("Region", "US", "CA"))
    .Build();

// Explicit grouped node (same as AddAnd/AddOr, but useful for clarity)
var grouped = WhereClauseBuilder.And()
    .AddGroupOr(g => g.Equals("Region", "US").Equals("Region", "CA"))
    .Build();
```

### SubQuery (two-phase execution)

Root conditions run in the database; the subquery runs in-memory on the filtered results. Use for collection fields (e.g. `Tags`) that aren't efficiently queryable in SQL.

**AddSubClause** – Attach a nested clause to the current group (root runs in the database; nested filter can run in-memory):

```csharp
var node = WhereClauseBuilder.And()
    .Equals("Age", 10)
    .AddSubClause(sub => sub.AddAnd(s => s.Equals("Name", "Alice")))
    .Build();
```

**AddConditionWithSubClause** – Attach sub-clause to a specific condition:

```csharp
var node = WhereClauseBuilder.And()
    .AddConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 5, sub => sub.Equals("Name", "B"))
    .Build();
```

### DeleteRequestBuilder, PatchRequestBuilder

```csharp
using Lyo.Api.Models.Builders;

var deleteReq = DeleteRequestBuilder.New()
    .WithKey(id)
    .WithKey(tenantId, userId)
    .Build();

var patchReq = PatchRequestBuilder.New()
    .WithKey(id)
    .SetProperty("Status", "Archived")
    .Build();
```

### BeforeQuery hook

```csharp
<LyoDataGrid BeforeQuery="@(query => query.AddIncludes("Addresses", "PhoneNumbers"))" ... />
```

## Export

Export requires `WithExportService<TContext>()` and `WithExport` or `WithCrud` with `ApiFeatureFlag.Export`.

```csharp
// ExportRequest
{
  "query": { /* QueryRequest */ },
  "format": "Csv" | "Xlsx" | "Json",
  "columns": {
    "Email Address": "Email",
    "Full Name": "{FirstName} {LastName}",
    "Created": "{CreatedAt:yyyy-MM-dd}"
  }
}
```

When `IFormatterService` is registered, values with `{` are SmartFormat templates.

## Query History

For temporal/history data:

```csharp
app.CreateBuilder<...>("/api/items", "Items")
    .WithQueryHistory(e => e.StartTime, e => e.EndTime)
    .Build();
```

## Query result caching

**`POST …/Query`** and **`POST …/QueryProject`** both use the same **`IQueryService`** pipeline and the same **`QueryOptions`** singleton. Cached entries are keyed from the request (filters, paging, includes/sort for Query; plus projection **`Select`**, computed fields, and projected row-shape flags for QueryProject — see **`QueryCacheKeyBuilder`**).

- **Default (`CacheQueryResultsAsUtf8Payload` = `false`)** — **`GetOrSetAsync`** stores **`QueryRes<T>`** / **`ProjectedQueryRes<T>`** with Fusion’s usual serialization for CLR graphs.
- **Typed payload (`CacheQueryResultsAsUtf8Payload` = `true`)** — **`GetOrSetPayloadAsync<T>`** stores framed bytes via **`ICachePayloadSerializer`** and **`ICachePayloadCodec`** (optional compress/encrypt under **`CacheOptions:Payload`**). The SQL-level QueryProject path and the load-then-project fallback both use this mode when enabled; fallback goes through **`QueryCore`**, which already applies the same flag.

With typed payloads, **`CacheOptions:Payload`** can **`AutoCompress`** (above **`AutoCompressMinSizeBytes`**) and, on supported targets, **`AutoEncrypt`** (with **`EncryptionKeyId`** and **`IEncryptionService`**). Those steps run **before** the entry is written to the cache implementation. For a **distributed backplane** (e.g. **Redis** via Fusion’s secondary layer), that means **fewer bytes cross the network** on every cache read/write, which **cuts backplane latency** and bandwidth versus storing large uncompressed JSON blobs. For typical repetitive JSON query results, **lossless compression alone often shrinks stored size on the order of ~90%**, so the win is largest when the API tier and Redis are not co-located or when payload sizes are large.

Register cache before **`AddLyoQueryServices`** / **`AddLyoCrudServices`**. **`AddLyoQueryServices`** registers **`ICachePayloadSerializer`** to match **`JsonOptions`**, so cached payloads stay consistent with API JSON.

### Invalidation on writes (built-in CRUD)

When query caching is enabled, successful writes call **`ICacheService.InvalidateQueryCacheAsync<TDbModel>()`**, which removes cache entries associated with the tag **`entity:{lowercased type name}`**. This runs from the standard **`CreateService`**, **`UpdateService`**, **`PatchService`**, **`DeleteService`**, and **`UpsertService`** pipelines — including bulk operations that complete successfully for the affected entity.

How tags attach to cached reads (see **`QueryService.QueryCore`** and **`Get`** overloads):

- **`POST …/Query`** with **`Include`**, and **`GET …/{id}`** with **`includes`**: each cached result is tagged with **`entity:{root}`** plus **`entity:{type}`** for every EF entity type returned by **`loaderService.GetReferencedTypes`** for those include paths. So **`InvalidateQueryCacheAsync<AddressEntity>()`** invalidates not only Address-only queries but also **cached parent queries** (e.g. Person) that **included** Address — the child tag is on the same cache entry.
- **`POST …/QueryProject`**: the **SQL projection** path uses the same **`GetReferencedTypes`** tagging as **`/Query`** for derived include paths from **`Select`** / **`Where`** (plus **`queries`**, **`queryproject`**, **`entities`**, and root **`entity:{T}`**). The **fallback** path goes through **`QueryCore`** and uses the same pattern. A write on a **related** entity type therefore invalidates matching **`QueryProject`** cache entries as well.

| Concern | Behavior |
|--------|----------|
| **Patch / Upsert / Update / Create / Delete** | **`InvalidateQueryCacheAsync<T>()`** for the written entity type after success |
| **Unrelated root entity** (e.g. Order vs Person) | **Not** invalidated — different `entity:` tags |
| **Child entity** updated via **its** endpoint | Clears that type’s tag — **does** invalidate **parent `GET`/`Query`/`QueryProject`** caches whose stored tags include that **`entity:{child}`** (includes / projected navigation paths) |

For extra fan-out (custom rules, raw SQL, or third-party writers), use **Before/After** hooks or **`InvalidateCacheItemByTag`** / **`InvalidateAllCachedQueriesAsync`** as needed.

Example (see also **`Lyo.TestApi`** `appsettings.json`):

```json
{
  "QueryOptions": {
    "CacheQueryResultsAsUtf8Payload": true
  },
  "CacheOptions": {
    "Payload": {
      "AutoCompress": true,
      "AutoCompressMinSizeBytes": 1024
    }
  }
}
```

Optional (**.NET 10+**): set **`AutoEncrypt`** to **`true`** and **`EncryptionKeyId`** when **`IEncryptionService`** is registered, so payloads are encrypted after compression and before they reach the backplane (defense in depth alongside TLS in transit). See **`CacheOptions:Payload`** in [Lyo.Cache README](../../../Core/Cache/Lyo.Cache/README.md).

Further detail: [Lyo.Cache README](../../../Core/Cache/Lyo.Cache/README.md).

## Options

### QueryOptions (singleton)

| Property                            | Default | Description |
|-------------------------------------|---------|-------------|
| DefaultPageSize                     | 100     | Default page size |
| MaxPageSize                         | 2000    | Max page size |
| MinPagingStart                      | 0       | Minimum `Start` (inclusive) |
| MaxPagingStart                      | 10000000 | Maximum `Start` (inclusive) |
| MinPagingAmount                     | 1       | Minimum `Amount` when set |
| MaxExportSize                       | 5000    | Max rows for export |
| EnableSplitQueries                  | true    | Split queries for includes |
| UseNoTrackingWithIdentityResolution | true    | NoTracking for reads |
| AllowSelectWildcards                | true    | Allow terminal `*` in QueryProject `Select` paths |
| CacheQueryResultsAsUtf8Payload      | false   | Use typed payload cache (`GetOrSetPayloadAsync`) for Query and QueryProject instead of CLR `GetOrSetAsync` |

### BulkOperationOptions (singleton)

| Property               | Default | Description             |
|------------------------|---------|-------------------------|
| MaxAmount              | 2000    | Max items per bulk      |
| MaxDegreeOfParallelism | 10      | Parallelism             |
| UseParallelProcessing  | true    | Use parallel processing |
| Timeout                | 5 min   | Bulk timeout            |

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Api.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.AspNetCore.Authorization` | `[10,)` |
| `Microsoft.AspNetCore.Http.Abstractions` | `2.*` |
| `Microsoft.AspNetCore.OpenApi` | `[10,)` |
| `Microsoft.EntityFrameworkCore` | `[10,)` |
| `Microsoft.EntityFrameworkCore.Analyzers` | `[10,)` |
| `Microsoft.EntityFrameworkCore.Relational` | `[10,)` |
| `Microsoft.Extensions.Hosting.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10.0.1,)` |

### Project references

- `Lyo.Api.Models`
- `Lyo.Cache`
- `Lyo.Csv`
- `Lyo.Diff`
- `Lyo.Formatter`
- `Lyo.Metrics`
- `Lyo.Query`
- `Lyo.Xlsx`

## Public API (generated)

Top-level `public` types in `*.cs` (*75*). Nested types and file-scoped namespaces may omit some entries.

- `ApiEndpointBuilder`
- `ApiEndpointBuilderExtensions`
- `ApiErrorResponseFactory`
- `ApiFeatureFlag`
- `BaseService`
- `BulkOperationOptions`
- `CreateConfigBuilder`
- `CreateEndpointConfigBuilder`
- `CreateService`
- `CrudConfigurationBuilder`
- `DeleteConfigBuilder`
- `DeleteEndpointConfigBuilder`
- `DeleteService`
- `DynamicCrudEndpointBuilder`
- `DynamicEndpointConfig`
- `DynamicEndpointConfigBuilder`
- `DynamicEndpointDefaults`
- `DynamicEndpointMapper`
- `DynamicEndpointOptions`
- `EntityEndpointConfig`
- `EntityEndpointConfigBuilder`
- `EntityLoaderService`
- `ExportConfigBuilder`
- `ExportEndpointConfigBuilder`
- `ExportService`
- `Extensions`
- `GetEndpointConfigBuilder`
- `ICreateService`
- `IDeleteService`
- `IEntityLoaderService`
- `IExportService`
- `ILyoMapper`
- `ILyoRepository`
- `Info`
- `IPatchService`
- `IProjectionService`
- `IQueryHistoryService`
- `IQueryPagingHelper`
- `IQueryPathExecutor`
- `IQueryService`
- `IsExternalInit`
- `ISprocService`
- `ITypeConversionService`
- `IUpdateService`
- `IUpsertService`
- `Job`
- `LoggingMiddleware`
- `LyoRepository`
- `MetadataEndpointConfigBuilder`
- `PatchConfigBuilder`
- `PatchEndpointConfigBuilder`
- `PatchPropertyAuthorizationApplier`
- `PatchPropertyAuthorizationBuilder`
- `PatchPropertyAuthorizationResult`
- `PatchService`
- `PostgresSprocService`
- `ProjectionService`
- `QueryCacheKeyBuilder`
- `QueryEndpointConfigBuilder`
- `QueryHistoryEndpointConfigBuilder`
- `QueryHistoryService`
- `QueryKeyExpressionBuilder`
- `QueryOptions`
- `QueryPagingHelper`
- `QueryPathExecutor`
- `QueryService`
- `ServiceCollectionExtensions`
- `StoredProcedures`
- `TypeConversionService`
- `UpdateConfigBuilder`
- `UpdateEndpointConfigBuilder`
- `UpdateService`
- `UpsertConfigBuilder`
- `UpsertEndpointConfigBuilder`
- `UpsertService`

<!-- LYO_README_SYNC:END -->

