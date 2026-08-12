# Lyo.Tag.Postgres

PostgreSQL implementation of `Lyo.Tag` using Entity Framework Core. Persists tags to the `tag.tag` table (schema constant: `PostgresTagOptions.Schema = "tag"`) with migrations
support. Tags carry **subject** / optional **actor** (`for_entity_*` / `from_entity_*`) and are uniquely keyed by `(for_entity_type, for_entity_id, name, tag_type, slug)` per
tenant.

`PostgresTagStore` implements `ITagStore` and `Lyo.Health.IHealth` (`HealthCheckName = "tag-postgres"`), so registering the store also wires up a liveness probe.

## Examples

### Usage

```csharp
services.AddPostgresTagStore(new PostgresTagOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Usage (2)

```json
{
  "PostgresTag": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

### Usage (3)

```csharp
services.AddPostgresTagStoreFromConfiguration(configuration);
```

### Example: Tag a docket as urgent

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

### Migrations

```bash
export TAG_CONNECTION_STRING="Host=localhost;Database=lyo;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Tag/Lyo.Tag.Postgres
```

## DI extensions

- `AddTagDbContextFactory(Action<PostgresTagOptions>)` / `AddTagDbContextFactory(PostgresTagOptions)` — register only the `IDbContextFactory<TagDbContext>` (useful when consuming
  the schema directly from migrations or another store).
- `AddTagDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresTagOptions.SectionName)` — same, but bound from configuration.
- `AddPostgresTagStore(Action<PostgresTagOptions>)` / `AddPostgresTagStore(PostgresTagOptions)` — register the DbContext factory **and** the `ITagStore` singleton.
- `AddPostgresTagStoreFromConfiguration(IConfiguration, string sectionName = PostgresTagOptions.SectionName)` — register the store using configuration binding. The default section
  name is `PostgresTag`.

## Usage

Or with configuration:

## Entity Reference

Uses `Lyo.EntityReference.Models.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Schema

- **tag.tag** – `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id` — nullable varchar 128/256), `name`, `slug`,
  `tag_type`, `tenant_id` (uuid), lifecycle from **`EntityRelationEntityBase`**, plus tag-specific indexes
- Unique index on (for_entity_type, for_entity_id, tag)
- Index on (for_entity_type, for_entity_id)
- Index on tag

## Tenancy

`PostgresTagStore` accepts an optional `Guid? tenantId` on every read/write method (mirroring `IFavoriteStore`) and resolves it through `TenancyResolver`
under the policy configured in `PostgresTagOptions.Tenancy` (inheriting from
`EntityRefOptions.Mode` when unset). The `tenant_id` column is non-null, so only
`SingleTenantDefault` and `MultiTenantStrict` modes are valid — `SystemOnly` is rejected at store construction. The store applies a `WhereTenant` filter on every query. See
[`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy)
for the full policy matrix and `appsettings.json` snippet.

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

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Lyo.Tag` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)