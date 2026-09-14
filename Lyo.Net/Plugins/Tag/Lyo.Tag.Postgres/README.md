# Lyo.Tag.Postgres

Entity Framework Core store for `Lyo.Tag` on PostgreSQL. Rows live in `tag.tag` (`PostgresTagOptions.Schema = "tag"`), and the package includes migrations. Each row carries a **subject** and an optional **actor** (`for_entity_*` / `from_entity_*`). Per tenant, uniqueness is `(for_entity_type, for_entity_id, name, tag_type, slug)`.

`PostgresTagStore` implements `ITagStore` and `Lyo.Health.IHealth` (`HealthCheckName = "tag-postgres"`). Registering the store also registers a liveness probe.

## Examples

### Register the store

```csharp
services.AddPostgresTagStore(new PostgresTagOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### appsettings.json

```json
{
  "PostgresTag": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

### Bind from configuration

```csharp
services.AddPostgresTagStoreFromConfiguration(configuration);
```

### Mark a docket urgent

```csharp
// Add tag (idempotent – no-op if already exists)
await tagStore.AddTagAsync(
    EntityRef.ForGuid("Docket", docketId),
    "urgent",
    EntityRef.ForKey("User", userId.ToString()));

// Get all tags for an entity
var tags = await tagStore.GetTagsForEntityAsync(EntityRef.ForGuid("Docket", docketId));

// Find all dockets with "urgent" tag
var urgentDockets = await tagStore.GetEntitiesWithTagAsync("urgent", "Docket");

// Remove a tag
await tagStore.RemoveTagAsync(EntityRef.ForGuid("Docket", docketId), "urgent");

// Remove all tags from an entity
await tagStore.RemoveAllTagsForEntityAsync(EntityRef.ForGuid("Docket", docketId));
```

### Add a migration

```bash
export TAG_CONNECTION_STRING="Host=localhost;Database=lyo;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Tag/Lyo.Tag.Postgres
```

## Service registration

- `AddTagDbContextFactory(Action<PostgresTagOptions>)` / `AddTagDbContextFactory(PostgresTagOptions)` register the `IDbContextFactory<TagDbContext>` only. Handy when you consume the schema from migrations or another store.
- `AddTagDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresTagOptions.SectionName)` does the same, bound from configuration.
- `AddPostgresTagStore(Action<PostgresTagOptions>)` / `AddPostgresTagStore(PostgresTagOptions)` register the DbContext factory **and** the `ITagStore` singleton.
- `AddPostgresTagStoreFromConfiguration(IConfiguration, string sectionName = PostgresTagOptions.SectionName)` registers the store by binding configuration. The default section name is `PostgresTag`.

## From configuration

The same registration from configuration:

## Building an EntityRef

Create refs with `Lyo.EntityReference.Models.EntityRef`, either generic or from strings:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Database schema

- **tag.tag.** `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, nullable varchar 128/256), `name`, `slug`, `tag_type`, `tenant_id` (uuid), lifecycle from `EntityRelationEntityBase`, plus tag-specific indexes
- Unique index on (for_entity_type, for_entity_id, tag)
- Index on (for_entity_type, for_entity_id)
- Index on tag

## Tenant isolation

Every read and write on `PostgresTagStore` takes an optional `Guid? tenantId` (same shape as `IFavoriteStore`). `TenancyResolver` applies the policy in `PostgresTagOptions.Tenancy`. When that is unset, it inherits `EntityRefOptions.Mode`. Because `tenant_id` is non-null, only `SingleTenantDefault` and `MultiTenantStrict` are valid. Construction rejects `SystemOnly`. Every query gets a `WhereTenant` filter. The policy matrix and an `appsettings.json` snippet live in [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy).

```json
{
  "PostgresTag": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Tag` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)