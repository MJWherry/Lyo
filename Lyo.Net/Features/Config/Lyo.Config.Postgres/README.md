# Lyo.Config.Postgres

PostgreSQL + EF Core implementation of [`Lyo.Config.IConfigStore`](../Lyo.Config/README.md) for storing typed configuration definitions and per-entity bindings.

## What ships

- `ConfigDbContext` (+ `ConfigDbContextFactory`) under `Database/` with `DbSet<ConfigDefinitionEntity>`, `DbSet<ConfigBindingEntity>`, and `DbSet<ConfigBindingRevisionEntity>`.
- Fluent-API entity configurations enforcing the uniqueness rules from `Lyo.Config`:
    - `config_definition` unique on `(ForEntityType, Key)`.
    - `config_binding` unique on `(DefinitionId, ForEntityType, ForEntityId)` with `ON DELETE CASCADE` from the definition row.
    - `config_binding_revision` keyed on `(BindingId, Revision)` — monotonic 1-based revision numbers per binding.
- `PostgresConfigStore`, the singleton `IConfigStore` implementation backed by an `IDbContextFactory<ConfigDbContext>`. It also implements `Lyo.Health.IHealth` so the container
  can be probed for relational connectivity.
- `PostgresConfigOptions` (`SectionName = "PostgresConfig"`, `Schema = "config"`, `ConnectionString`, `EnableAutoMigrations`).
- Migrations under `Migrations/` (`InitialCreate` baseline) using the `config` schema for the EF migrations history table.

## DI registration (`Extensions`)

All entry points are exposed as `IServiceCollection` extensions:

| Entry point                                                                                                          | What it does                                                                                                                                                                                                                                                                    |
|----------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddConfigDbContextFactory(Action<PostgresConfigOptions>)`                                                           | Registers `IOptions<PostgresConfigOptions>`, `AddPostgresMigrations<ConfigDbContext, PostgresConfigOptions>()`, and `IDbContextFactory<ConfigDbContext>` (`UseNpgsql` + migrations history under the configured schema). DbContext only — does **not** register `IConfigStore`. |
| `AddConfigDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresConfigOptions.SectionName)` | Same as above; binds from the configuration section (default `"PostgresConfig"`).                                                                                                                                                                                               |
| `AddConfigDbContextFactory(PostgresConfigOptions options)`                                                           | Same as above with a pre-built options instance.                                                                                                                                                                                                                                |
| `AddPostgresConfigStore(Action<PostgresConfigOptions>)`                                                              | Calls `AddConfigDbContextFactory(...)` then registers `IConfigStore` → singleton `PostgresConfigStore`.                                                                                                                                                                         |
| `AddPostgresConfigStoreFromConfiguration(IConfiguration, string sectionName = PostgresConfigOptions.SectionName)`    | Same as above; binds from configuration.                                                                                                                                                                                                                                        |
| `AddPostgresConfigStore(PostgresConfigOptions options)`                                                              | Same as above with a pre-built options instance — for tests / integration harnesses.                                                                                                                                                                                            |

`PostgresConfigOptions.ConnectionString` is validated (non-empty) by all entry points. The `EnableAutoMigrations` flag is consumed by `AddPostgresMigrations<>` — see
[`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) — to gate the hosted-startup migration runner.

## Runtime expectations

`PostgresConfigStore` opens a fresh `ConfigDbContext` per call via the registered `IDbContextFactory<ConfigDbContext>`, so it is safe as a singleton under concurrent load.
`SaveBindingAsync` writes the current value to `config_binding` **and** appends a new `config_binding_revision` row inside the same `SaveChangesAsync`, so revision history
stays in lock-step with the latest binding value. The seeded initial migration writes revision `1` from each existing binding so history starts at deploy time.

## Tenancy

Bindings (and their revisions) carry a nullable `tenant_id` column; definitions do not — schema is deployment-global. The `config_binding` unique constraint includes
`tenant_id`, so the same definition can be bound per tenant without collision. Filtered tenant indexes (`ix_config_binding_tenant`, `ix_config_binding_revision_tenant`)
back lookups.

`IConfigStore` binding methods (not definition methods) accept an explicit `Guid? tenantId` and run it through `TenancyResolver.Resolve` using the policy configured in
`PostgresConfigOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset):

- `SystemOnly` — bindings are persisted with `tenant_id = NULL`.
- `SingleTenantDefault` *(default)* — caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict` — caller must supply a non-empty `tenantId` or the store throws.

See [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) for the full matrix.

```json
{
  "PostgresConfig": {
    "ConnectionString": "Host=localhost;Database=config;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

*(Synchronized from `Lyo.Config.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                      | Version |
|----------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`       | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`  | `[10,)` |

### Project references

- [`Lyo.Config`](../Lyo.Config/README.md)
- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
