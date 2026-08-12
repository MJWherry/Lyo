# Lyo.Query.Models

Filter / sort / projection DTOs and fluent builders for query requests. The same `WhereClause` tree is consumed by [`Lyo.Query`](../Lyo.Query/README.md) (turns it into LINQ on
`IQueryable`) and by [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) endpoints (`QueryConcrete`, `QueryProject`, root `Query`), so HTTP clients and in-process tests build
queries the same way.

Covers the polymorphic where-clause AST, `QueryConcreteReq` / `ProjectionQueryReq` / `QueryReq`, sort + explain result shapes, and builders (`WhereClauseBuilder`,
`QueryConcreteReqBuilder`, `ProjectionQueryReqBuilder`, `QueryReqBuilder`).

> **Caching:** Result caching for `POST …/QueryConcrete` and `POST …/QueryProject` is configured > on the API host (`QueryOptions.CacheQueryResultsAsUtf8Payload`, `ICacheService` /
> Fusion), > not on these DTOs. See *Query result caching* in the [Lyo.Api README](../../../Integration/Api/Lyo.Api/README.md#query-result-caching).

Targets `netstandard2.0;net10.0`. Depends on `Lyo.Exceptions` and `Lyo.Common`.

## Features

- **WhereClause AST** — polymorphic `condition` / `group` JSON tree with optional `SubClause` for two-phase filters
- **QueryConcreteReq** — full entity-graph body for `POST …/QueryConcrete` (includes, sort, keys, options)
- **ProjectionQueryReq** — `Select` + computed fields for `POST …/QueryProject`
- **QueryReq (root)** — `From` / `Joins` / `Select` for dynamic-context `POST …/Query`
- **Fluent builders** — `WhereClauseBuilder`, `QueryConcreteReqBuilder`, `ProjectionQueryReqBuilder`, `QueryReqBuilder`
- **ParameterOptions** — static key/label list or root `QueryReq` template for Job/Reporting definition parameter pickers (`ParameterOptionsJson`, `ParameterOptionsBinder`)
- **Shared with Lyo.Query + Lyo.Api** — same DTOs for in-process LINQ and HTTP endpoints

## Examples

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

### QueryReqBuilder (root /Query)

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

### SubClause (two-phase)

```csharp
var node = WhereClauseBuilder.And()
    .Equals("Age", 10)
    .AddSubClause(sub => sub.AddAnd(s => s.Equals("Name", "Alice")))
    .Build();
```

### ProjectionQueryReqBuilder

```csharp
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Enums;

var query = ProjectionQueryReqBuilder.New()
    .AddSelects("Id", "Name", "Email")
    .AddWhere(b => b.Equals("Status", "Active"))
    .AddComputedField("Label", "{Name} — {Email}")
    .SetZipSiblingCollectionSelections(true)
    .SetPagination(0, 20)
    .Build();
// POST {baseRoute}/QueryProject
```

## Benchmarks

- Portfolio suite: `query`

## Where-clause AST

| Type                     | Role                                                                                                                                                                                       |
|--------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `WhereClause` (abstract) | Root of the polymorphic filter tree. `[JsonDerivedType]` discriminators are `condition` / `group`. Carries optional `Description` and `SubClause` (two-phase filter chain).                |
| `ConditionClause`        | Leaf: dotted `Field`, `Comparison` (`ComparisonOperatorEnum`), and `Value` (scalar, list, or CSV string for `In` / `NotIn`). Implements `IEquatable<ConditionClause>` and `Print(indent)`. |
| `GroupClause`            | Branch: `Operator` (`GroupOperatorEnum`) + `List<WhereClause> Children`; structural `Equals` / `GetHashCode` and `Print(indent)`.                                                          |

Polymorphic JSON shape produced by `System.Text.Json`:

```json
{
  "$type": "group",
  "operator": "And",
  "children": [
    { "$type": "condition", "field": "Status", "comparison": "Equals", "value": "Open" },
    { "$type": "condition", "field": "Lines.Quantity", "comparison": "GreaterThan", "value": 0 }
  ]
}
```

JSON property names match the C# property names under the default camelCase policy (`condition`/`group` come from `[JsonDerivedType]`); the model classes do **not** use
`[JsonPropertyName]`.

## Enums

- `ComparisonOperatorEnum` — `Unknown`, `Equals`, `NotEquals`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `NotStartsWith`, `NotEndsWith`, `GreaterThan`,
  `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `In`, `NotIn`, `Regex`, `NotRegex`. Each carries a `[Description]` symbol (`=`, `≠`, etc.) for UI use. `GreaterThan*` /
  `LessThan*` over collection navigations operate on the collection's count.
- `GroupOperatorEnum` — `And`, `Or`.
- `QueryTotalCountMode` — `Exact`, `None`, `HasMore`.
- `QueryIncludeFilterMode` — `Full`, `MatchedOnly`.
- `JoinType` — `Inner`, `Left` (root `/Query` joins; v1).

## Request DTOs (`Common/Request`)

- `QueryRequestBase` — shared fields: `Start`, `Amount` (paging), `Keys`. Polymorphic JSON (`$type`: `concrete` / `project` / `root`) so cache/API can deserialize
  `ProjectedQueryRes.QueryRequest` (`List<object[]>` of composite primary keys), `WhereClause`, `Include` (navigation paths for eager load), `SortBy` (`List<SortBy>`).
- `QueryConcreteReq : QueryRequestBase, IQueryExecutionRequest` — request body for `/QueryConcrete` (full entity graphs). `Options : QueryRequestOptions` (TotalCount +
  IncludeFilter).
- `ProjectionQueryReq : QueryRequestBase, IQueryExecutionRequest` — request body for `/QueryProject`. Adds `Select` (required) and `ComputedField[] ComputedFields`. `Include` is
  ignored — navigations are derived from `Select` and any collection paths referenced in `WhereClause`. `Options : ProjectedQueryRequestOptions` adds
  `ZipSiblingCollectionSelections` (default `true`).
- `QueryReq : QueryRequestBase, IQueryExecutionRequest` — request body for **root** `/Query` (dynamic context base). Required `From` (`FromClause`), optional `Joins`
  (`JoinClause`), required `Select` (alias.property), optional `ComputedFields`. `Include` forbidden. Nested `FromClause.Query` / `JoinClause.Query` is a `SourceQueryScope`
  (Where/Keys), not `WhereClause.SubClause`.
- `FromClause` / `JoinClause` / `JoinOn` / `SourceQueryScope` — join AST for root Query.
- `ComputedField(Name, Template)` — adds a column derived from a SmartFormat template evaluated against the projected row (requires `IFormatterService` in the host).
- `IQueryExecutionRequest` — common execution surface for concrete / projection / root query.

Maps onto `Lyo.Api` host routes (see [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) for caching, options, and SQL projection details):

| Request DTO          | Endpoint                         | Response                                    |
|----------------------|----------------------------------|---------------------------------------------|
| `QueryConcreteReq`   | `POST {baseRoute}/QueryConcrete` | `QueryRes<T>` (entity graphs)               |
| `ProjectionQueryReq` | `POST {baseRoute}/QueryProject`  | `ProjectedQueryRes<T>`                      |
| `QueryReq`           | `POST {dynamicBase}/Query`       | `ProjectedQueryRes` (JSON rows; From/Joins) |

**Result caching** for QueryConcrete / QueryProject is host-side (`QueryOptions.CacheQueryResultsAsUtf8Payload` + `ICacheService` / Fusion), not on these DTOs. Both endpoints share
the same option and tag-based invalidation (`QueryCacheKeyBuilder` / `QueryCacheTagBuilder`).

## Sort

- `SortBy(PropertyName, Direction?, Priority?)` — dotted property path with an optional explicit `Priority`. When omitted the list order in the request determines tie-break order.

## Explain results

`WhereClauseExplainResult`, `WhereClauseExplainNode`, `WhereClauseExplainKind`, and `ExplainOrBranchOutcome` — produced by `IWhereClauseService.ExplainMatch<TEntity>(...)` in
`Lyo.Query`. Each node tracks `Passed`, AST `Path`, optional `Description`, group `Operator`, condition `Field` / `Comparison` / `FilterValue` / `ActualValueSummary`, and
`SubClause` chains. The top-level result also carries `BlockingPath`, `FailureSummary`, and per-branch detail for failed `Or` groups.

## Builders

| Builder                     | Produces             | Notes                                                                                                                                                              |
|-----------------------------|----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `WhereClauseBuilder`        | `WhereClause`        | `And()` / `Or()`; per-operator helpers (`Equals`, `Contains`, `In`, `Regex`, …); nested groups; `AddSubClause` / `AddConditionWithSubClause` for two-phase filters |
| `WhereClauseBuilderFor<T>`  | `WhereClause`        | From `WhereClauseBuilder.For<T>()` — property paths via `Expression<Func<T, …>>`                                                                                   |
| `QueryConcreteReqBuilder`   | `QueryConcreteReq`   | Includes, keys, where, sort, paging, total-count / include-filter modes; `For<T>()` typed helpers                                                                  |
| `ProjectionQueryReqBuilder` | `ProjectionQueryReq` | Same as concrete plus `AddSelect` / `AddComputedField` / zip sibling collections                                                                                   |
| `QueryReqBuilder`           | `QueryReq`           | Root `/Query`: `From`, `Join`, selects, where/sort/paging                                                                                                          |

See **Examples** above for full builder samples (also documented under Query & Request Builders in `Lyo.Api`).

## Attributes and exceptions

- `[QueryPropertyName("CanonicalName")]` — overrides the serialized / query path name when the C# property differs from the canonical query path (useful when EF scaffolding or DTOs
  rename a column).
- `InvalidQueryException : InvalidOperationException` — thrown by `Lyo.Query` for invalid paths or unsupported operators.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)