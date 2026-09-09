# Lyo.Endato.Postgres

EF Core context and PostgreSQL schema that cache Endato Person Search (PS) and Contact Enrichment (CE) responses. Schema name is `endato`.

## Examples

### Start here

```csharp
services.AddEndatoDbContextFactoryFromConfiguration(builder.Configuration);

// or inline:
services.AddEndatoDbContextFactory(o => {
    o.ConnectionString = "Host=localhost;Database=lyo;Username=postgres;Password=postgres";
    o.EnableAutoMigrations = true;
});
```

## What's included

- `EndatoDbContext` ([`Database/EndatoDbContext.cs`](Database/EndatoDbContext.cs)) and `EndatoDbContextFactory` for design-time tooling.
- `PostgresEndatoOptions` ([`PostgresEndatoOptions.cs`](PostgresEndatoOptions.cs)). `IPostgresMigrationConfig`. Section `"PostgresEndato"`. Properties: `ConnectionString`,
 `EnableAutoMigrations` (default `false`); `Schema = "endato"` is constant.
- **Migrations** under [`Migrations/`](Migrations).

Person Search and Contact Enrichment stay in separate entity groups on purpose so either source can be cached on its own:

| Group | Entities |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Person Search (`Ps`)** | `EndatoPsQueryEntity`, `EndatoPsPersonEntity`, `EndatoPsAddressEntity`, `EndatoPsEmailAddressEntity`, `EndatoPsPhoneNumberEntity` (+ matching `*Configuration` classes). |
| **Contact Enrichment (`Ce`)** | `EndatoCeQueryEntity`, `EndatoCePersonEntity`, `EndatoCeAddressEntity`, `EndatoCeEmailAddressEntity`, `EndatoCePhoneNumberEntity` (+ matching `*Configuration` classes). |

`OnModelCreating` calls `HasDefaultSchema("endato")` and applies each entity configuration; the migrations history table is stored in the `endato` schema as well.

## Wire into DI ([`Extensions.cs`](Extensions.cs))

Every registration is an `IServiceCollection` extension (declared inside `extension(IServiceCollection services)` blocks):

| Method | Description |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `AddEndatoDbContext(string connectionString)` | Shortcut: registers the factory plus a scoped `EndatoDbContext` resolved from that factory. |
| `AddEndatoDbContextFactory(Action<PostgresEndatoOptions>)` | Constructs options in place. |
| `AddEndatoDbContextFactoryFromConfiguration(config, sectionName?)` | Reads options from configuration (default section `"PostgresEndato"`). |
| `AddEndatoDbContextFactory(PostgresEndatoOptions)` | Registers `IDbContextFactory<EndatoDbContext>` (Npgsql provider), wires `AddPostgresMigrations<EndatoDbContext, …>`, and points the migrations history table at the `endato` schema. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Health` (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)