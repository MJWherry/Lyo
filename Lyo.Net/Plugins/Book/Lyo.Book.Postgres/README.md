# Lyo.Book.Postgres

PostgreSQL and EF Core implementation of `Lyo.Book.IBookStore` (`PostgresBookStore`) via `BookDbContext`, `PostgresBookOptions`, and
`AddPostgresMigrations<BookDbContext, PostgresBookOptions>`. Page bodies are raw `text` with a stored generated `tsvector` (English; HTML tags stripped). `PostgresBookStore`
implements `Lyo.Health.IHealth`.

## Features

- **Schema `book`.** Series, books, titles, identifiers, contributors, types, formats, hierarchical genres, chapters, and pages.
- **Search.** GIN on generated `search_vector`. `SearchPagesAsync` uses `ts_headline` snippets.
- **Constraints.** Catalog-wide unique `(scheme, normalized_value)`; filtered unique primary title/identifier; contributor `(book_id, role, ordinal)`.
- **DI.** `AddBookDbContextFactory` / `AddPostgresBookStore` and `FromConfiguration` overloads (section `PostgresBook`).
- **Lookups.** `BookDbSeeder` upserts types, formats, and a hierarchical genre tree (idempotent by slug).

## Examples

### Registration

```csharp
services.AddPostgresBookStore(o => {
    o.ConnectionString = connStr;
    o.EnableAutoMigrations = true;
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Book` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)