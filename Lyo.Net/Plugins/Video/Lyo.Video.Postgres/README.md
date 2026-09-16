# Lyo.Video.Postgres

PostgreSQL and EF Core implementation of `Lyo.Video.IVideoStore` (`PostgresVideoStore`) via `VideoDbContext`, schema `video`, and `AddPostgresMigrations`. `PostgresVideoStore` implements `Lyo.Health.IHealth`. YouTube link columns live in this schema. The store does not reference `Lyo.Google.Youtube.Client`.

## Features

- **Schema `video`.** Titles, seasons, episodes, credits, identifiers, YouTube links, genres, alternate titles, collections, and cuts.
- **Constraints.** Unique slug, identifier `(scheme, normalized value)` per owner table, season and episode numbers, and one primary cut per movie. `VideoId` is unique across titles and episodes.
- **Text.** Stored `tsvector` columns and GIN indexes on movie and episode transcripts.
- **DI.** `AddVideoDbContextFactory` / `AddPostgresVideoStore` and `FromConfiguration` overloads (section `PostgresVideo`).

## Examples

### Registration

```csharp
services.AddPostgresVideoStore(o => {
    o.ConnectionString = connStr;
    o.EnableAutoMigrations = true;
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Video` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Google.Youtube.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)