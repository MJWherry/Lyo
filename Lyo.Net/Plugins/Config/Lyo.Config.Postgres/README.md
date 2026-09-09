# Lyo.Config.Postgres

PostgreSQL and EF Core implementation of [`Lyo.Config.IConfigStore`](../Lyo.Config/README.md) for typed configuration definitions and per-entity bindings.

## Package contents

- `ConfigDbContext` (+ `ConfigDbContextFactory`) under `Database/` with `DbSet<ConfigDefinitionEntity>`, `DbSet<ConfigBindingEntity>`, `DbSet<ConfigBindingRevisionEntity>`, and `DbSet<ConfigDefinitionRevisionEntity>`.
- Fluent-API entity configurations enforcing the uniqueness rules from `Lyo.Config`:
- `config_definition` unique on `(ForEntityType, Key)`.
- `config_binding` unique on `(DefinitionId, ForEntityType, ForEntityId)` with `ON DELETE CASCADE` from the definition row.
- `config_binding_revision` keyed on `(BindingId, Revision)` monotonic 1-based revision numbers per binding.
- `config_definition_revision` keyed on `(DefinitionId, Revision)` monotonic 1-based metadata snapshots per definition. Every `SaveDefinitionAsync` appends a row.
- `IsEncrypted` on the definition plus `encrypted_default_value` / `encrypted_value` `bytea` columns. Encryption runs only when `IConfigValueEncryptionService` is registered (Config.Api / TestApi). Toggling Encrypted rewrites stored defaults and all binding revisions. TestGateway does not register encryption. It writes through `ConfigApiStore` HTTP to TestApi.
- `PostgresConfigStore`, the singleton `IConfigStore` implementation backed by an `IDbContextFactory<ConfigDbContext>`. It also implements `Lyo.Health.IHealth` so the container can be probed for relational connectivity.
- `PostgresConfigOptions` (`SectionName = "PostgresConfig"`, `Schema = "config"`, `ConnectionString`, `EnableAutoMigrations`).
- Migrations under `Migrations/` (`InitialCreate` baseline) using the `config` schema for the EF migrations history table.

## Service registration (`Extensions`)

All entry points are exposed as `IServiceCollection` extensions:

| Entry point | Effect |
| -------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `AddConfigDbContextFactory(Action<PostgresConfigOptions>)` | Registers `IOptions<PostgresConfigOptions>`, `AddPostgresMigrations<ConfigDbContext, PostgresConfigOptions>()`, and `IDbContextFactory<ConfigDbContext>` (`UseNpgsql` + migrations history under the configured schema). DbContext only. Does **not** register `IConfigStore`. |
| `AddConfigDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresConfigOptions.SectionName)` | Same as above. Binds from the configuration section (default `"PostgresConfig"`). |
| `AddConfigDbContextFactory(PostgresConfigOptions options)` | Same as above with a pre-built options instance. |
| `AddPostgresConfigStore(Action<PostgresConfigOptions>)` | Calls `AddConfigDbContextFactory(...)` then registers `IConfigStore` → singleton `PostgresConfigStore`. |
| `AddPostgresConfigStoreFromConfiguration(IConfiguration, string sectionName = PostgresConfigOptions.SectionName)` | Same as above. Binds from configuration. |
| `AddPostgresConfigStore(PostgresConfigOptions options)` | Same as above with a pre-built options instance, for tests / integration harnesses. Resolves optional `IConfigValueEncryptionService` from DI (null on workbench hosts). |
| `AddConfigValueEncryption(keyName = ConfigQueryRoutes.EncryptionKeyName)` | Registers `IConfigValueEncryptionService` using a keyed `IEncryptionService`. Call on API hosts only. |
| `AddConfigQueryServices()` | Registers query/CRUD-read/export services for `ConfigDbContext`. Call on API hosts before `MapConfigQueryEndpoints`. |
| `MapConfigQueryEndpoints()` | Maps query/get/export for `Config/Definition` and `Config/Binding`. Writes stay on `IConfigStore`. `DeniedSelectFields` hide `DefaultValueJson` and `EncryptedDefaultValue`. |

`PostgresConfigOptions.ConnectionString` is validated (non-empty) by all entry points. The `EnableAutoMigrations` flag is consumed by `AddPostgresMigrations<>`. See [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) to gate the hosted-startup migration runner.

## Runtime expectations

`PostgresConfigStore` opens a fresh `ConfigDbContext` per call via the registered `IDbContextFactory<ConfigDbContext>`, so a singleton is safe under concurrent load. `SaveDefinitionAsync` upserts `config_definition` **and** appends `config_definition_revision`. When `IsEncrypted` flips, existing defaults and all `config_binding_revision` rows are rewritten to ciphertext or back to JSON. `SaveBindingAsync` appends `config_binding_revision` inside the same `SaveChangesAsync`. When `IsEncrypted` is set, plaintext JSON columns are cleared and ciphertext is stored in `bytea`. Public `LoadConfigAsync` decrypts so apps get usable config.

## Tenant isolation

Bindings (and their revisions) carry a nullable `tenant_id` column. Definitions do not. Schema is deployment-global. The `config_binding` unique constraint includes `tenant_id`, so the same definition can be bound per tenant without collision. Filtered tenant indexes (`ix_config_binding_tenant`, `ix_config_binding_revision_tenant`) back lookups.

`IConfigStore` binding methods (not definition methods) accept an explicit `Guid? tenantId` and run it through `TenancyResolver.Resolve` using the policy configured in `PostgresConfigOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset):

- `SystemOnly`. Bindings are persisted with `tenant_id = NULL`.
- `SingleTenantDefault` *(default)*, caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict`. Caller must supply a non-empty `tenantId` or the store throws.

See [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) for the policy matrix.

```json
{
  "PostgresConfig": {
    "ConnectionString": "Host=localhost;Database=config;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Config` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)