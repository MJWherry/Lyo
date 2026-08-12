<p align="center">
  <h1 align="center">Lyo</h1>
  <p align="center"><strong>The complete API layer for .NET — query, projection, CRUD, cache, ship. You've already built this three times. Stop rebuilding it.</strong></p>
</p>

---

**Lyo** is a production-ready API framework for .NET that gives you 17 fully-featured endpoints from a single builder call. Dynamic queries, **SQL-aware field projection** (
`QueryProject`), full CRUD, bulk operations, caching, auth, and observability — all wired together, all out of the box.

```csharp
app.CreateBuilder<AppDbContext, PersonEntity, PersonRequest, PersonResponse>("person")
    .WithCrud(ApiFeatureSet.CoreAll + ExportApiFeature.Instance)
    .RequireAuthorization("AdminPolicy")
    .Build();
```

That's it. You now have a complete REST API with filtering, sorting, projections, subqueries, pagination, CRUD, bulk operations, export, caching, and OpenTelemetry — for any EF
Core entity.

---

## The Problem

Every .NET team builds the same infrastructure over and over:

- Filtering, sorting, and pagination logic
- CRUD endpoints with validation and error handling
- Bulk operations that don't lose the entire batch on one failure
- Caching that actually invalidates when data changes
- Per-endpoint authorization
- Observability and structured logging

This takes **2–6 months** per service. Then you maintain it forever. Then you do it again for the next service.

**Lyo eliminates all of it.**

---

## What You Get

### Dynamic Query Engine

A structured JSON query language with the expressiveness of GraphQL and the simplicity of REST.

```json
{
  "Start": 0,
  "Amount": 100,
  "QueryNode": {
    "Condition": "And",
    "Children": [
      { "Property": "IsActive", "Comparator": "Equals", "Value": "true" },
      { "Property": "FirstName", "Comparator": "NotEquals", "Value": null },
      {
        "Condition": "Or",
        "Children": [
          { "Property": "LastName", "Comparator": "Regex", "Value": "^[A-Z]" },
          { "Property": "Source", "Comparator": "In", "Value": "A,B,C,D,E,F" }
        ]
      },
      {
        "SubQuery": {
          "Condition": "And",
          "Children": [
            { "Property": "DateOfBirth", "Comparator": "GreaterThan", "Value": "1990-01-01" }
          ]
        }
      }
    ]
  },
  "Include": [
    "contactaddresses.address",
    "contactphonenumbers.phonenumber",
    "contactemailaddresses.emailaddress"
  ],
  "Select": ["Id", "FirstName", "LastName", "contactaddresses.address.city"],
  "SortBy": [
    { "Property": "LastName", "Direction": "Asc" },
    { "Property": "FirstName", "Direction": "Asc" }
  ]
}
```

**Features:**

- Nested logical trees (And/Or) with unlimited depth
- Subquery support with SQL-first pushdown — no in-memory materialization
- 10+ comparators: Equals, NotEquals, Contains, In, GreaterThan, LessThan, Regex, and more
- Multi-hop navigation includes (`contactaddresses.address` traverses two tables)
- GraphQL-style field projection with nested path support
- Multi-sort with direction control
- Works on **any `IQueryable<T>`** — not just EF Core

### Query projection (`QueryProject`)

A first-class **`POST …/QueryProject`** endpoint for **sparse, nested projections** — not just trimming JSON after a full entity load.

- **Declarative `Select`** — list the fields you need, including **multi-hop paths** (e.g. `contactaddresses.address.city`) and collection branches; optional **computed fields**
  for server-side expressions.
- **SQL-level projection when possible** — the engine pushes eligible shapes to the database so you move **less data** and avoid hydrating full entity graphs when the query allows
  it; more complex shapes fall back to **load-then-project** with the same request model.
- **Same power as `Query`** — shared filters, `Include` / `MatchedOnly`, sorts, paging, subqueries, and **query result caching** (including optional UTF-8 payload + compression).
- **Client-friendly JSON** — projected rows can include **`entityTypes`** (and related metadata) so consumers know the shape of each column.

Use **`Query`** when you want full entities (or maximum flexibility with includes); use **`QueryProject`** when grids, APIs, or integrations need **narrow columns and smaller
payloads** by design.

### 17 Endpoints from One Builder

| Operation              | Method | Endpoint                                                     |
|------------------------|--------|--------------------------------------------------------------|
| Query (entity graph)   | POST   | `/QueryConcrete` (typed or `{entityType}/QueryConcrete`)     |
| Projected query        | POST   | `/QueryProject`                                              |
| Root join query        | POST   | `{dynamicBase}/Query` (From/Joins + Select → projected rows) |
| Get by ID              | GET    | `/{id}`                                                      |
| Create                 | POST   | `/`                                                          |
| Create Bulk            | POST   | `/Bulk`                                                      |
| Update                 | POST   | `/Update`                                                    |
| Update Bulk            | POST   | `/Bulk/Update`                                               |
| Patch (property-level) | PATCH  | `/`                                                          |
| Patch Bulk             | PATCH  | `/Bulk`                                                      |
| Upsert                 | POST   | `/Upsert`                                                    |
| Upsert Bulk            | POST   | `/Bulk/Upsert`                                               |
| Delete                 | DELETE | `/{id}`                                                      |
| Delete (by body)       | DELETE | `/`                                                          |
| Delete Bulk            | DELETE | `/Bulk`                                                      |
| Export                 | POST   | `/Export`                                                    |
| Stored Procedures      | —      | Configurable                                                 |

### Bulk Operations with Individual Fallback

Every bulk operation follows the same pattern:

1. Attempt the batch operation for maximum throughput
2. If any item fails, fall back to individual processing
3. Return a detailed result showing which succeeded and which failed

```json
{
  "createdCount": 498,
  "failedCount": 2,
  "results": [
    { "isSuccess": true, "data": { "id": "..." } },
    { "isSuccess": false, "error": { "message": "Duplicate key violation" } }
  ]
}
```

No more "one bad record kills the entire import." Partial success is the default.

### Property-Level Patch

Update individual fields without sending the full entity:

```json
{
  "Keys": ["550e8400-e29b-41d4-a716-446655440000"],
  "Properties": {
    "FirstName": "Jane",
    "IsActive": true
  },
  "AllowMultiple": false
}
```

### Export

Query your data and export it directly as CSV, XLSX, or JSON — with column mapping and formatting.

### Cross-Schema Navigations (same database)

Register relationships at DI startup without editing scaffolded `OnModelCreating`. Related tables must share the host’s database; EF emits JOINs so Include / Where / Sort / Select
stay one query with correct paging.

```csharp
services.AddCrossSchemaNavigations<AppDbContext>(navs =>
{
    navs.AddSameContext<Order, Customer>(e => e.Customer, e => e.CustomerId);
    navs.AddCrossSchema<Order, PersonEntity>(
        e => e.Person, e => e.PersonId,
        table: "person", schema: "people",
        configureRelated: b => b.ApplyConfiguration(new PersonEntityConfiguration()));
});
services.AddDbContextFactoryWithLyoNavigations<AppDbContext>(ob => ob.UseNpgsql(conn));
```

Add CLR navigation properties on the root entity (partials are fine). Cross-schema mappings use `ExcludeFromMigrations`.

### Before/After Hooks

Inject custom logic at every stage of every operation:

```csharp
app.CreateBuilder<AppDbContext, PersonEntity, PersonRequest, PersonResponse>("person")
    .WithCreate(config => {
        config.Before = async (entity, ctx) => {
            entity.CreatedBy = ctx.User.Identity.Name;
            return entity;
        };
        config.After = async (entity, ctx) => {
            await notificationService.SendCreatedAlert(entity);
            return entity;
        };
    })
    .Build();
```

### Per-Endpoint Authorization

Granular auth control — at the builder level or per operation:

```csharp
app.CreateBuilder<AppDbContext, PersonEntity, PersonRequest, PersonResponse>("person")
    .RequireAuthorization("ReadPolicy")
    .WithCreate(config => config.Auth = EndpointAuth.RequireRole("Admin"))
    .WithDelete(config => config.Auth = EndpointAuth.RequireClaim("permission", "delete"))
    .WithQuery()
    .WithGet()
    .Build();
```

### Feature Flags

Only expose what you need:

```csharp
.WithCrud(ApiFeatureSet.ReadOnly)                              // Query + Get only
.WithCrud(ApiFeatureSet.BasicCrud)                            // Query, Get, Create, Update, Patch, Delete
.WithCrud(ApiFeatureSet.FullCrud)                             // BasicCrud + Upsert
.WithCrud(ApiFeatureSet.CoreAll)                              // CRUD + bulk (no Export)
.WithCrud(ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance) // DefaultCrud + export endpoint
```

### Caching with Automatic Invalidation

Built-in support for local caching or distributed caching via FusionCache:

- Query and QueryProject results are cached with **request-derived keys** (**`QueryCacheKeyBuilder`**) and **tags** (**`QueryCacheTagBuilder`**) for invalidation: scope tags (*
  *`queries`**, **`queryproject`**, **`entities`**), type-wide **`entity:{type}`**, optional per-row **`entity:{type}:{pk}`** (when **`CacheOptions:QueryCacheTagGranularity`** is *
  *`Granular`**), and SQL-projected **`projshape:{sha1}`** (select + computed + zip fingerprint). **Default is `Broad`**: type-scoped tags only — lower CPU when writing cache
  entries; **`Granular`** opts into per-row instance tags and finer invalidation.
- **Invalidation after writes** — **`QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync`** runs after successful **Update**, **Patch**, **Delete**, and many **Upsert**
  flows. With **`Broad`**, it removes the **`entity:{type}`** tag only (same cost as a type-wide sweep for that entity). With **`Granular`**, it removes **instance tags** *
  *`entity:{type}:{pk}`** and **directly** invalidates canonical **`GET …/{id}`** keys from **`QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey`** (plus **`:raw`** variants).
  Above a configurable bulk key count, it falls back to **broad type** invalidation.
- **Create** still uses a **broad type sweep** (**`InvalidateQueryCacheAsync<TDbModel>()`**) so new rows invalidate all list/query caches for that entity without relying on
  instance tags that did not exist before.
- **Projection-shape busting** — optional **`QueryCacheInvalidation.InvalidateProjectedQueriesByProjShapeAsync`** invalidates every **`QueryProject`** page tagged with a given *
  *`projshape:…`** (useful for frontend grids keyed by projection shape).
- **Per–root-entity isolation across unrelated types** — invalidation for **Person** does not clear **Order** caches; unrelated aggregates use different **`entity:`** tags and
  keys.
- **Includes and related entity types** — **`GET`**, **`/QueryConcrete`**, and **`/QueryProject`** attach tags for **`GetReferencedTypes`** and per-entity instance tags. Updating a
  **child
  ** row invalidates **parent** cached reads that carried that child’s **`entity:{child}:{pk}`** tag (granular path) or the child type’s broad tag when using *
  *`InvalidateQueryCacheAsync<Child>()`**.

### OpenTelemetry and Observability

Every operation is instrumented:

- `api.crud.duration` — execution time per operation
- `api.crud.requests` / `api.crud.success` / `api.crud.failure` — counters
- `api.crud.result_count` — items returned
- Structured logging with correlation IDs
- Trace and span IDs in error responses

### Database Agnostic

Lyo works with any EF Core provider:

- PostgreSQL (Npgsql)
- SQL Server
- MySQL / MariaDB
- SQLite
- Oracle
- Cosmos DB

The only provider-specific feature is the `Regex` comparator, which uses PostgreSQL functions. All other features work identically across providers.

### Works Beyond EF Core

The query engine operates on `IQueryable<T>`, which means it works with:

- In-memory collections (`List<T>.AsQueryable()`)
- Custom `IQueryable` implementations
- Any data source that implements the interface
- Unit tests without a database

Use it in background jobs, data pipelines, report generation — anywhere you filter and page over collections.

---

## Performance

Benchmarked on a laptop (Intel Core Ultra 7 155U, 62 GB RAM) with API, PostgreSQL, and the load generator all running on the same machine. Latest archived k6 suite: **July 2026**
(`k6/framework-person/results/20260726-235847/`), the full 12-suite matrix (QueryConcrete / QueryProject / root `/Query` × load/stress/spike/soak, **~1.35M requests** total). **
`CacheOptions:QueryCacheTagGranularity`** was **`Broad`** (non-granular tags, default) and the harness bypasses caches. See `K6_BENCHMARK_ANALYSIS.md` for methodology and caveats.
Production deployments on dedicated infrastructure would perform better.

### Lightweight Queries (projections, root joins, computed fields)

QueryProject and root `/Query` suites under load/spike/soak (July 2026 archive):

| Metric          | Result (July 2026 archive)                               |
|-----------------|----------------------------------------------------------|
| Scenario spread | **~12–23 ms** avg (root load → projection spike)         |
| p95 latency     | **~31–65 ms** (scenario-dependent; medians **~8–13 ms**) |
| p99 latency     | **~42–84 ms**                                            |
| Throughput      | 7–60 req/s sustained (arrival-rate capped)               |
| Success rate    | **100%**                                                 |

Root flat select averages **~3 ms**; scalar computed projection (`fullName`) **~4 ms**; a chained three-table root join with exact count stays **~23 ms** avg under load — on shared
laptop hardware with cache-bypassing pagination.

### Heavy Navigation Queries (full entities, 1–3 include branches, 100–300 rows)

Full-entity `QueryConcrete` suites, `realistic_include`/`heavy_include` cases (person → contact_addresses → address, up to 3 include branches):

| Metric                  | Result (July 2026 archive)                                            |
|-------------------------|-----------------------------------------------------------------------|
| Steady-state average    | **~96–119 ms** (spike/soak)                                           |
| Steady-state p95        | ~224–233 ms                                                           |
| Stress (ramp to 40 VUs) | ~659 ms avg / ~1.79 s p95                                             |
| Throughput              | **71 req/s** mixed-case stress (**34K** requests in the stress stage) |
| Success rate            | status/shape **100%**; checks **99.98%** under stress                 |

API, Postgres, and k6 share CPU under load, so the stress tail is pessimistic. Lighter `QueryConcrete` cases (baseline, filter+sort, subquery, QueryNode tree) stay **~14–22 ms
p95** under load; see `K6_BENCHMARK_ANALYSIS.md` and the dashboard for per-case hotspots.

### Sustained Load (three 2-hour soak tests)

| Metric          | Result (July 2026 archive)                                       |
|-----------------|------------------------------------------------------------------|
| Total requests  | **1,178,393** (340K Query · 406K QueryProject · 432K root Query) |
| Duration        | 3 × 2 hours (one soak per endpoint family)                       |
| Average latency | **12–45 ms** per endpoint family                                 |
| Success rate    | **100%** (all k6 checks)                                         |
| Errors          | **0**                                                            |

Zero HTTP failures across all three soak windows. Mixed query cases with periodic heavy-include shapes; tail latency includes intentional heavy shapes — p95 stayed at **181 ms**
(Query), **65 ms** (QueryProject), and **31 ms** (root Query).

### How This Compares

| Framework                          | Typical dynamic read p95 (industry ballpark)                                            | Notes                                 |
|------------------------------------|-----------------------------------------------------------------------------------------|---------------------------------------|
| **Lyo** (archived k6)              | **~31–65 ms** projection/root scenarios (medians ~8–13 ms; lightest shapes ~4–6 ms p95) | Expression trees + EF Core + Postgres |
| Hasura / PostgREST                 | 5–30 ms                                                                                 | No ORM — direct DB to JSON            |
| Typical EF Core API (hand-written) | 50–200 ms                                                                               | Manual filter/sort implementation     |
| Django REST Framework              | 50–300 ms                                                                               | Python ORM                            |
| Spring Boot + JPA                  | 30–150 ms                                                                               | Hibernate                             |
| Ruby on Rails                      | 80–400 ms                                                                               | ActiveRecord                          |

Lyo stays in the **same order of magnitude** as thin Postgres-to-JSON gateways for comparable read shapes on this hardware, while keeping full EF Core mapping, navigation fixup,
and the dynamic query surface.

---

## Generic by Design

Lyo is built on five type parameters:

```
TDbContext    — your EF Core DbContext
TDbEntity     — your database entity
TRequest      — your request DTO
TResponse     — your response DTO
TKey          — your primary key type (Guid, int, long, string)
```

Define your entity, map your DTOs, call `Build()`. Lyo handles everything between the HTTP request and the database.

```csharp
// Services
builder.Services.AddLyoQueryServices();
builder.Services.AddLyoCrudServices<AppDbContext>();

// Endpoints
app.CreateBuilder<AppDbContext, OrderEntity, OrderRequest, OrderResponse, int>("orders", "Orders")
    .WithCrud(ApiFeatureSet.FullCrud)
    .RequireAuthorization()
    .Build();

app.CreateBuilder<AppDbContext, ProductEntity, ProductRequest, ProductResponse>("products", "Products")
    .WithCrud(ApiFeatureSet.ReadOnly)
    .AllowAnonymous()
    .Build();

app.CreateBuilder<AppDbContext, CustomerEntity, CustomerRequest, CustomerResponse>("customers", "Customers")
    .WithCrud(ApiFeatureSet.CoreAll + ExportApiFeature.Instance)
    .RequireAuthorization("AdminPolicy")
    .WithDelete(config => config.Auth = EndpointAuth.RequireRole("SuperAdmin"))
    .Build();
```

Three entities. Full APIs. Under 20 lines.

---

## Thread Safe

All services are designed for concurrent access. No external synchronization required. Proven under sustained concurrent load with zero race conditions or data corruption across
301K+ requests.

---

## Architecture

```
HTTP Request
    │
    ▼
┌─────────────────────────────────┐
│  ApiEndpointBuilder             │  ← Route mapping, auth, feature flags
│  (17 minimal API endpoints)     │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  CRUD Services                  │  ← Create, Query, Update, Patch,
│  (Before/After hooks, metrics)  │     Delete, Upsert, Export
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  Query Engine                   │  ← Expression trees, filter compilation,
│  (IQueryable<T>)                │     subquery pushdown, projection
└────────────┬────────────────────┘
             │
        ┌────┴────┐
        ▼         ▼
┌────────────┐ ┌─────────────┐
│  EF Core   │ │  Any        │
│  DbContext │ │  IQueryable │
└────────────┘ └─────────────┘
        │
        ▼
   ┌──────────┐
   │ Database │  ← PostgreSQL, SQL Server, MySQL, SQLite, etc.
   └──────────┘

Cross-cutting: Cache (FusionCache / Local) │ OpenTelemetry │ Logging │ Auth
```

---

## FAQ

**Is this another OData?**
No. OData uses URL query strings with a flat syntax that breaks down for complex queries. Lyo uses structured JSON request bodies that support nested logical trees, subqueries, and
regex — things OData can't express. Lyo also provides full CRUD, bulk operations, export, and caching. OData provides none of those.

**Is this GraphQL?**
No. Lyo is REST-native. No schema definitions, no resolvers, no DataLoader pattern, no client-side libraries required. Any HTTP client that can POST JSON can use Lyo. You get
GraphQL-level query power without the GraphQL complexity.

**Do I need PostgreSQL?**
No. Lyo works with any EF Core provider. The only PostgreSQL-specific feature is the `Regex` comparator. All other features — filters, sorts, includes, subqueries, projections,
CRUD, bulk operations — work on any database.

**Can I add custom business logic?**
Yes. Every operation supports Before and After hooks that run in-process. You have full access to the entity, the HttpContext, and any injected services.

**What if I only need the query engine?**
The query engine works independently on any `IQueryable<T>`. Use it in services, background jobs, or data pipelines — no HTTP endpoints required.

**What about existing entities?**
Lyo works with your existing EF Core entities and DbContext. No base classes to inherit, no interfaces to implement, no attributes to add. Point it at your entity and build.

**Is it production-ready?**
Yes. Benchmarked under sustained load (301K requests, 2 hours, zero HTTP failures), with OpenTelemetry instrumentation, structured logging, and clean error responses with
trace/span IDs.