# Lyo.Query

Translates [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) `WhereClause` ASTs into
LINQ `IQueryable<T>` expressions, applies multi-key sort, and explains matches in
memory. The same JSON payload that crosses the wire can execute in the database (EF
Core translates the expression tree to SQL) or be replayed against materialized
entities for tests, export pipelines, or sub-graph filtering.

The library is deliberately **EF-agnostic** at the boundary — it manipulates
`IQueryable` and reflection metadata. EF surfaces only in the caller's host when a
`DbSet`-backed queryable is handed in.

Targets `net10.0`.

## Services

### `IWhereClauseService` / `BaseWhereClauseService`

- `ApplyWhereClause<TEntity>(IQueryable<TEntity>, WhereClause?, bool includeSubClauses = true)`
  — walks group and condition nodes, honors AND/OR, and compiles comparators
  (`Equals`, `NotEquals`, `Contains`/`NotContains`, `StartsWith`/`EndsWith` and their
  negations, `GreaterThan*` / `LessThan*` — collection navigations compare against
  the collection count — `In` / `NotIn`, `Regex` / `NotRegex`). With
  `includeSubClauses = false` it omits `WhereClause.SubClause` chains so callers can
  split execution (e.g. root pass in SQL, sub-tree refinement in memory). Cached
  compiled predicates and matchers are stored under `filter_ef_predicate` /
  `filter_matcher` cache keys.
- `SortByProperty<TEntity>(queryable, propertyName, direction?)` — single-key
  `OrderBy` / `OrderByDescending` over a dotted path (defaults to descending when
  direction is `null`).
- `ApplyOrdering<TEntity>(queryable, sortByProps, defaultOrder, defaultDirection)` —
  applies a `SortBy[]` collection in `Priority` order, falling back to the list
  order, and attaches a default tie-break expression to keep paging stable when
  user-supplied columns compare equal.
- `MatchesWhereClause<TEntity>(entity, whereClause)` — compiles the same AST and
  evaluates it against a single instance.
- `ExplainMatch<TEntity>(entity, whereClause)` — returns
  `WhereClauseExplainResult` (per-node pass/fail, blocking path, OR-branch
  outcomes). Default interface implementation throws
  `NotSupportedException`; `BaseWhereClauseService` implements it for **in-memory**
  evaluation only — SQL-backed pipelines do not support explanation in-process.
- `TryValidatePropertyPath<TEntity>(propertyName, out errorMessage)` — fast pre-flight
  for sort keys and where-clause fields. Invalid paths throw `InvalidQueryException`
  when used at runtime.
- `GetCollectionIncludePathsForWhereClause<TEntity>(whereClause)` — distinct,
  case-insensitive list of navigation paths that traverse collections; use these as
  EF `Include` chains before running in-memory sub-clause evaluation on the loaded
  graph.

### `IPropertyComparisonService` / `PropertyComparisonService`

`GetPropertyDifferences<TEntity, TOther>(entity, newData)` walks public writable
properties on `TEntity` that have a same-named readable property on `TOther` and
returns a dictionary of property name → proposed new value for properties whose
values differ. Comparison strategies (direct equality, enum/string coercion,
`Convert.ChangeType`) are inferred per property and cached via `ICacheService`. Used
by patch/update pipelines for diff payloads.

### `IValueConversionService` / `ValueConversionService`

- `ConvertToTargetType(value, targetType)` — coerces JSON literals into the CLR
  property type (primitives, nullables, `Guid`, enums, date/time, lists).
- `GetUnderlyingType(type)` — strips `Nullable<T>`.
- `IsObjectEnumerable(value)` — returns `true` for non-`string`, non-`byte[]`
  `IEnumerable` instances.

> **Hosting with `Lyo.Api`:** `Lyo.Api` exposes a richer `ITypeConversionService`
> that extends `IValueConversionService` with EF-specific helpers (primary-key
> extraction, etc.). When hosting `Lyo.Api`, register its conversion service and
> call:
>
> ```csharp
> services.AddLyoQueryServices(registerValueConversion: false);
> ```
>
> Otherwise you will have two `IValueConversionService` registrations with
> ambiguous behavior.

## Registration

```csharp
using Lyo.Query;

services.AddLocalCache(/* … */);     // or AddFusionCache(...)
services.AddLyoQueryServices();      // registers ValueConversionService + property + where-clause
```

`AddLyoQueryServices` registers `IValueConversionService` (unless suppressed),
`IPropertyComparisonService`, and `IWhereClauseService` as singletons. It requires
`ICacheService` and `CacheOptions` because property comparison and where-clause
metadata are cached on hot paths.

## Metrics

When `IMetrics` is registered, `BaseWhereClauseService` emits durations and counters
under `Lyo.Query.Constants.Metrics`:

| Name                                       | Kind            |
|--------------------------------------------|-----------------|
| `query.filter.apply_query_node.duration`   | histogram/timer |
| `query.filter.apply_query_node.success`    | counter         |
| `query.filter.sort_by_property.duration`   | histogram/timer |
| `query.filter.sort_by_property.success`    | counter         |
| `query.filter.apply_ordering.duration`     | histogram/timer |
| `query.filter.apply_ordering.success`      | counter         |
| `query.filter.matches_query_node.duration` | histogram/timer |
| `query.filter.matches_query_node.success`  | counter         |
| `query.filter.sort_by_count`               | gauge           |

Tag keys: `entity_type`, `operation`.

## Mental model vs `Lyo.Api`

| Concern                                                        | `Lyo.Query`          | `Lyo.Api`                                |
|----------------------------------------------------------------|----------------------|------------------------------------------|
| JSON DTOs / builders                                           | `Lyo.Query.Models`   | Endpoints compose those DTOs             |
| AST → LINQ                                                     | **This package**     | Wires authenticated CRUD/query endpoints |
| EF Core specifics (tracking, Include graphs, compiled queries) | Stays mostly outside | Implemented in the mapper pipeline       |

`Lyo.Query` is reusable library logic; `Lyo.Api` adds HTTP, authorization, and EF
integration.

## Related projects

- [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) — DTOs, enums, builders.
- [`Lyo.Query.Web.Components`](../Lyo.Query.Web.Components/README.md) — Blazor
  query workbench.
- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) — production query
  endpoints and result caching.
- [`Lyo.Cache`](../../../Core/Cache/Lyo.Cache/README.md) — required cache backend.
