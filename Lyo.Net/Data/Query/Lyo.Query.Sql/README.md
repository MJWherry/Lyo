# Lyo.Query.Sql

Walks a `QueryConcreteReq`, `ProjectionQueryReq`, or `QueryReq` and returns command text plus positional parameters. Column names come from a caller-supplied `SqlColumnMap`, not from an EF model. The translator does not open a connection.

String compares use `lower()`. `Contains` / `StartsWith` / `EndsWith` become `LIKE` with bound, escaped patterns. Fuzzy prints `similarity()` and does not require `pg_trgm` to be installed at translate time. Regex, collection paths, sub-clauses, computed fields, includes, and keys throw `SqlTranslationException`.

Targets `net10.0`. Depends on `Lyo.Query.Models`.

## Features

- **Concrete.** `SELECT` of every mapped column, plus `WHERE`, `ORDER BY`, `OFFSET`, and `LIMIT`.
- **Projection.** Listed scalar columns. Aggregates become `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`, or `COUNT(DISTINCT)`, with `GROUP BY` when `Select` is the group key.
- **Root.** `FROM` alias, `INNER` / `LEFT` / `RIGHT` / `FULL OUTER` joins, and `alias.property` paths.
- **Parameters.** Values stay in `$1`, `$2`, … and are never interpolated.

## Examples

### Translate a concrete filter

```csharp
var map = new SqlColumnMap("people", "person", new Dictionary<string, string> { ["Status"] = "status" });
var request = new QueryConcreteReq { WhereClause = new ConditionClause("Status", ComparisonOperatorEnum.Equals, "Active") };
var sql = new SqlQueryTranslator().Translate(request, map);
// SELECT "status" FROM "people"."person" WHERE lower("status") = lower($1)
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)