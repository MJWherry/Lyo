# Lyo.Query.Models

Filter / sort / projection DTOs and fluent builders for query requests. [`Lyo.Query`](../Lyo.Query/README.md) consumes the same `WhereClause` tree (turns it into LINQ on `IQueryable`) and [`Lyo.Api`](../../../Apps/Api/Lyo.Api/README.md) endpoints (`QueryConcrete`, `QueryProject`, root `Query`) do too, so HTTP clients and in-process tests build queries the same way.

Covers the polymorphic where-clause AST, `QueryConcreteReq` / `ProjectionQueryReq` / `QueryReq`, sort + explain result shapes, and builders (`WhereClauseBuilder`, `QueryConcreteReqBuilder`, `ProjectionQueryReqBuilder`, `QueryReqBuilder`).

> **Caching.** Result caching for `POST …/QueryConcrete` and `POST …/QueryProject` is configured > on the API host (`QueryOptions.CacheQueryResultsAsUtf8Payload`, `ICacheService` / Fusion), > not on these DTOs. See *Query result caching* in the [Lyo.Api README](../../../Apps/Api/Lyo.Api/README.md#query-result-caching).

Targets `netstandard2.0;net10.0`. Depends on `Lyo.Exceptions`, `Lyo.Common.Core`, and `Lyo.Result`.

## Features

- **WhereClause AST.** Polymorphic `condition` / `group` JSON tree, with optional `SubClause` for two-phase filters.
- **QueryConcreteReq.** Entity-graph body for `POST …/QueryConcrete` (includes, sort, keys, options).
- **ProjectionQueryReq.** `Select` plus computed fields for `POST …/QueryProject`.
- **QueryReq (root).** `From` / `Joins` / `Select` for dynamic-context `POST …/Query`.
- **Builders.** `WhereClauseBuilder`, `QueryConcreteReqBuilder`, `ProjectionQueryReqBuilder`, `QueryReqBuilder`.
- **ParameterOptions.** Static key/label list, a root `QueryReq` template, or a `schema.func` sproc (`StoredProcName`, `SprocParameters` with `{{Key}}` placeholders) for Job/Reporting definition parameter pickers (`ParameterOptionsJson`, `ParameterOptionsBinder`, `ParameterOptionsResolveReq`).
- **Shared with Lyo.Query + Lyo.Api.** The same DTOs for in-process LINQ and HTTP endpoints.
- **Explain → errors.** `WhereClauseExplainResult.ToErrors` maps a failed in-memory explain tree to `Lyo.Result.Error` (AND = per-leaf, OR = one summary). Used by validation schemas, not for SQL `ApplyWhereClause`.
- **Parameter validation** (`Parameters/`). `LyoParameterSpec` / `LyoParameterValueSpec` are the backend-neutral projections of a parameter definition and a supplied value (`From(ILyoParameterDefinition)` / `From(ILyoParameterValue)`), and `LyoParameterValidator` is the single validator over them. Job, Reporting, and Config all delegate here instead of each carrying their own copy of the required / regex / length / allowed-values rules.

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

### Build a root /Query with QueryReqBuilder

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

### Two-phase SubClause

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

## Where-clause tree

| Type | Role |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `WhereClause` (abstract) | Root of the polymorphic filter tree. `[JsonDerivedType]` discriminators are `condition` / `group`. Carries optional `Description` and `SubClause` (two-phase filter chain). |
| `ConditionClause` | Leaf: dotted `Field`, `Comparison` (`ComparisonOperatorEnum`), and `Value` (scalar, list, or CSV string for `In` / `NotIn`). Implements `IEquatable<ConditionClause>` and `Print(indent)`. |
| `GroupClause` | Branch: `Operator` (`GroupOperatorEnum`) + `List<WhereClause> Children`; structural `Equals` / `GetHashCode` and `Print(indent)`. |

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

- `ComparisonOperatorEnum`. `Unknown`, `Equals`, `NotEquals`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `NotStartsWith`, `NotEndsWith`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `In`, `NotIn`, `Regex`, `NotRegex`. Each carries a `[Description]` symbol (`=`, `≠`, etc.) for UI use. `GreaterThan*` / `LessThan*` over collection navigations operate on the collection's count.
- `GroupOperatorEnum`. `And`, `Or`.
- `QueryTotalCountMode`. `Exact`, `None`, `HasMore`.
- `QueryIncludeFilterMode`. `Full`, `MatchedOnly`.
- `JoinType`. `Inner`, `Left` (root `/Query` joins; v1).

## Request DTOs under `Common/Request`

- `QueryRequestBase`. Shared fields: `Start`, `Amount` (paging), `Keys`. Polymorphic JSON (`$type`: `concrete` / `project` / `root`) so cache/API can deserialize `ProjectedQueryRes.QueryRequest` (`List<object[]>` of composite primary keys), `WhereClause`, `Include` (navigation paths for eager load), `SortBy` (`List<SortBy>`).
- `QueryConcreteReq : QueryRequestBase, IQueryExecutionRequest`. Request body for `/QueryConcrete` (entity graphs). `Options : QueryRequestOptions` (TotalCount + IncludeFilter).
- `ProjectionQueryReq : QueryRequestBase, IQueryExecutionRequest`. Request body for `/QueryProject`. Adds `Select` (required) and `ComputedField[] ComputedFields`. `Include` is ignored. Navigations are derived from `Select` and any collection paths referenced in `WhereClause`. `Options : ProjectedQueryRequestOptions` adds `ZipSiblingCollectionSelections` (default `true`).
- `QueryReq : QueryRequestBase, IQueryExecutionRequest`. Request body for root `/Query` (dynamic context base). Required `From` (`FromClause`), optional `Joins` (`JoinClause`), required `Select` (alias.property), optional `ComputedFields`. `Include` forbidden. Nested `FromClause.Query` / `JoinClause.Query` is a `SourceQueryScope` (Where/Keys), not `WhereClause.SubClause`.
- `FromClause` / `JoinClause` / `JoinOn` / `SourceQueryScope`. Join AST for root Query.
- `ComputedField(Name, Template)`. Adds a column derived from a SmartFormat template evaluated against the projected row (requires `IFormatterService` in the host).
- `IQueryExecutionRequest`. Shared methods for concrete / projection / root query.

Maps onto `Lyo.Api` host routes (see [`Lyo.Api`](../../../Apps/Api/Lyo.Api/README.md) for caching, options, and SQL projection details):

| Request DTO | Endpoint | Response |
| --- | --- | --- |
| `QueryConcreteReq` | `POST {baseRoute}/QueryConcrete` | `QueryRes<T>` (entity graphs) |
| `ProjectionQueryReq` | `POST {baseRoute}/QueryProject` | `ProjectedQueryRes<T>` |
| `QueryReq` | `POST {dynamicBase}/Query` | `ProjectedQueryRes` (JSON rows; From/Joins) |

**Result caching** for QueryConcrete / QueryProject is host-side (`QueryOptions.CacheQueryResultsAsUtf8Payload` + `ICacheService` / Fusion), not on these DTOs. Both endpoints share the same option and tag-based invalidation (`QueryCacheKeyBuilder` / `QueryCacheTagBuilder`).

## Sort

- `SortBy(PropertyName, Direction?, Priority?)`. Dotted property path with an optional explicit `Priority`. When omitted, list order in the request determines tie-break order.

## Explain results

`WhereClauseExplainResult`, `WhereClauseExplainNode`, `WhereClauseExplainKind`, and `ExplainOrBranchOutcome` come from `IWhereClauseService.ExplainMatch<TEntity>(...)` in `Lyo.Query`. Each node tracks `Passed`, AST `Path`, optional `Description`, group `Operator`, condition `Field` / `Comparison` / `FilterValue` / `ActualValueSummary`, and `SubClause` chains. The top-level result also carries `BlockingPath`, `FailureSummary`, and per-branch detail for failed `Or` groups.

## Builders

| Builder | Produces | Notes |
| --- | --- | --- |
| `WhereClauseBuilder` | `WhereClause` | `And()` / `Or()`; per-operator helpers (`Equals`, `Contains`, `In`, `Regex`, …); nested groups; `AddSubClause` / `AddConditionWithSubClause` for two-phase filters |
| `WhereClauseBuilderFor<T>` | `WhereClause` | From `WhereClauseBuilder.For<T>()`. Property paths via `Expression<Func<T, …>>` |
| `QueryConcreteReqBuilder` | `QueryConcreteReq` | Includes, keys, where, sort, paging, total-count / include-filter modes; `For<T>()` typed helpers |
| `ProjectionQueryReqBuilder` | `ProjectionQueryReq` | Same as concrete plus `AddSelect` / `AddComputedField` / zip sibling collections |
| `QueryReqBuilder` | `QueryReq` | Root `/Query`: `From`, `Join`, selects, where/sort/paging |

See Examples above for builder samples (also documented under Query & Request Builders in `Lyo.Api`).

## Attributes plus exceptions

- `[QueryPropertyName("CanonicalName")]`. Overrides the serialized / query path name when the C# property differs from the canonical query path (useful when EF scaffolding or DTOs rename a column).
- `InvalidQueryException : InvalidOperationException`. Thrown by `Lyo.Query` for invalid paths or unsupported operators.

## Parameter validation (`Parameters/`)

`LyoParameterValidator` holds the rules that Job, Reporting, and Config parameters share:

| Member | Role |
| --- | --- |
| `Validate(specs, values)` | Returns the human-readable error list for a set of supplied values against their definitions: missing required parameters, unknown keys, regex mismatches, length bounds, and allowed-value membership. Empty list means valid. |
| `ValidateSpec(spec, errors)` | Validates a definition itself — that its own regex compiles, its bounds are coherent, and its allowed-values list is usable. Used on write paths so a bad definition is rejected at save time rather than at run time. |
| `ValidateUniqueKeys(specs, errors)` | Rejects duplicate parameter keys within one definition set. |
| `MaxValidationRegexLength` (500) / `RegexMatchTimeout` (1s) | Guardrails against catastrophic backtracking from user-supplied `ValidationRegex` values. |

Values carrying an encrypted payload satisfy a required check without the plaintext being present (`LyoParameterValueSpec.HasEncryptedValue`).

Type checking accepts both storage conventions in the tree: Reporting persists JSON-encoded values, Job persists what the user typed. A scalar that is not valid JSON gets a second chance as a JSON string literal, so `2026-01-01` passes as a `DateTime`. Structured types (JSON objects and arrays, collections, XML, formatter templates) get no such leniency — quoting a malformed payload would turn it into a valid string and hide the error.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Parameters` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)