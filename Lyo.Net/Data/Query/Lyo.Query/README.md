# Lyo.Query

Execution engine for [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) filter trees. `IWhereClauseService` / `BaseWhereClauseService` translates polymorphic `WhereClause` ASTs (`condition` / `group`, optional `SubClause`) into LINQ expression trees on `IQueryable<T>`, applies multi-key `SortBy`, and can evaluate / explain the same AST against a loaded entity in memory.

**What this package is:** AST → expression / matcher. **What it is not:** HTTP endpoints, `QueryConcreteReq` / `ProjectionQueryReq` / `QueryReq` builders, or query-*result* caching — those live in Models + [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md).

EF-agnostic at the boundary (works on any `IQueryable`, including EF `DbSet` and in-memory). Requires `ICacheService` + `CacheOptions` for compiled predicate / matcher / metadata caches.

Targets `net10.0`.

## Features

- **AST → `IQueryable`** — `ApplyWhereClause` builds EF-translatable (or provider) expression trees from `WhereClause`
- **16 comparators** — Equals / NotEquals / Contains* / StartsWith* / EndsWith* / Greater* / Less* / In / NotIn / Regex / NotRegex
- **Collection paths** — `Lines.Quantity` → `Any(...)` on collection elements; bare collection / `.Count` style paths compare against count
- **Two-phase SubClause** — `includeSubClauses: false` for SQL root pass; load includes then match sub-tree in memory
- **Multi-key sort** — `SortByProperty` + `ApplyOrdering` with `Priority` and stable default tie-break
- **In-memory match + explain** — `MatchesWhereClause` / `ExplainMatch` with blocking path and OR-branch detail
- **ICache hot path** — compiled EF predicates (`filter_ef_predicate:*`), matchers (`filter_matcher:*`), sort keys, property-path metadata
- **Path validation** — `TryValidatePropertyPath` / `InvalidQueryException` for bad dotted paths
- **Contains→Regex coalesce** — adjacent `Contains` leaves on the same string field can merge into one `Regex` alternation
- **Property diffing** — `IPropertyComparisonService` for patch/update pipelines
- **Value coercion** — `IValueConversionService` for JSON literals → CLR property types (suppressible when `Lyo.Api` owns conversion)

## Examples

### Register services

```csharp
using Lyo.Query;

services.AddLocalCache(/* … */); // or AddFusionCache(...)
services.AddLyoQueryServices();
// registers IValueConversionService, IPropertyComparisonService, IWhereClauseService (singletons)

// Hosting Lyo.Api (richer ITypeConversionService already registered):
// services.AddLyoQueryServices(registerValueConversion: false);
```

### Apply WhereClause to IQueryable

```csharp
using Lyo.Query;
using Lyo.Query.Models;
using Lyo.Query.Models.Builders;

var where = WhereClauseBuilder.And()
    .Equals("Status", "Active")
    .GreaterThan("Age", 18)
    .Build();

IQueryable<Person> people = db.People.AsQueryable();
var filtered = whereClauseService.ApplyWhereClause(people, where);
var page = await filtered.Skip(20).Take(20).ToListAsync(ct);
```

### Two-phase SubClause (SQL then in-memory)

```csharp
// Root runs as SQL; SubClause refined after Include load
var where = WhereClauseBuilder.And()
    .Equals("Status", "Open")
    .AddSubClause(sub => sub.Contains("Tags", "vip"))
    .Build();

var sqlPhase = whereClauseService.ApplyWhereClause(db.Orders, where, includeSubClauses: false);
var includePaths = whereClauseService.GetCollectionIncludePathsForWhereClause<Order>(where);
// apply Include(includePaths…) then materialize
var rows = await sqlPhase /* .Include(...) */ .ToListAsync(ct);
var matched = rows.Where(o => whereClauseService.MatchesWhereClause(o, where)).ToList();
```

### Multi-key ordering

```csharp
using Lyo.Query.Models;
using Lyo.Common.Enums;

var sorted = whereClauseService.ApplyOrdering(
    queryable: people,
    sortByProps:
    [
        new SortBy { PropertyName = "LastName", Direction = SortDirection.Ascending, Priority = 0 },
        new SortBy { PropertyName = "FirstName", Direction = SortDirection.Ascending, Priority = 1 },
    ],
    defaultOrder: p => p.Id,
    defaultSortDirection: SortDirection.Ascending);
```

### Match and ExplainMatch

```csharp
var entity = await db.People.AsNoTracking().FirstAsync(ct);

if (whereClauseService.MatchesWhereClause(entity, where))
{
    var explain = whereClauseService.ExplainMatch(entity, where);
    // explain.Passed, BlockingPath, FailureSummary, Nodes (per-condition ActualValueSummary)
    // In-memory only — not for SQL-only pipelines
}
```

### Validate paths + collection includes

```csharp
if (!whereClauseService.TryValidatePropertyPath<Person>("Addresses.City", out var err))
    throw new InvalidOperationException(err);

foreach (var path in whereClauseService.GetCollectionIncludePathsForWhereClause<Person>(where))
    query = query.Include(path); // e.g. "Addresses" for "Addresses.City"
```

### WhereClauseUtils fingerprints

```csharp
using Lyo.Query.Services.WhereClause;

var hash = WhereClauseUtils.GetWhereClauseTreeHash(where); // stable structural fingerprint
var hasSub = WhereClauseUtils.HasAnySubClause(where);
if (WhereClauseUtils.TryExtractConditions(where, out var conditions, out var op))
{
    // flat And/Or leaves — false when SubClause or unsupported nesting present
}
```

### Property diff for patch

```csharp
var diffs = propertyComparisonService.GetPropertyDifferences(entity, patchDto);
// Dictionary<propertyName, newValue> — only changed writable props
```

## Benchmarks

- Portfolio suite: `query`

## Package map

| Package | Responsibility |
| --- | --- |
| **`Lyo.Query` (this)** | `WhereClause` → LINQ / matcher; sort; explain; path metadata; `ICache` for compiled trees |
| [`Lyo.Query.Models`](../Lyo.Query.Models/README.md) | AST types, `QueryConcreteReq` / `ProjectionQueryReq` / `QueryReq`, builders, enums |
| [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) | `POST …/QueryConcrete`, `/QueryProject`, root `/Query`; **query result** caching; projection SQL |
| [`Lyo.Query.Web.Components`](../Lyo.Query.Web.Components/README.md) | Blazor query workbench |

Builders and endpoint payloads are documented on **Models** / **Api**. This doc covers the runtime translator.

## IWhereClauseService

`BaseWhereClauseService` is the default DI implementation.

| Member | Behavior |
| --- | --- |
| `ApplyWhereClause<T>(source, where, includeSubClauses = true)` | Compiles AST → `Expression<Func<T,bool>>`, caches under `filter_ef_predicate:…`, applies `.Where`. `includeSubClauses: false` skips `SubClause` chains (SQL phase). |
| `SortByProperty<T>(source, propertyName, direction?)` | Single dotted path `OrderBy` / `OrderByDescending` (default Desc when null). Collections order by **count**. |
| `ApplyOrdering<T>(…, sortByProps, defaultOrder, defaultSortDirection)` | Multi-key sort by `SortBy.Priority` then list order; always attaches default tie-break for stable paging. |
| `MatchesWhereClause<T>(entity, where)` | Compiled in-memory matcher (`filter_matcher:…` cache). |
| `ExplainMatch<T>(entity, where)` | `WhereClauseExplainResult` — per-node pass/fail, `BlockingPath`, OR-branch outcomes, `SubClause` chains. **In-memory only** (default interface throws). |
| `GetCollectionIncludePathsForWhereClause<T>(where)` | Distinct navigation prefixes that cross collections (for EF `Include` before sub-clause match). |
| `TryValidatePropertyPath<T>(name, out error)` | Preflight for sort / filter fields. |

Invalid paths / operators → `InvalidQueryException` (`Lyo.Query.Models.Exceptions`).

## Comparators and path semantics

Operators come from `ComparisonOperatorEnum` on each `ConditionClause`:

| Operator | Notes |
| --- | --- |
| `Equals` / `NotEquals` | Simple comparison; null-safe rules enforced |
| `GreaterThan` / `GreaterThanOrEqual` / `LessThan` / `LessThanOrEqual` | On scalars: value compare. On **collection** navigations: compare against **collection count** |
| `Contains` / `NotContains` / `StartsWith` / `EndsWith` / negations | String methods when the target type supports them |
| `In` / `NotIn` | Value may be a list or CSV string |
| `Regex` / `NotRegex` | String regex; trivial patterns like `.*` short-circuit to `true` |

**Dotted paths**

- Scalar: `Status`, `Address.City`
- Into collection: `Lines.Quantity` → `lines.Any(e => e.Quantity …)`
- Collection count: path metadata `IsCountPath` builds `Enumerable.Count` then compares

**Group optimization:** within an AND/OR group, multiple `Contains` leaves on the same string field may be coalesced into a single case-insensitive `Regex` alternation before expression build.

## Two-phase SubClause execution

`WhereClause.SubClause` (on a condition or group) supports split execution — used heavily by `Lyo.Api` query pipelines:

1. **Phase 1 (DB)** — `ApplyWhereClause(..., includeSubClauses: false)` so only the primary predicate becomes SQL.
2. **Load** — `GetCollectionIncludePathsForWhereClause` → EF `Include` for collection segments referenced by the sub-tree.
3. **Phase 2 (memory)** — `MatchesWhereClause` / `ExplainMatch` on materialized entities with the **full** tree (`includeSubClauses` default true).

Helpers:

- `WhereClauseUtils.HasAnySubClause(node)` — detect whether a two-phase path is needed
- `WhereClauseUtils.TryExtractConditions` — flatten simple And/Or trees for projection-level filtering (returns `false` if any `SubClause`)
- `WhereClauseUtils.GetWhereClauseTreeHash` — structural fingerprint for cache keys / logging (not cryptographic)

```csharp
var where = WhereClauseBuilder.And()
    .Equals("Age", 10)
    .AddSubClause(sub => sub.AddAnd(s => s.Equals("Name", "Alice")))
    .Build();
```

## ICache usage (predicate / matcher cache)

`AddLyoQueryServices` **requires** `ICacheService` + `CacheOptions` (e.g. `AddLocalCache` / `AddFusionCache`). This is **not** the same as `Lyo.Api` query-*result* caching (`QueryOptions.CacheQueryResultsAsUtf8Payload`).

| Cache key prefix / pattern | Stores |
| --- | --- |
| `filter_ef_predicate:…` | Compiled `Expression` / predicate for `ApplyWhereClause` (keyed by entity + tree + `includeSubClauses`) |
| `filter_matcher:…` | Compiled in-memory matcher for `MatchesWhereClause` |
| Sort-key lambdas | Per `(TEntity, propertyName)` order key selectors |
| Property-path metadata | Resolved dotted-path segments, collection indexes, final CLR types |
| `SubQueryIncludePaths_…` | Include-path lists for a where tree |
| Property-comparison strategies | Per-property equality strategy for `IPropertyComparisonService` |

Tags typically include the entity CLR type so hosts can invalidate by type when schemas change.

**Api result cache** (optional UTF-8 payload entries for `/QueryConcrete` + `/QueryProject`) is documented under [Lyo.Api — Query result caching](../../../Integration/Api/Lyo.Api/README.md#query-result-caching).

## IPropertyComparisonService

`GetPropertyDifferences<TEntity, TOther>(entity, newData)` walks public **writable** properties on `TEntity` that have a same-named **readable** property on `TOther`, compares with an inferred strategy (direct equality, enum/string coercion, or `Convert.ChangeType`), and returns `Dictionary<string, object?>` of **changed** property → proposed new value.

Used by patch/update pipelines (e.g. `Lyo.Api`) to build minimal diffs. Strategies are cached via `ICacheService`.

## IValueConversionService

- `ConvertToTargetType(value, targetType)` — JSON literals → CLR (primitives, nullables, `Guid`, enums, date/time, lists)
- `GetUnderlyingType(type)` — strip `Nullable<T>`
- `IsObjectEnumerable(value)` — non-`string` / non-`byte[]` `IEnumerable`

When hosting **`Lyo.Api`**, register Api’s richer `ITypeConversionService` (extends this interface) and call:

```csharp
services.AddLyoQueryServices(registerValueConversion: false);
```

Otherwise two `IValueConversionService` registrations fight.

## Registration

```csharp
services.AddLocalCache(/* … */); // prerequisite
services.AddLyoQueryServices(registerValueConversion: true);
```

Registers as **singletons**:

- `IValueConversionService` → `ValueConversionService` (unless `registerValueConversion: false`)
- `IPropertyComparisonService` → `PropertyComparisonService`
- `IWhereClauseService` → `BaseWhereClauseService`

Optional: `IMetrics` / `ILogger` are taken from DI when present (`Constants.Metrics.*`).

## Metrics

When `IMetrics` is registered, `BaseWhereClauseService` emits under `Lyo.Query.Constants.Metrics`:

| Name | Kind |
| ------------------------------------------ | --------------- |
| `query.filter.apply_query_node.duration` | histogram/timer |
| `query.filter.apply_query_node.success` | counter |
| `query.filter.sort_by_property.duration` | histogram/timer |
| `query.filter.sort_by_property.success` | counter |
| `query.filter.apply_ordering.duration` | histogram/timer |
| `query.filter.apply_ordering.success` | counter |
| `query.filter.matches_query_node.duration` | histogram/timer |
| `query.filter.matches_query_node.success` | counter |
| `query.filter.sort_by_count` | gauge |

Tags: `entity_type`, `operation` (`Constants.Metrics.Tags`).

## Related docs

- DTOs / builders / request shapes → [`Lyo.Query.Models`](../Lyo.Query.Models/README.md)
- HTTP endpoints, projection SQL, **result** caching → [`Lyo.Api` Query & Request Builders](../../../Integration/Api/Lyo.Api/README.md#query--request-builders)
- Blazor workbench → [`Lyo.Query.Web.Components`](../Lyo.Query.Web.Components/README.md)
- Portfolio benchmarks suite → `query`

## Links

- [Lyo.Query.Models](../Lyo.Query.Models/README.md)
- [Lyo.Api query builders](../../../Integration/Api/Lyo.Api/README.md#query--request-builders)
- [Lyo.Api query result caching](../../../Integration/Api/Lyo.Api/README.md#query-result-caching)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Query.Models` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)