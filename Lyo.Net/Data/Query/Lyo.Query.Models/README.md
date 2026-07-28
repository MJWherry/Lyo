# Lyo.Query.Models

Filter / sort / projection DTOs and fluent builders for query requests. The same
`WhereClause` tree is consumed by [`Lyo.Query`](../Lyo.Query/README.md) (turns it into
LINQ on `IQueryable`) and by `Lyo.Api` endpoints, so HTTP clients and in-process tests
build queries the same way.

> **Caching:** Result caching for `POST …/QueryConcrete` and `POST …/QueryProject` is configured
> on the API host (`QueryOptions.CacheQueryResultsAsUtf8Payload`, `CacheOptions`), not
> on these DTOs. Both endpoints share the same option; see the *Query result caching*
> section in the [Lyo.Api README](../../../Integration/Api/Lyo.Api/README.md#query-result-caching).

Targets `netstandard2.0;net10.0`. Depends on `Lyo.Exceptions` and `Lyo.Common`.

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

JSON property names match the C# property names under the default camelCase policy
(`condition`/`group` come from `[JsonDerivedType]`); the model classes do **not** use
`[JsonPropertyName]`.

## Enums

- `ComparisonOperatorEnum` — `Unknown`, `Equals`, `NotEquals`, `Contains`,
  `NotContains`, `StartsWith`, `EndsWith`, `NotStartsWith`, `NotEndsWith`,
  `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `In`, `NotIn`,
  `Regex`, `NotRegex`. Each carries a `[Description]` symbol (`=`, `≠`, etc.) for
  UI use. `GreaterThan*` / `LessThan*` over collection navigations operate on the
  collection's count.
- `GroupOperatorEnum` — `And`, `Or`.
- `QueryTotalCountMode` — `Exact`, `None`, `HasMore`.
- `QueryIncludeFilterMode` — `Full`, `MatchedOnly`.
- `JoinType` — `Inner`, `Left` (root `/Query` joins; v1).

## Request DTOs (`Common/Request`)

- `QueryRequestBase` — shared fields: `Start`, `Amount` (paging), `Keys`.
  Polymorphic JSON (`$type`: `concrete` / `project` / `root`) so cache/API can
  deserialize `ProjectedQueryRes.QueryRequest`
  (`List<object[]>` of composite primary keys), `WhereClause`, `Include` (navigation
  paths for eager load), `SortBy` (`List<SortBy>`).
- `QueryConcreteReq : QueryRequestBase, IQueryExecutionRequest` — request body for `/QueryConcrete`
  (full entity graphs). `Options : QueryRequestOptions` (TotalCount + IncludeFilter).
- `ProjectionQueryReq : QueryRequestBase, IQueryExecutionRequest` — request body for
  `/QueryProject`. Adds `Select` (required) and `ComputedField[] ComputedFields`.
  `Include` is ignored — navigations are derived from `Select` and any collection
  paths referenced in `WhereClause`. `Options : ProjectedQueryRequestOptions` adds
  `ZipSiblingCollectionSelections` (default `true`).
- `QueryReq : QueryRequestBase, IQueryExecutionRequest` — request body for **root** `/Query`
  (dynamic context base). Required `From` (`FromClause`), optional `Joins` (`JoinClause`),
  required `Select` (alias.property), optional `ComputedFields`. `Include` forbidden.
  Nested `FromClause.Query` / `JoinClause.Query` is a `SourceQueryScope` (Where/Keys), not
  `WhereClause.SubClause`.
- `FromClause` / `JoinClause` / `JoinOn` / `SourceQueryScope` — join AST for root Query.
- `ComputedField(Name, Template)` — adds a column derived from a SmartFormat template
  evaluated against the projected row (requires `IFormatterService` in the host).
- `IQueryExecutionRequest` — common execution surface for concrete / projection / root query.

## Sort

- `SortBy(PropertyName, Direction?, Priority?)` — dotted property path with an
  optional explicit `Priority`. When omitted the list order in the request
  determines tie-break order.

## Explain results

`WhereClauseExplainResult`, `WhereClauseExplainNode`, `WhereClauseExplainKind`, and
`ExplainOrBranchOutcome` — produced by
`IWhereClauseService.ExplainMatch<TEntity>(...)` in `Lyo.Query`. Each node tracks
`Passed`, AST `Path`, optional `Description`, group `Operator`, condition `Field` /
`Comparison` / `FilterValue` / `ActualValueSummary`, and `SubClause` chains. The
top-level result also carries `BlockingPath`, `FailureSummary`, and per-branch
detail for failed `Or` groups.

## Builders

| Builder                     | Produces             | Notes                                                                                                                                                                                                                                                                                              |
|-----------------------------|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `WhereClauseBuilder`        | `WhereClause`        | `WhereClauseBuilder.And()` / `Or()` start a group; per-operator helpers (`Equals`, `Contains`, `In`, `Regex`, `GreaterThan`, `LessThan`, …); `AddCondition`, `AddConditionWithSubClause`; nested `And()` / `Or()` for groups; static `Condition(...)` and `ConditionWithSubClause(...)` factories. |
| `WhereClauseBuilderFor<T>`  | `WhereClause`        | Returned by `WhereClauseBuilder.For<T>()`; resolves property paths from `Expression<Func<T, …>>` lambdas.                                                                                                                                                                                          |
| `QueryConcreteReqBuilder`   | `QueryConcreteReq`   | `AddIncludes`, `AddKey` / `AddKeys`, `AddWhere(WhereClause)` / `AddWhere(Action<WhereClauseBuilder>)`, `AddSort`, `SetPagination(start, amount)`, `First()`, `SetTotalCountMode`, `SetIncludeFilterMode`, `For<T>()`.                                                                              |
| `ProjectionQueryReqBuilder` | `ProjectionQueryReq` | Same shape as `QueryConcreteReqBuilder` plus `AddSelect`, `AddComputedField`, and projection options.                                                                                                                                                                                              |
| `QueryReqBuilder`           | `QueryReq`           | Root `/Query`: `From`, `Join`, `AddSelects`, `AddKey` / `AddKeys`, where/sort/paging, zip option.                                                                                                                                                                                                  |

## Attributes and exceptions

- `[QueryPropertyName("CanonicalName")]` — overrides the serialized / query path name
  when the C# property differs from the canonical query path (useful when EF scaffolding
  or DTOs rename a column).
- `InvalidQueryException : InvalidOperationException` — thrown by `Lyo.Query` for
  invalid paths or unsupported operators.

## Related projects

- [`Lyo.Query`](../Lyo.Query/README.md) — AST → LINQ on `IQueryable`.
- [`Lyo.Query.Web.Components`](../Lyo.Query.Web.Components/README.md) — Blazor query
  workbench.
- [`Lyo.Api`](../../../Integration/Api/Lyo.Api/README.md) — production query
  endpoints and projection.
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md),
  [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md).
