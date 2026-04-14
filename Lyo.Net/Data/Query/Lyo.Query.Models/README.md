# Lyo.Query.Models

DTOs and builders for **query** requests (`QueryReq`, `ProjectionQueryReq`) and the **`WhereClause`** filter tree used by the API and Blazor tooling.

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
