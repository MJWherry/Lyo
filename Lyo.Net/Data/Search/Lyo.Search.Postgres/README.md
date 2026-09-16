# Lyo.Search.Postgres

Maps a stored English tsvector and a GIN index, and filters a query with plainto_tsquery. Callers pass the SQL expression for the text they search.

## Features

- Map adds search_vector and a GIN index.
- WhereMatches keeps rows that match a plain query.

## Examples

### Map a body column

```csharp
PostgresTextSearch.Map(builder, e => e.SearchVector, PostgresTextSearch.EnglishColumn("content"), "ix_note_search_vector");
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)