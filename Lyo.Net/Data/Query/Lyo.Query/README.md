# Lyo.Query

Translates [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) **`WhereClause`** ASTs into **LINQ `IQueryable<T>`** expressions—so the same JSON/query payload you accept over HTTP can execute **in the database** (EF Core translates to SQL) or be **replayed in-memory** against materialized entities for tests, export pipelines, or subgraph filtering.

This assembly is **deliberately EF-agnostic** at the boundary: it manipulates **`IQueryable`** and reflection metadata; EF shows up only in *your* host when you hand it a DbSet-backed queryable.

## Services

### `IWhereClauseService` / `BaseWhereClauseService`

**`ApplyWhereClause<TEntity>(IQueryable<TEntity> source, WhereClause? clause, bool includeSubClauses = true)`**

- Walks **group** and **condition** nodes, honor **AND/OR**, compile comparators (`Equals`, `Contains`, `In`, `Regex`, etc.—see `ComparisonOperatorEnum` on the model side).
- **`includeSubClauses`** — when **`false`**, omits nodes that require **sub-query** / **correlated** shapes so you can split work (e.g. root SQL pass vs in-memory refinement) in advanced hosts.
- Uses **`IPropertyComparisonService`** for how each leaf maps to expression trees, and **`IValueConversionService`** to coerce JSON primitives into the CLR property type before comparisons fire.

**Sorting & ordering**

- **`SortByProperty`** / **`ApplyOrdering`** interpret API **`SortBy`** lists, validate paths, and attach **ThenBy** chains with a **default tie-break** expression you supply (prevents unstable pagination when two rows compare equal on user columns).

**Validation & includes**

- **`TryValidatePropertyPath<TEntity>`** — fail fast with a human-readable reason when a client sends **`"Nested.Collection.Prop"`** that does not exist or is not traversable.
- **`GetCollectionIncludePathsForWhereClause<TEntity>`** — returns navigation paths you should **`Include`** / expand before running **in-memory** subtree filters (anything involving collection navigation without a SQL translation path).

**In-memory evaluation**

- **`MatchesWhereClause<TEntity>(TEntity entity, WhereClause? clause)`** evaluates the same AST against a single instance (handy for validating a row post-load or for tests).

**Explanation**

- **`ExplainMatch<TEntity>(...)`** documents per-node pass/fail along the AST. The default contract **throws `NotImplementedException`** for SQL-backed paths; **`BaseWhereClauseService`** supports explanation for **in-memory** evaluation only (see XML remarks on the interface—do not assume SQL pipelines implement this).

### `IPropertyComparisonService` / `PropertyComparisonService`

Resolves **comparison strategies** and **per-property metadata caches** (including interaction with **`ICacheService`** for **`PropertyComparisonInfo`** and type shape information). This is the layer that answers: “Given this property type and comparator, which expression shape do we emit?”

### `IValueConversionService` / `ValueConversionService`

Coerces request payload values to **target property types**: primitives, nullable forms, **`Guid`**, enums, **date/time**, lists (excluding **`string`** and **`byte[]`** false positives per `IsObjectEnumerable`), …

**Important:** [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) exposes a richer **`ITypeConversionService`** that **extends** this interface with EF-specific helpers (primary key extraction, etc.). When hosting **`Lyo.Api`**, register its conversion service and call:

```csharp
services.AddLyoQueryServices(registerValueConversion: false);
```

Otherwise you will have **two** `IValueConversionService` registrations and ambiguous behavior.

## Registration

```csharp
using Lyo.Query;

services.AddLocalCache(...);        // or AddFusionCache(...)
services.AddLyoQueryServices();     // registers ValueConversionService + property + where-clause services
```

**`AddLyoQueryServices` requires `ICacheService` + `CacheOptions`** because property comparison and conversion paths consult cached reflection metadata to avoid hammering **`PropertyInfo`** on hot query paths.

## Mental model vs `Lyo.Api`

| Concern | `Lyo.Query` | `Lyo.Api` |
|---------|---------------|-----------|
| JSON DTOs / builders | **`Lyo.Query.Models`** | Endpoints compose those DTOs |
| AST → LINQ | **This package** | Wires authenticated CRUD/query endpoints |
| EF Core specifics (tracking, Include graphs, compiled queries) | Stays mostly outside | Implemented in mapper pipeline |

So: **`Lyo.Query` is reusable library logic**; **`Lyo.Api` is HTTP + authorization + EF integration**.

## See also

- [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) — `WhereClauseBuilder`, projection DTOs, serialization names.
- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) — production query endpoints, caching, projection.
- [`Lyo.Cache`](../../../Core/Cache/Lyo.Cache/README.md) — why metadata caching appears in query stack.
