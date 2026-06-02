# Lyo.Tag.Postgres

PostgreSQL implementation of `Lyo.Tag` using Entity Framework Core. Persists
tags to the `tag.tag` table (schema constant: `PostgresTagOptions.Schema = "tag"`)
with migrations support. Tags carry **subject** / optional **actor** (`for_entity_*` / `from_entity_*`) and are uniquely keyed by
`(for_entity_type, for_entity_id, name, tag_type, slug)` per tenant.

`PostgresTagStore` implements `ITagStore` and `Lyo.Health.IHealth`
(`HealthCheckName = "tag-postgres"`), so registering the store also wires up a
liveness probe.

## DI extensions

Defined in `Extensions.cs` as `IServiceCollection` extensions:

- `AddTagDbContextFactory(Action<PostgresTagOptions>)` /
  `AddTagDbContextFactory(PostgresTagOptions)` — register only the
  `IDbContextFactory<TagDbContext>` (useful when consuming the schema directly
  from migrations or another store).
- `AddTagDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresTagOptions.SectionName)`
  — same, but bound from configuration.
- `AddPostgresTagStore(Action<PostgresTagOptions>)` /
  `AddPostgresTagStore(PostgresTagOptions)` — register the DbContext factory
  **and** the `ITagStore` singleton.
- `AddPostgresTagStoreFromConfiguration(IConfiguration, string sectionName = PostgresTagOptions.SectionName)`
  — register the store using configuration binding. The default section name is
  `PostgresTag`.

## Usage

```csharp
services.AddPostgresTagStore(new PostgresTagOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

Or with configuration:

```json
{
  "PostgresTag": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

```csharp
services.AddPostgresTagStoreFromConfiguration(configuration);
```

## Migrations

```bash
export TAG_CONNECTION_STRING="Host=localhost;Database=lyo;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Tag/Lyo.Tag.Postgres
```

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

## Example: Tag a docket as urgent

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

## Schema

- **tag.tag** – `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id` — nullable varchar 128/256), `name`, `slug`,
  `tag_type`, `tenant_id` (uuid), lifecycle from **`EntityRelationEntityBase`**, plus tag-specific indexes
- Unique index on (for_entity_type, for_entity_id, tag)
- Index on (for_entity_type, for_entity_id)
- Index on tag

## Tenancy

`PostgresTagStore` accepts an optional `Guid? tenantId` on every read/write
method (mirroring `IFavoriteStore`) and resolves it through `TenancyResolver`
under the policy configured in `PostgresTagOptions.Tenancy` (inheriting from
`EntityRefOptions.Mode` when unset). The `tenant_id` column is non-null, so only
`SingleTenantDefault` and `MultiTenantStrict` modes are valid — `SystemOnly` is
rejected at store construction. The store applies a `WhereTenant` filter on
every query. See
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

*(Synchronized from `Lyo.Tag.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.Tag`](../Lyo.Tag/README.md)