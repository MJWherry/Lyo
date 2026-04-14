# Lyo.Query.Models

DTOs and builders for **query** requests (`QueryReq`, `ProjectionQueryReq`) and the **`WhereClause`** filter tree used by the API and Blazor tooling.

**Caching:** Result caching for **`POST …/Query`** and **`POST …/QueryProject`** is configured on the API host (**`QueryOptions.CacheQueryResultsAsUtf8Payload`**, **`CacheOptions`**) — not on these DTOs. Both endpoints share the same option; see the *Query result caching* section in the [Lyo.Api README](../../../Integration/Api/Lyo.Api/README.md#query-result-caching).

## Where-clause model

| Type | Role |
|------|------|
| **`WhereClause`** (abstract) | Root of the polymorphic filter tree. JSON discriminators: **`condition`** / **`group`** (`GroupClause`). |
| **`ConditionClause`** | Leaf: **field** + **comparison** + **value**. |
| **`GroupClause`** | Branch: **operator** (AND/OR) + **children**. |

## Enums

- **`ComparisonOperatorEnum`** — how a value is compared to a field (Equals, Contains, In, Regex, …). The JSON property name for this value on a condition is **`comparator`** (see `[JsonPropertyName]` on `ConditionClause.Comparison`).
- **`GroupOperatorEnum`** — `And` / `Or` for grouping. Serialized as **`operatorEnum`** on `GroupClause` (see `[JsonPropertyName]` on `GroupClause.Operator`).

## Builders

Use **`WhereClauseBuilder`** (and `WhereClauseBuilder.For<T>()` for typed paths) to construct trees in code. Factory helpers include `WhereClauseBuilder.Condition(...)` and `WhereClauseBuilder.ConditionWithSubClause(...)`.
