# Lyo.Audiobook.Postgres

PostgreSQL and EF Core implementation of `Lyo.Audiobook.IAudiobookStore` (`PostgresAudiobookStore`) via `AudiobookDbContext`, schema `audiobook`, and `AddPostgresMigrations`. `PostgresAudiobookStore` implements `Lyo.Health.IHealth`.

## Features

- **Schema `audiobook`.** Works, chapters, credits, identifiers, genres, and series.
- **Constraints.** Unique slug, identifier `(scheme, normalized value)`, and chapter number per work.
- **Text.** Stored `tsvector` columns and GIN indexes on work and chapter transcripts.
- **DI.** `AddAudiobookDbContextFactory` / `AddPostgresAudiobookStore` and `FromConfiguration` overloads (section `PostgresAudiobook`).

## Examples

### Registration

```csharp
services.AddPostgresAudiobookStore(o => {
    o.ConnectionString = connStr;
    o.EnableAutoMigrations = true;
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Audiobook` (direct, lyo)
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