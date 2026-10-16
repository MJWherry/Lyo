# Lyo.Music.Postgres

PostgreSQL and EF Core implementation of `Lyo.Music.IMusicStore` (`PostgresMusicStore`) via `MusicDbContext`, schema `music`, and `AddPostgresMigrations`. `PostgresMusicStore` implements `Lyo.Health.IHealth`.

## Features

- **Schema `music`.** Release groups, releases, tracks, credits, identifiers, genres, formats, and recordings.
- **Constraints.** Unique slug, catalog-wide `(scheme, normalized_value)`, track position, and filtered unique ISRC.
- **Text.** Stored `tsvector` and a GIN index on track lyrics.
- **DI.** `AddMusicDbContextFactory` / `AddPostgresMusicStore` and `FromConfiguration` overloads (section `PostgresMusic`).

## Examples

### Registration

```csharp
services.AddPostgresMusicStore(o => {
    o.ConnectionString = connStr;
    o.EnableAutoMigrations = true;
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Music` (direct, lyo)
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