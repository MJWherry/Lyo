# Lyo.Api

Minimal-API library that maps EF Core entities to REST CRUD with caching, `ILyoMapper` DTO mapping, validation, and per-endpoint authorization.

Registration methods live on `ServiceCollectionExtensions` (`AddLyoQueryServices`, `AddLyoCrudServices`, optional export/sproc/diff). Map routes with `ApiEndpointBuilder` and `ApiEndpointBuilderExtensions` (`CreateBuilder`, `CreateReadOnlyBuilder`), or `DynamicCrudEndpointBuilder.MapDynamicCrudEndpoints` for one `{entityType}` route tree per context. DTOs and HTTP contracts live in `Lyo.Api.Models`. When `GenerateDocumentationFile` is on in `Directory.Build.props`, IntelliSense uses the same XML summaries as this package doc. CRUD services use `<inheritdoc />` against the `I*Service` interfaces.

## Features

- **WhereClause.** Filter tree for `QueryConcreteReq` / `ProjectionQueryReq`, serialized as `whereClause`. JSON discriminators are `condition` (leaf: field, comparison, value) and `group` (AND/OR children).
- **SubQuery / two-phase.** Optional `subClause` on a node, or `WhereClauseBuilder.AddSubClause`, runs the root filter in the database and the nested filter in memory.
- **16 comparators.** Equals, NotEquals, Contains, NotContains, StartsWith, EndsWith, NotStartsWith, NotEndsWith, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, In, and the remaining operators on `ComparisonOperatorEnum`.
- **Include.** Multi-hop navigation (`contactaddresses.address` traverses two tables).
- **Keys.** Fetch by primary key: single `[[id]]` or composite `[[tenantId, id]]`.
- **SortBy.** Multi-sort with direction and priority.
- **TotalCountMode.** `Exact`, `None`, or `HasMore` for pagination.
- **IncludeFilterMode.** `Full` (all related) or `MatchedOnly` (only items matching the filter).
- **`IQueryable<T>`.** Runs beyond EF Core: in-memory, custom providers, unit tests.
- **Select.** Sparse field selection. Only requested fields are returned.
- **Nested paths.** `JobRuns.CreatedBy`, `contactaddresses.address.city`.
- **Wildcards.** `Collection.*` (entire nested objects), `*` (root entity flattened).
- **Collection scalar projection.** `JobRuns.CreatedBy` becomes an array of scalar arrays per row.
- **MatchedOnly.** When filtering on nested fields, collections keep only matched items.
- **SQL-level projection.** Projects in the database when it can, so it does not load the whole entity. Falls back to load-then-project for wildcards and subqueries. Both paths participate in caching.
- **Derived includes.** Select paths add the navigation properties they need.
- **Computed fields.** Optional `ComputedFields` on the request. SmartFormat templates add named columns from other projected values. Needs `IFormatterService` and `ApiFeature.ProjectionComputedFields`.
- **`entityTypes` (success only).** On successful `QueryProject` responses, `entityTypes` is a sorted, distinct list of CLR class names for the root entity and every navigation or template path involved.
- **Bulk with individual fallback.** Batch first. On failure, retry items one by one. Partial success is returned.
- **Property-level Patch.** Update only specified properties. Optional per-request property allowlists via `PatchPropertyAuthorization` (typed and dynamic CRUD).
- **Delete by Keys or Query.** `DeleteRequest` accepts `Keys` (primary keys) or `Query` (a `WhereClause`).
- **Upsert.** Create or update by key. Supports `UpsertInheritCreate` / `UpsertInheritUpdate`.
- **Caching.** Query and QueryProject result caching (FusionCache / Lyo.Cache). Optional typed UTF-8 payload entries when `QueryOptions.CacheQueryResultsAsUtf8Payload` is true.
- **Authorization.** Builder-level and per-endpoint via `RequireAuthorization`, `AllowAnonymous`, `EndpointAuth`.
- **CrudConfiguration.** Before/After hooks and per-operation auth. Optional patch property authorization (policy allowlists or custom rules). Disallowed keys return 403.
- **ApiFeature / ApiFeatureSet.** Choose which endpoints to generate.
- **ILyoMapper.** Request/response mapping (Mapster, AutoMapper, or hand-written). When source and destination types are identical, CRUD services skip the mapper.
- **Request validation.** Paging bounds (`Start` / `Amount` vs `QueryOptions`), bulk batch size vs `BulkOperationOptions`, `PatchRequest` property names and convertible values.
- **Export.** CSV, XLSX, JSON with optional SmartFormat column templates.
- **Errors.** Failures return `LyoProblemDetails` (RFC 7807 problem details, including trace/span context when available).
- **OpenTelemetry.** `api.crud.duration`, `api.crud.requests`, structured logging, trace/span IDs in errors.
- **LyoDataGrid.** Server-side grid against the Query endpoints. Search, filter, sort, bulk export/patch/delete, auto-refresh.
- **LyoDataGridProjected.** Projected variant with `LyoProjectedColumn`. Uses QueryProject for sparse fields.
- **BeforeQuery.** Hook to add includes, filters, and similar before the grid loads.

## Examples

### Setup

```csharp
// Required: query services, cache, ILyoMapper, DbContext factory
builder.Services.AddLocalCache(); // or AddFusionCache()
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoCrudServices<MyDbContext>();
builder.Services.AddDbContextFactory<MyDbContext>(...);
// Register ILyoMapper — any implementation (Mapster/AutoMapper/custom). Many samples use a thin Mapster adapter.
builder.Services.AddScoped<ILyoMapper, MapsterLyoMapper>();

// Optional: export endpoint + IExportService (Lyo.Api.Export); add format handlers separately
builder.Services.AddLyoApiExport<MyDbContext>();
builder.Services.AddCsvExport(); // Lyo.Api.Export.Csv + AddCsvService()
builder.Services.AddXlsxExport(); // Lyo.Api.Export.Xlsx + AddXlsxService()
// Optional: PostgreSQL set-returning functions (ISprocService), Lyo.Diff helpers
// builder.Services.AddPostgresSprocService<MyDbContext>();
// builder.Services.AddLyoDiffServices();
```

### Quick start

```csharp
using Lyo.Api.ApiEndpoint;

app.CreateBuilder<MyDbContext, MyEntity, MyRequest, MyResponse, Guid>("/api/items", "Items")
    .WithCrud(crud => crud
        .WithFlags(ApiFeatureSet.FullCrud)
        .CreateAuth(EndpointAuth.RequireRole("Editor")))
    .Build();
```

### Fluent config

```csharp
app.MapDynamicCrudEndpoints<JobContext>(c => c
    .WithDefaults(d => {
        d.BaseRoute = "api/Job";
        d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
    })
    .For<JobDefinition>(e => e
        .ExcludeCreate()
        .ForPatch(p => p.Before((ctx, entity) => entity.ModifiedAt = DateTime.UtcNow)))
    .For<JobRun>(e => e.ExcludeExport())
    .IncludeOnly<JobDefinition, JobRun>());
```

### QueryConcreteReqBuilder

```csharp
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Enums;

var query = QueryConcreteReqBuilder.New()
    .AddIncludes("Addresses", "PhoneNumbers")
    .AddWhere(b => b
        .Equals("Status", "Active")
        .AddAnd(inner => inner
            .GreaterThan("Age", 18)
            .Contains("Tags", "verified")))
    .AddSort("CreatedAt", SortDirection.Desc)
    .SetPagination(0, 20)
    .Build();

// Typed via For<T>()
var typed = QueryConcreteReqBuilder.New()
    .For<Person>()
    .Include(p => p.Addresses)
    .AddWhere(q => q.AddEquals(p => p.Status, "Active"))
    .Done()
    .Build();

```

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

### Root query (From / Joins)

```csharp
var query = QueryReqBuilder.New()
    .From("o", "OrderEntity")
    .Join("p", "PersonEntity", JoinType.Left, on => {
        on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" });
    }, asName: "recipient")
    .AddSelects("o.Id", "p.FirstName", "p.LastName")
    .SetPagination(0, 50)
    .Build();
// POST /api/Job/Query

```

### Subquery (two-phase execution)

```csharp
var node = WhereClauseBuilder.And()
    .Equals("Age", 10)
    .AddSubClause(sub => sub.AddAnd(s => s.Equals("Name", "Alice")))
    .Build();

```

### BeforeQuery hook

```csharp
<LyoDataGrid BeforeQuery="@(query => query.AddIncludes("Addresses", "PhoneNumbers"))" ... />
```

### Cross-schema / same-context navigations (DI, no `OnModelCreating` edits)

```csharp
// Root entity partial — required for Include/Where/Select path walking
public partial class OrderEntity
{
    public virtual PersonEntity? Person { get; set; }
}

builder.Services.AddCrossSchemaNavigations<AppDbContext>(navs =>
{
    // Related type already on this context
    navs.AddSameContext<OrderEntity, CustomerEntity>(
        e => e.Customer, e => e.CustomerId);

    // Related type owned by another module's migrations (same Postgres DB)
    navs.AddCrossSchema<OrderEntity, PersonEntity>(
        e => e.Person, e => e.PersonId,
        table: "person", schema: "people",
        configureRelated: b => b.ApplyConfiguration(new PersonEntityConfiguration()));
});

builder.Services.AddDbContextFactoryWithLyoNavigations<AppDbContext>(ob =>
    ob.UseNpgsql(connectionString));
```

### DynamicEndpointOptions (simple overload)

```csharp
// All entities – single set of routes: /{entityType}/QueryConcrete, /{entityType}/{id}, etc.
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

### CreateBuilder vs CreateReadOnlyBuilder

```csharp
// Full CRUD (request/response types)
app.CreateBuilder<MyDbContext, MyEntity, MyRequest, MyResponse, Guid>("/api/items", "Items")
    .WithCrud(crud => crud.WithFlags(ApiFeatureSet.FullCrud))
    .Build();

// Read-only (no request type; use object for TRequest)
app.CreateReadOnlyBuilder<MyDbContext, MyEntity, MyResponse>("/api/items", "Items")
    .WithReadOnlyEndpoints()
    .Build();

// Entity-as-both (no DTOs; mapping skipped when TRequest = TDbModel or TDbModel = TResult)
app.CreateBuilder<MyDbContext, MyEntity, MyEntity, MyEntity, Guid>("/api/items", "Items")
    .WithCrud(crud => crud.WithFlags(ApiFeatureSet.CoreAll + ExportApiFeature.Instance))
    .Build();
```

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

### Builder-level

```csharp
app.CreateBuilder<...>("/api/items", "Items")
    .RequireAuthorization() // All endpoints require auth
    .RequireAuthorization("AdminOnly") // Or specific policy
    .AllowAnonymous() // Or allow anonymous
    .WithCrud(crud => crud.WithFlags(ApiFeatureSet.FullCrud))
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

## Benchmarks

k6 load / spike / soak / stress against Person QueryConcrete, QueryProject, and root Query.

- Portfolio suite: `query-api`
- [QueryConcrete load](/benchmarks/query-api)
- [QueryConcrete spike](/benchmarks/query-api)
- [QueryConcrete soak](/benchmarks/query-api)
- [QueryProject load](/benchmarks/query-api)

## Setup

Register services before building endpoints:

## Cross-schema / same-context navigations (DI, no `OnModelCreating` edits)

When related rows live in the **same database** but another schema/module (or an unmapped FK on the same context), register navigations at startup. EF then JOINs in one SQL query
so Include / Where / Sort / Select keep correct pagination. Do **not** post-filter in memory.

Requirements:

1. CLR navigation properties on the root entity (a `partial` next to a scaffolded type is fine).
2. `AddCrossSchemaNavigations<TContext>(...)` then `AddDbContextFactoryWithLyoNavigations<TContext>(...)` (replaces a plain `AddDbContextFactory`).
3. Same database connection. Cross-schema mappings use `ExcludeFromMigrations` so the root context never owns the related tables.

`LyoComposingModelCustomizer` runs after the context's `OnModelCreating` and applies the registrations. Clients then use normal includes (`Person`, `Person.FirstName` filters,
QueryProject `Select`).

DI navigations intentionally diverge from the EF migration snapshot (soft FKs / `ExcludeFromMigrations`). When registrations exist, `AddDbContextFactoryWithLyoNavigations` ignores
`PendingModelChangesWarning` so `MigrateAsync` is not blocked.

## `ApiClientOptions` and integration HTTP clients

[`Lyo.Api.Client.ApiClientOptions`](../Lyo.Api.Client/ApiClientOptions.cs) is the shared configuration base for [`ApiClient`](../Lyo.Api.Client/ApiClient.cs) and for integration option types (for example `LyoDiscordClientOptions`, `TypecastClientOptions`). `BaseUrl`, `EnsureStatusCode`, `AcceptEncodings`, response decompression, and request compression bind under each integration's configuration section with the same JSON shape as the top-level `ApiClient` section.

## Host service registration ([`ServiceCollectionExtensions`](ServiceCollectionExtensions.cs))

All registrations are extension methods on `IServiceCollection` (no `IServiceCollection` first parameter shown. They live inside `extension(IServiceCollection services)` blocks):

| Method | Registers |
| ------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddLyoQueryServices()` | `ITypeConversionService` (also exposed as `IValueConversionService`), `IEntityLoaderService`, `IProjectionService`, `IQueryPathExecutor`, `IQueryPagingHelper`, `QueryOptions`, plus `ICachePayloadSerializer` bound to host JSON options. Calls `AddLyoQueryServices(false)`. |
| `AddCrossSchemaNavigations<TContext>(configure)` | Registers same-context / cross-schema navigations applied after `OnModelCreating` (see [Cross-schema navigations](#cross-schema--same-context-navigations-di-no-onmodelcreating-edits)). |
| `AddDbContextFactoryWithLyoNavigations<TContext>` | `IDbContextFactory<TContext>` + `LyoComposingModelCustomizer` so cross-schema registrations take effect. Call after `AddCrossSchemaNavigations`. |
| `AddLyoCrudServices<TContext>()` | Scoped `IQueryService<TContext>`, `ICreateService<TContext>`, `IPatchService<TContext>`, `IDeleteService<TContext>`, `IUpdateService<TContext>`, `IUpsertService<TContext>`, `ILyoRepository<TContext>`; registers `BulkOperationOptions` and `CacheOptions` defaults; registers JSON export handler. |
| `AddLyoApiExport<TContext>()` (`Lyo.Api.Export`) | Scoped `IExportService<TContext>` + export endpoint contributor (`ExportApiFeature`). Requires `AddLyoCrudServices`. |
| `AddCsvExport()` / `AddXlsxExport()` | Optional format handlers (`Lyo.Api.Export.Csv` / `.Xlsx`). |
| `AddPostgresSprocService<TContext>()` | Scoped `ISprocService` → `PostgresSprocService<TContext>` for PostgreSQL set-returning functions (`SELECT * FROM schema.func(…)`). |
| `AddLyoDiffServices()` | Forwards to `Lyo.Diff.AddLyoDiff()` (text and object-graph diff, `IDiffService`). |

## Diagnostic recording ([`LyoApiDiagnosticExtensions`](LyoApiDiagnosticExtensions.cs))

`services.AddLyoApiDiagnosticRecording(configure?)` is a thin alias over `Lyo.Diagnostic.AspNetCore.AddLyoDiagnosticsWeb`. It registers the breadcrumb pipeline, in-memory inbox, and structured logging for ASP.NET Core hosts. Pass an optional `Action<DiagnosticWebOptions>` to override capture behavior.

## `LoggingMiddleware` ([`Middleware/LoggingMiddleware.cs`](Middleware/LoggingMiddleware.cs))

- **Source of truth** for writing and logging JSON API failures. Hosts must register `app.UseMiddleware<LoggingMiddleware>()` and should **not** use `UseStatusCodePages()`.
- **Per-request scope** with `Trace`, host, user-agent, **client IP**, **user id/name** (when authenticated), method, path, and sanitized query.
- **Debug** request/response lines.
- **Caught failures (Warn):** throw `ApiErrorException` (preferred), `HttpException`, or `ValidationException`. Middleware writes `LyoProblemDetails` as `application/problem+json` and logs **Warn** with status, error codes, IP, user, and `GetFullMessage()`.
- **Uncaught (Error):** any other exception → 500 problem + **Error** log with the exception. Client disconnect / abort (`OperationCanceledException` when `RequestAborted` is set) is **Debug** and is not turned into a 500. Query cache misses on abort rethrow instead of returning `Cancelled` problem JSON.
- **Empty-body fallback (Warn):** bare `Results.NotFound()` / empty 4xx/5xx get a synthesized Lyo problem.
- Endpoints should **throw** `ApiErrorException.From(...)` / `ApiErrorResponseFactory.ThrowForError(...)` instead of returning problem JSON.
- Use `Constants.ApiErrorCodes` for `errors[].code`; put field/path names in `errors[].description`.

## Cross-cutting helpers in [`Extensions.cs`](Extensions.cs)

- **`Extensions.ApiErrorFromException(ex, message?, errorCode?)`.** builds a `LyoProblemDetails` with `Activity.Current` trace/span ids; defaults to `Constants.ApiErrorCodes.Unknown` and uses `ex.Message` when no message is supplied.
- **`IFormFile.HashAsync(ct)`.** async SHA-256 of an uploaded form file (32-byte digest); disposes the read stream.
- **`Type` extensions**. `IsNumericType`, `IsNullable`, `GetUnderlyingType`, `GetCollectionElementType`, `GetFriendlyTypeName`, `IsCollectionType` (used by `PatchRequestPropertyValidator` and friends).
- **`object` extensions**. `IsObjectEnumerable`, `TryGetAsEnumerable<T>`, `ConvertToTargetType(targetType)`, `ConvertToType(targetType)` (single-value and collection coercion that understands `JsonElement`, enums, `Guid`/`DateTime`/`DateOnly`/`TimeOnly`, booleans, and numerics).
- **`JsonElement` extensions**. `ExtractValueFromJsonElement` and `ExtractArrayFromJsonElement` (safe value pull-out used by patch/dynamic-CRUD payloads).

## Validation

Endpoints validate requests before running CRUD/query logic. Failures return `LyoProblemDetails` with stable error codes:

| Area | What is checked |
| ------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Paging** | `Start` / `Amount` against `QueryOptions` (defaults and max page size; export uses max export size where applicable) |
| **Bulk** | Number of items in bulk requests vs `BulkOperationOptions.MaxAmount` |
| **Patch** | JSON property keys exist and are writable on `TDbModel`; values convert to the property type (`PatchRequestPropertyValidator`) |
| **Query / QueryProject** | Filter **field** paths, `Include` segments, and projection `Select` paths against the EF model (cached `QueryPathValidationCache` + `ProjectedQueryModelValidator` for projected queries) |

Authorization and patch property allowlists run after patch key/value validation.

## Quick start

You can still pass a `CrudConfiguration<...>` record: `.WithCrud(ApiFeatureSet.FullCrud, new CrudConfiguration<...> { ... })`.

## ApiFeature / ApiFeatureSet

Features are extensible records in a set (`features.Contains(ApiFeature.Query)`). `Export` lives in `Lyo.Api.Export` as `ExportApiFeature.Instance`, not in core presets.

| Feature / preset | Endpoints |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `ApiFeature.Query` | QueryConcrete, QueryProject, and (on dynamic CRUD) root `/Query` |
| `ApiFeature.Get` | Get |
| `ApiFeature.Create` | Create |
| `ApiFeature.CreateBulk` | Bulk create |
| `ApiFeature.Update` | Update |
| `ApiFeature.UpdateBulk` | Bulk update |
| `ApiFeature.Patch` | Patch |
| `ApiFeature.PatchBulk` | Bulk patch |
| `ApiFeature.Delete` | Delete |
| `ApiFeature.DeleteBulk` | Bulk delete |
| `ApiFeature.Upsert` | Upsert |
| `ApiFeature.UpsertBulk` | Bulk upsert |
| `ExportApiFeature.Instance` | Export (`Lyo.Api.Export` + `AddLyoApiExport`) |
| `ApiFeature.Metadata` | Metadata endpoint |
| `ApiFeature.ProjectionComputedFields` | Enables `ComputedFields` on `ProjectionQueryReq` for `/QueryProject` (requires `Query` + registered `IFormatterService`) |
| `ApiFeatureSet.ReadOnly` | Query + Get |
| `ApiFeatureSet.BasicCrud` | Query, Get, Create, Update, Patch, Delete |
| `ApiFeatureSet.FullCrud` | BasicCrud + Upsert |
| `ApiFeatureSet.BulkOperations` | All bulk variants |
| `ApiFeatureSet.CoreAll` | Standard CRUD + bulk (**no Export**, Metadata, or ProjectionComputedFields) |
| `ApiFeatureSet.DefaultCrud` | `CoreAll` + upsert/patch inheritance flags |
| `ApiFeature.UpsertInheritCreate` | Upsert uses Create hooks |
| `ApiFeature.UpsertInheritUpdate` | Upsert uses Update hooks |
| `ApiFeature.PatchInheritsUpdate` | Patch uses Update hooks |

## Dynamic endpoint builder (MapDynamicCrudEndpoints)

Register CRUD endpoints for all entities in a DbContext with **dynamic routes** `{baseRoute}/{entityType}/…` (e.g. `POST /api/Job/Person/QueryConcrete`, `GET /api/Job/Person/{id}`
when
`BaseRoute = "api/Job"`). `entityType` is the entity type's CLR **name**. Uses entity-as-request-response (no DTOs) and infers primary key and default order from the EF model.

For **per-entity routes with custom DTOs**, use `CreateBuilder` (see Quick start).

### Fluent config

```csharp
app.MapDynamicCrudEndpoints<JobContext>(c => c
    .WithDefaults(d => {
        d.BaseRoute = "api/Job";
        d.Features = ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance;
    })
    .For<JobDefinition>(e => e
        .ExcludeCreate()
        .ForPatch(p => p.Before((ctx, entity) => entity.ModifiedAt = DateTime.UtcNow)))
    .For<JobRun>(e => e.ExcludeExport())
    .IncludeOnly<JobDefinition, JobRun>());
```

### DynamicEndpointOptions (simple overload)

```csharp
// All entities – single set of routes: /{entityType}/QueryConcrete, /{entityType}/{id}, etc.
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

| Property | Default | Description |
|---------------|-------------|----------------------------------------------|
| BaseRoute | "" | Route prefix (e.g. "/api") |
| Features | DefaultCrud | `ApiFeatureSet` for each entity |
| IncludedTypes | [] | When non-empty, only these types (whitelist) |
| ExcludedTypes | [] | Types to exclude (e.g. xref tables) |

### Per-entity overrides (EntityEndpointConfigBuilder)

| Method | Description |
|-----------------|---------------------------------------|
| `ExcludeCreate` | Exclude Create endpoint |
| `ExcludeUpdate` | Exclude Update, UpdateBulk |
| `ExcludePatch` | Exclude Patch, PatchBulk |
| `ExcludeDelete` | Exclude Delete, DeleteBulk |
| `ExcludeUpsert` | Exclude Upsert, UpsertBulk |
| `ExcludeExport` | Exclude Export |
| `ExcludeQuery` | Exclude Query, QueryProject |
| `ExcludeGet` | Exclude Get |
| `ForCreate` | Configure Create hooks (Before/After) |
| `ForPatch` | Configure Patch hooks |
| `ForUpdate` | Configure Update hooks |
| `ForDelete` | Configure Delete hooks, Includes |
| `ForUpsert` | Configure Upsert hooks |
| `ForExport` | Configure Export (auth, etc.) |

Routes: `{baseRoute}/{entityType}/QueryConcrete`, `{baseRoute}/{entityType}/QueryProject`, `{baseRoute}/{entityType}/{id}`, etc. Entity type name is the route segment (e.g.
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

For `CreateBuilder`, metadata is opt-in via `.WithMetadata()` or `ApiFeature.Metadata` and is exposed at `GET {baseRoute}/Metadata`.

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
    .WithCrud(crud => crud.WithFlags(ApiFeatureSet.FullCrud))
    .Build();

// Read-only (no request type; use object for TRequest)
app.CreateReadOnlyBuilder<MyDbContext, MyEntity, MyResponse>("/api/items", "Items")
    .WithReadOnlyEndpoints()
    .Build();

// Entity-as-both (no DTOs; mapping skipped when TRequest = TDbModel or TDbModel = TResult)
app.CreateBuilder<MyDbContext, MyEntity, MyEntity, MyEntity, Guid>("/api/items", "Items")
    .WithCrud(crud => crud.WithFlags(ApiFeatureSet.CoreAll + ExportApiFeature.Instance))
    .Build();
```

### WithCrud vs individual With* methods

Use `WithCrud(Action<CrudConfigurationBuilder<...>>)` for hooks and per-operation auth in one place, or `WithCrud(features, CrudConfiguration<...>)` with a record
initializer.

Or register operations with `WithQuery`, `WithGet`, `WithCreate`, etc. Each supports either delegate parameters, a config record, or a **fluent builder** (
`Action<..EndpointConfigBuilder>`).

```csharp
app.CreateBuilder<...>("/api/items", "Items")
    .WithQuery(q => q.Auth(EndpointAuth.Anonymous()).ComputedFields())
    .WithGet()
    .WithCreate(c => c.Before(ctx => { }).After(ctx => { }).Auth(EndpointAuth.RequireRole("Editor")))
    .WithExport()
    .Build();
```

**Combined single/bulk helpers** (same hooks and auth wiring for both routes):

| Method | Registers |
|---------------------------------------------------------------|-------------------------------------------------------------------------------------|
| `WithCreateAndBulk(Action<CreateEndpointConfigBuilder<...>>)` | Create + Bulk |
| `WithUpdateAndBulk(Action<UpdateEndpointConfigBuilder<...>>)` | Update + Bulk/Update |
| `WithPatchAndBulk(Action<PatchEndpointConfigBuilder<...>>)` | Patch + Bulk (auth, inherit-from-update, **property authorization**) |
| `WithUpsertAndBulk(Action<UpsertEndpointConfigBuilder<...>>)` | Upsert + Bulk/Upsert (`Auth` for single; `BulkAuth` optional override for bulk) |

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

Same options via `WithCrud(crud =>..)`: `WithFlags` (required), lifecycle `Before*` / `After*`, `DeleteIncludes`, `Metadata`, per-operation `*Auth` methods, and `PatchPropertyAuthorization`. Either a built `PatchPropertyAuthorization` record or `PatchPropertyAuthorization(b => b.AllowPropertiesForPolicy(...))` for policy maps.
For a custom rule, set `PatchPropertyAuthorization` on the record or assign `PatchPropertyAuthorization.Custom` in code.

## Authorization

### Builder-level

```csharp
app.CreateBuilder<...>("/api/items", "Items")
    .RequireAuthorization() // All endpoints require auth
    .RequireAuthorization("AdminOnly") // Or specific policy
    .AllowAnonymous() // Or allow anonymous
    .WithCrud(crud => crud.WithFlags(ApiFeatureSet.FullCrud))
    .Build();
```

### Per-endpoint (EndpointAuth)

| Method | Description |
|------------------------------------------------------------------|-----------------------------|
| `EndpointAuth.RequireAuthorization()` | Requires authenticated user |
| `EndpointAuth.RequireAuthorization("Policy1", "Policy2")` | Requires specified policies |
| `EndpointAuth.RequireAuthorization(p => p.RequireRole("Admin"))` | Inline policy |
| `EndpointAuth.RequireRole("Admin", "Editor")` | Role-based |
| `EndpointAuth.RequireClaim("scope", "write", "admin")` | Claim-based |
| `EndpointAuth.RequireAuthenticatedUser()` | Authenticated user |
| `EndpointAuth.RequireUserName("admin@example.com")` | Specific user |
| `EndpointAuth.Anonymous()` | Anonymous access |

When `EndpointAuth` is null for an endpoint, builder-level auth is used.

## Endpoints

Routes emitted by **CreateBuilder** / **CreateReadOnlyBuilder** when the matching `With*` / `ApiFeature / ApiFeatureSet` configuration is enabled (17 HTTP endpoints when everything
below is
turned on).

| Method | Route | Description |
|--------|----------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|
| POST | `{baseRoute}/QueryConcrete` | Entity-graph query with filters, includes, sort, pagination (typed CreateBuilder) |
| POST | `{baseRoute}/QueryProject` | Projected query (`Select`); SQL-level projection when possible; optional computed fields when `ProjectionComputedFields` is set |
| POST | `{dynamicBase}/Query` | **Root** From/Joins + Select (dynamic CRUD only); any allowlisted entity on that context; returns `ProjectedQueryRes` |
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
| GET | `{baseRoute}/Metadata` | OpenAPI-style metadata for this group (`WithMetadata` / `ApiFeature.Metadata`) |

## Query and request builders

### QueryConcreteReqBuilder

```csharp
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Enums;

var query = QueryConcreteReqBuilder.New()
    .AddIncludes("Addresses", "PhoneNumbers")
    .AddWhere(b => b
        .Equals("Status", "Active")
        .AddAnd(inner => inner
            .GreaterThan("Age", 18)
            .Contains("Tags", "verified")))
    .AddSort("CreatedAt", SortDirection.Desc)
    .SetPagination(0, 20)
    .Build();

// Typed via For<T>()
var typed = QueryConcreteReqBuilder.New()
    .For<Person>()
    .Include(p => p.Addresses)
    .AddWhere(q => q.AddEquals(p => p.Status, "Active"))
    .Done()
    .Build();
```

Use `ProjectionQueryReqBuilder` when building `ProjectionQueryReq` (includes `AddSelects`, `AddWhere`, `SetZipSiblingCollectionSelections`, etc.).

### QueryRequest Keys (object[][])

`Keys` fetches specific entities by primary key. Each element is a key array:

- **Single-key**: `[[1], [2], [3]]` for ids 1, 2, 3
- **Composite-key**: `[["tenant-a", 1], ["tenant-b", 2]]` for (TenantId, Id)

### Projection (QueryProject)

`POST {baseRoute}/QueryProject` accepts a `ProjectionQueryReq` body and returns a `ProjectedQueryRes<T>` envelope (not the same shape as `QueryRes<T>` from `/QueryConcrete`).
Navigation-based Select only. No From/Joins. The response echoes the request in `queryRequest` (including `Select` as executed computed-field dependencies may have been
merged server-side). On success,
`entityTypes` lists CLR entity class names involved in the projection (root + navigations + template paths);
see [Projection (QueryProject) & SQL-Level Query Generation](#projection-queryproject--sql-level-query-generation) above.

### Root query (From / Joins)

`POST {dynamicBase}/Query` (registered by `MapDynamicCrudEndpoints`, at the context base. **not** under `{entityType}`) accepts a `QueryReq` with required `From`,
optional `Joins`, and required `Select`. Returns `ProjectedQueryRes` (generic JSON rows).

- Sources are mapped EF entity types (`entityType` = CLR type name, same as dynamic routes) plus optional nested `query` (Where/Keys scope on that `DbSet` before join).
- Outer `whereClause` / `sortBy` may only reference the **From** alias in v1; filter join sides via nested `Join.Query`.
- Join `as` (e.g. `recipient`) nests joined fields under that key in multi-field rows.
- Cross-schema related types still need to be mapped on the context (e.g. `AddCrossSchemaNavigations`); typed QueryProject continues to use EF navigations without joins.

```csharp
var query = QueryReqBuilder.New()
    .From("o", "OrderEntity")
    .Join("p", "PersonEntity", JoinType.Left, on => {
        on.Add(new JoinOn { From = "o.PersonId", To = "p.Id" });
    }, asName: "recipient")
    .AddSelects("o.Id", "p.FirstName", "p.LastName")
    .SetPagination(0, 50)
    .Build();
// POST /api/Job/Query
```

Use `Select` to specify which fields to return. Supports dotted paths and wildcards.

#### Computed fields

Optional `ComputedFields`: each entry has `name` (output column) and `template` (SmartFormat string). Placeholders reference projected paths, e.g.
`"{LastName}, {FirstName}"`, `"{contactaddresses.address.city}"`, or a single bare dotted path as the whole template. Enable the feature with `ApiFeature.ProjectionComputedFields` and register `IFormatterService`. Template placeholders contribute to `entityTypes` the same way `Select` paths do.

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

**Select fields**. Return only specified fields:

```json
{
  "Start": 0,
  "Amount": 10,
  "Select": ["Id", "Name", "Email"],
  "whereClause": { "$type": "condition", "field": "Status", "comparison": "Equals", "value": "Active" }
}
```

Response: `[{ "Id": "..", "Name": "Alice", "Email": "alice@example.com" },..]`

**Nested paths**. Project scalar values from collections:

```json
{
  "Select": ["JobRuns.CreatedBy"],
  "whereClause": { "$type": "condition", "field": "Id", "comparison": "Equals", "value": "..." }
}
```

Response: `[["user-1", "user-2"],..]` (array of scalar arrays per row)

**Sibling fields on the same collection**. When several `Select` paths share one collection prefix (e.g. `DocketCharges.Code` and `DocketCharges.Number`), the API can either zip
them into a single array of objects under that prefix (`DocketCharges: [{ "Code": "..", "Number": ".." },..]`) or keep one column per path (parallel arrays). Control this with
`options.zipSiblingCollectionSelections`: omit or `true` to zip (default), `false` for parallel columns. SQL projection, computed fields, MatchedOnly includes, and caching still
apply; only the final row shape changes.

**Wildcard**. Project entire nested objects:

```json
{
  "Select": ["JobRuns.*"],
  "whereClause": { "$type": "condition", "field": "Id", "comparison": "Equals", "value": "..." }
}
```

Response: `[[{ "Id": "..", "CreatedBy": "user-1", "State": ".." },..],..]`

**Root wildcard**. Flatten root entity to a single object:

```json
{"Select": ["*"], "Start": 0, "Amount": 1}
```

Response: `[{ "Id": "..", "Name": "..", "CreatedAt": ".." },..]` (no `*` key; properties at root)

**IncludeFilterMode: MatchedOnly**. When filtering on nested fields (e.g. `contactemailaddresses.emailaddress.email`), include only matched items in collections. Example for `/QueryProject` (includes are derived from `Select` / filter paths do not rely on `Include` here):

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

For `/QueryConcrete`, you can add an `"include"` array alongside `"whereClause"` as usual.

Returns only emails matching `@gmail.com` or `@yahoo.com`; excludes `@charter.net` etc.

### QueryRequestOptions

| Property | Default | Description |
|-------------------|---------|--------------------------------------------------------------------------------------------|
| TotalCountMode | Exact | `Exact`, `None`, or `HasMore` (pagination optimization) |
| IncludeFilterMode | Full | `Full` = all related items; `MatchedOnly` = only items matching the **whereClause** filter |

`ProjectionQueryReq` uses `ProjectedQueryRequestOptions`, which adds:

| Property | Default | Description |
|--------------------------------|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ZipSiblingCollectionSelections | `true` | When `true`, sibling `Select` paths under the same collection are zipped into one array of objects; when `false`, each path stays a separate column (parallel arrays). |

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

### Subquery (two-phase execution)

Root conditions run in the database; the subquery runs in-memory on the filtered results. Use for collection fields (e.g. `Tags`) that aren't efficiently queryable in SQL.

**AddSubClause**. Attach a nested clause to the current group (root runs in the database; nested filter can run in-memory):

```csharp
var node = WhereClauseBuilder.And()
    .Equals("Age", 10)
    .AddSubClause(sub => sub.AddAnd(s => s.Equals("Name", "Alice")))
    .Build();
```

**AddConditionWithSubClause**. Attach sub-clause to a specific condition:

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

Export requires `AddLyoApiExport<TContext>()`, optional `AddCsvExport()` / `AddXlsxExport()`, and `ExportApiFeature.Instance` on the route (or `.WithExport()`).

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

## Query result caching

`POST …/QueryConcrete` and `POST …/QueryProject` both use the same `IQueryService` pipeline and the same `QueryOptions` singleton. Cached entries are keyed from the
request (filters, paging, includes/sort for Query; plus projection `Select`, computed fields, and projected row-shape flags for QueryProject. See `QueryCacheKeyBuilder`).
Tags for invalidation are built with `QueryCacheTagBuilder` (scope tags such as `queries` / `queryproject`, `entities`, type-wide `entity:{type}`, per-row `entity:{type}:{pk}`, and SQL-projected `projshape:{sha1}` fingerprints. See below).

- **Default (`CacheQueryResultsAsUtf8Payload` = `false`)**. `GetOrSetAsync` stores `QueryRes<T>` / `ProjectedQueryRes<T>` with Fusion's usual serialization for CLR
 graphs.
- **Typed payload (`CacheQueryResultsAsUtf8Payload` = `true`)**. `GetOrSetPayloadAsync<T>` stores framed bytes via `ICachePayloadSerializer` and `ICachePayloadCodec` (
 optional compress/encrypt under `CacheOptions:Payload`). The SQL-level QueryProject path and the load-then-project fallback both use this mode when enabled; fallback goes
 through `QueryCore`, which already applies the same flag.

With typed payloads, `CacheOptions:Payload` can `AutoCompress` (above `AutoCompressMinSizeBytes`) and, on supported targets, `AutoEncrypt` (with **`EncryptionKeyId`
** and `IEncryptionService`). Those steps run **before** the entry is written to the cache implementation. For a **distributed backplane** (e.g. **Redis** via Fusion's
secondary layer), that means **fewer bytes cross the network** on every cache read/write, which **reduces backplane latency** and bandwidth versus storing large uncompressed JSON
blobs. For typical repetitive JSON query results, **lossless compression alone often shrinks stored size on the order of ~90%**, so the win is largest when the API tier and Redis
are not co-located or when payload sizes are large.

Register cache before `AddLyoQueryServices` / `AddLyoCrudServices`. `AddLyoQueryServices` registers `ICachePayloadSerializer` to match `JsonOptions`, so cached
payloads stay consistent with API JSON.

`CacheOptions:QueryCacheTagGranularity` (default `Broad`) controls how list/query/GET entries are tagged in `QueryCacheTagBuilder`. `Broad` keeps type-wide `entity:{type}` (plus scope/shape tags). Less CPU when storing entries; `Granular` adds `entity:{type}:{pk}` instance tags for finer invalidation. When `Broad` is
active, `InvalidateQueryCachesForEntityKeysAsync` only removes the broad `entity:{type}` tag (no per-key work). Set `Granular` when you want mutations to bust only
affected cached pages.

### Invalidation on writes (built-in CRUD)

Invalidation uses **tags** (Fusion `RemoveByTagAsync`) and, where useful, **direct cache keys** so a single primary key can clear list queries, `GET …/{id}` rows, and
projected pages without relying on tag scans alone.

**Granular primary keys. `QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync`**

Used after successful **Patch**, **Update**, **Delete**, and many **Upsert** paths when the affected rows are known. For each distinct primary key (up to `CacheOptions.MaxBulkQueryInvalidationByIdCount`; above that it falls back to the broad type tag only):

- `InvalidateCacheItemByTag` with `entity:{lowercased type name}:{pkSegment}`. Removes every cached entry carrying that **instance tag** (list `/QueryConcrete` / `/QueryProject` pages that tagged those rows, `GET` results that included that entity in the graph, etc.).
- `InvalidateCacheItem` for the **canonical single-entity GET keys** built by `QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey`: the base key **matches the instance tag
 string** (EF key order, same as `QueryCacheTagBuilder.EntityInstanceTag`), with optional `:include=…` and `:raw` suffixes on `GET`. Invalidation explicitly
 removes the **no-`include`** mapped and `:raw` entries so `GET` by id is cleared even when clients also subscribe by key.

**Broad type sweep. `InvalidateQueryCacheAsync<TDbModel>()`**

Maps to tag `entity:{lowercased type name}`. Still used where a **new row** cannot be expressed as existing instance tags (e.g. `CreateService` after create) and remains
the bulk fallback when too many distinct keys would be invalidated at once.

**Projection shape. `QueryCacheTagBuilder.FormatProjShapeTag` / `projshape:{sha1}`**

SQL-level `POST …/QueryProject` cache entries are tagged with a stable `projshape:…` fingerprint (sorted select paths, computed fields, zip flag). Call `QueryCacheInvalidation.InvalidateProjectedQueriesByProjShapeAsync` to drop every cached page for that grid shape without invalidating unrelated projections.

How tags attach to cached reads (see `QueryService.QueryCore`, `Get` overloads, and `QueryCacheTagBuilder`):

- `GET …/{id}` (mapped and raw overloads): cache **key** is `BuildSingleEntityGetCacheKey`; tags include `entity:{type}` and `entity:{type}:{pk}` (plus cascade tags
 when `includes` are used). **Patch** / **Update** `cache.Set` for the written DTO uses the **same key shape** and **instance tag** so post-write caches stay aligned.
- `POST …/QueryConcrete` with `Include`, and `GET` with `includes`: each cached result is tagged with `entity:{root}` plus `entity:{type}` for every EF
 entity type
 from `loaderService.GetReferencedTypes`, and **instance tags** for entities in the result. `InvalidateQueryCacheAsync<AddressEntity>()` (broad) or **per-key**
 invalidation for Address rows clears cached parent reads (e.g. Person) that carried `entity:address:…` tags.
- `POST …/QueryProject`: the **SQL projection** path adds `projshape:…`, `queryproject`, `GetReferencedTypes` type tags, **instance tags** from projected rows (root
 and include cascade where applicable), plus the same scope tags as `/QueryConcrete`. The **fallback** path uses `QueryCore` tagging. A write on a **related** entity type
 still
 invalidates matching `QueryProject` entries via shared `entity:{child}:…` instance tags.

| Concern | Behavior |
|---------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Patch / Update / Delete / Upsert** (known keys) | `InvalidateQueryCachesForEntityKeysAsync`. Instance **tag** + **direct GET keys** (canonical mapped + `:raw` without includes); falls back to `InvalidateQueryCacheAsync<T>()` when key count exceeds the configured bulk threshold |
| **Create** | `InvalidateQueryCacheAsync<T>()`. Broad `entity:{T}` (new rows have no prior instance tags) |
| **Unrelated root entity** (e.g. Order vs Person) | **Not** invalidated by another type's keys. Different `entity:` tags / keys |
| **Child entity** updated via **its** endpoint | Instance tag `entity:{child}:{pk}` invalidates **parent** **`GET`/`Query`/`QueryProject`** entries that included that tag |

For extra fan-out (custom rules, raw SQL, or third-party writers), use **Before/After** hooks or `InvalidateCacheItem`, `InvalidateCacheItemByTag`, `InvalidateProjectedQueriesByProjShapeAsync`, or `InvalidateAllCachedQueriesAsync` as needed.

Example (see also `Lyo.TestApi` `appsettings.json`):

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

Optional (**.NET 10+**): set `AutoEncrypt` to `true` and `EncryptionKeyId` when `IEncryptionService` is registered, so payloads are encrypted after compression and
before they reach the backplane (defense in depth alongside TLS in transit). See `CacheOptions:Payload` in [Lyo.Cache README](../../../Core/Cache/Lyo.Cache/README.md).

Further detail: [Lyo.Cache README](../../../Core/Cache/Lyo.Cache/README.md).

## Options

### QueryOptions (singleton)

| Property | Default | Description |
|-------------------------------------|----------|------------------------------------------------------------------------------------------------------------|
| DefaultPageSize | 100 | Default page size |
| MaxPageSize | 2000 | Max page size |
| MinPagingStart | 0 | Minimum `Start` (inclusive) |
| MaxPagingStart | 10000000 | Maximum `Start` (inclusive) |
| MinPagingAmount | 1 | Minimum `Amount` when set |
| MaxExportSize | 5000 | Max rows for export |
| EnableSplitQueries | true | Split queries for includes |
| UseNoTrackingWithIdentityResolution | true | NoTracking for reads |
| AllowSelectWildcards | true | Allow terminal `*` in QueryProject `Select` paths |
| CacheQueryResultsAsUtf8Payload | false | Use typed payload cache (`GetOrSetPayloadAsync`) for Query and QueryProject instead of CLR `GetOrSetAsync` |

### BulkOperationOptions (singleton)

| Property | Default | Description |
|------------------------|---------|-------------------------|
| MaxAmount | 2000 | Max items per bulk |
| MaxDegreeOfParallelism | 10 | Parallelism |
| UseParallelProcessing | true | Use parallel processing |
| Timeout | 5 min | Bulk timeout |

## Dependencies

*(Synchronized from `Lyo.Api.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
|---------------------------------------------|---------|
| `Microsoft.AspNetCore.Authorization` | `[10,)` |
| `Microsoft.AspNetCore.Http.Abstractions` | `2.*` |
| `Microsoft.AspNetCore.OpenApi` | `[10,)` |
| `Microsoft.EntityFrameworkCore.Analyzers` | `[10,)` |
| `Microsoft.EntityFrameworkCore.Relational` | `[10,)` |
| `Microsoft.Extensions.Hosting.Abstractions` | `[10,)` |

### Project references

- [`Lyo.Api.Models`](../Lyo.Api.Models/README.md)
- [`Lyo.Cache`](../../../Core/Cache/Lyo.Cache/README.md)
- [`Lyo.Diff`](../../../Core/Diff/Lyo.Diff/README.md)
- [`Lyo.Diagnostic.AspNetCore`](../../../Core/Diagnostic/Lyo.Diagnostic.AspNetCore/README.md)
- [`Lyo.Formatter`](../../../Data/Formatter/Lyo.Formatter/README.md)
- [`Lyo.Hashing`](../../../Security/Hashing/Lyo.Hashing/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Query`](../../../Data/Query/Lyo.Query/README.md)
- [`Lyo.Validation`](../../../Core/Validation/Lyo.Validation/README.md)

### Related / optional packages

- [`Lyo.Api.Export`](../Lyo.Api.Export/README.md). Export endpoints; format handlers ship in `Lyo.Api.Export.Csv` and `Lyo.Api.Export.Xlsx` (no separate README)
- [`Lyo.Csv`](../../../Data/Csv/Lyo.Csv/README.md), [`Lyo.Xlsx`](../../../Data/Xlsx/Lyo.Xlsx/README.md). Used by export add-ons

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Diagnostic.AspNetCore` (direct, lyo)
- `Lyo.Diff` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Query` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `Microsoft.AspNetCore.Authorization` `10.0.5` (direct, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` (direct, microsoft)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)