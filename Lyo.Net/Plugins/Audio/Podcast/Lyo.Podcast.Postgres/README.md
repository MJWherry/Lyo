# Lyo.Podcast.Postgres

PostgreSQL and EF Core implementation of `Lyo.Podcast.IPodcastStore` (`PostgresPodcastStore`) via `PodcastDbContext`, schema `podcast`, and `AddPostgresMigrations`.
`PostgresPodcastStore` implements `Lyo.Health.IHealth`.

## Features

- **Schema `podcast`.** Shows, seasons, episodes, credits, identifiers, and genres.
- **Constraints.** Unique slug, identifier `(scheme, normalized value)` per owner table, and season and episode numbers.
- **Text.** Stored `tsvector` column and GIN index on episode transcripts.
- **DI.** `AddPodcastDbContextFactory` / `AddPostgresPodcastStore` and `FromConfiguration` overloads (section `PostgresPodcast`).

## Examples

### Registration

```csharp
services.AddPostgresPodcastStore(o => {
    o.ConnectionString = connStr;
    o.EnableAutoMigrations = true;
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Podcast` (direct, lyo)
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