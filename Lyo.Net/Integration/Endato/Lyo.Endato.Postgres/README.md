# Lyo.Endato.Postgres

PostgreSQL schema and EF Core context for caching Endato Person Search (PS) and Contact Enrichment (CE) responses. Schema name is `endato`.

## Examples

### Quick start

```csharp
services.AddEndatoDbContextFactoryFromConfiguration(builder.Configuration);

// or inline:
services.AddEndatoDbContextFactory(o => {
    o.ConnectionString = "Host=localhost;Database=lyo;Username=postgres;Password=postgres";
    o.EnableAutoMigrations = true;
});
```

## What ships

- **`EndatoDbContext`** ([`Database/EndatoDbContext.cs`](Database/EndatoDbContext.cs)) and `EndatoDbContextFactory` for design-time tooling.
- **`PostgresEndatoOptions`** ([`PostgresEndatoOptions.cs`](PostgresEndatoOptions.cs)) — `IPostgresMigrationConfig`. Section `"PostgresEndato"`. Properties: `ConnectionString`,
  `EnableAutoMigrations` (default `false`); `Schema = "endato"` is constant.
- **Migrations** under [`Migrations/`](Migrations).

The two domains (Person Search and Contact Enrichment) are intentionally split into separate entity groups so caching either source is independent:

| Group | Entities |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Person Search (`Ps`)** | `EndatoPsQueryEntity`, `EndatoPsPersonEntity`, `EndatoPsAddressEntity`, `EndatoPsEmailAddressEntity`, `EndatoPsPhoneNumberEntity` (+ matching `*Configuration` classes). |
| **Contact Enrichment (`Ce`)** | `EndatoCeQueryEntity`, `EndatoCePersonEntity`, `EndatoCeAddressEntity`, `EndatoCeEmailAddressEntity`, `EndatoCePhoneNumberEntity` (+ matching `*Configuration` classes). |

`OnModelCreating` sets `HasDefaultSchema("endato")` and applies each entity configuration; the migrations history table is also written to the `endato` schema.

## DI surface ([`Extensions.cs`](Extensions.cs))

All registrations are extension methods on `IServiceCollection` (declared inside `extension(IServiceCollection services)` blocks):

| Method | Description |
| ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddEndatoDbContext(string connectionString)` | Convenience: registers the factory plus a scoped `EndatoDbContext` resolved from the factory. |
| `AddEndatoDbContext(Action<DbContextOptionsBuilder>)` | Standard EF `AddDbContext` overload. |
| `AddEndatoDbContextFactory(Action<PostgresEndatoOptions>)` | Builds options inline. |
| `AddEndatoDbContextFactoryFromConfiguration(config, sectionName?)` | Binds options from configuration (default section `"PostgresEndato"`). |
| `AddEndatoDbContextFactory(PostgresEndatoOptions)` | Registers `IDbContextFactory<EndatoDbContext>` (Npgsql provider), wires `AddPostgresMigrations<EndatoDbContext, …>`, and sets the migrations history table to the `endato` schema. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)