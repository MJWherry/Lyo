# Lyo.Query.Evaluation

This slice of the query engine turns a `WhereClause` into an expression tree and evaluates it. Every `IWhereClauseService` member is implemented here except caching, which `Lyo.Query` adds by overriding this package's virtual members.

The split exists because `Lyo.Validation` needs exactly this and nothing else. Evaluating a `ValidationSchema` used to mean referencing `Lyo.Query`, which pulled in cache, encryption, hashing, key store, compression, and health. `WhereClauseEvaluator` uses a `ConcurrentDictionary` instead of the shared cache, so a consumer that only wants to know whether an object matches a clause pays for expression building and nothing else.

Metrics are recorded here rather than in `Lyo.Query`, because `Lyo.Metrics` has no NuGet dependencies of its own. `IMetrics` is an optional constructor parameter that defaults to `NullMetrics.Instance`, so the uninstrumented path costs nothing.

## Features

- **In-memory matching.** `MatchesWhereClause` against a compiled predicate.
- **Match explanation.** `ExplainMatch` produces the `WhereClauseExplainNode` tree that validation turns into errors.
- **Queryable translation.** `ApplyWhereClause`, `SortByProperty`, and `ApplyOrdering` against `IQueryable<T>`.
- **Path resolution.** Dotted paths, including collection segments, go through `TryValidatePropertyPath` and `SharedEntityMetadataCache`.
- **Value conversion.** Filter literals go through `IValueConversionService` / `ValueConversionService`, which delegates to `TypeConversion`.
- **Metrics.** Durations, success counters, and errors under `Lyo.Query.Constants.Metrics`, tagged `entity_type`. Pass an `IMetrics` to opt in; omit it and nothing is recorded or allocated.
- **Extension points.** `GetMatcher`, `GetEfPredicate`, and `GetIncludePaths` are virtual so a host can plug in its own caching.

## Examples

### Evaluate a clause without Lyo.Query

```csharp
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;

IWhereClauseService where = new WhereClauseEvaluator(new ValueConversionService());

var clause = new ConditionClause("Status", ComparisonOperatorEnum.In, new[] { "Open", "Pending" });
if (!where.MatchesWhereClause(order, clause))
    Console.WriteLine(where.ExplainMatch(order, clause).FailureSummary);
```

### Smallest DI wiring

```csharp
services.TryAddSingleton<IValueConversionService, ValueConversionService>();
services.TryAddSingleton<IWhereClauseService, WhereClauseEvaluator>();
```

### Record metrics without Lyo.Query

```csharp
using Lyo.Metrics;

// Pass an IMetrics to instrument evaluation. Omitting it uses NullMetrics.Instance.
var metrics = new MetricsService();
IWhereClauseService where = new WhereClauseEvaluator(new ValueConversionService(), metrics);

where.MatchesWhereClause(order, clause);

var successes = metrics.GetCounterValue(
    Lyo.Query.Constants.Metrics.MatchesWhereClauseSuccess,
    tags: [(Lyo.Query.Constants.Metrics.Tags.EntityType, nameof(Order))]);
```

## How this relates to `Lyo.Query`

`Lyo.Query.BaseWhereClauseService` subclasses `WhereClauseEvaluator` and overrides the protected `GetMatcher` / `GetEfPredicate` / `GetIncludePaths` members to go through `ICacheService`. It also forwards the injected `IMetrics` only when `CacheOptions.EnableMetrics` is set, since that flag lives in `Lyo.Cache` and cannot be read from here. `IWhereClauseService` signatures are the same either way, so nothing downstream can tell which one it got.

Take this package when the consumer only evaluates clauses. Take `Lyo.Query` when the consumer wants the shared cache and the rest of the request/paging surface.

## `netstandard2.0` caveat

On `net` targets, `IWhereClauseService.ExplainMatch` ships a default implementation so database-only services need not implement it. `netstandard2.0` has no default interface implementations, so the member is abstract there and implementers must supply it.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)