# Lyo.Tag.Postgres

PostgreSQL implementation of Lyo.Tag using Entity Framework Core. Persists tags to `tag.tag` table with migrations support. Tags have **For** (what is tagged) and optional **From
** (who applied the tag) entity references. Unique constraint on (for_entity_type, for_entity_id, tag).

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
services.AddPostgresTagStore(configuration);
```

## Migrations

```bash
export TAG_CONNECTION_STRING="Host=localhost;Database=lyo;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Tag/Lyo.Tag.Postgres
```

## Entity Reference

Uses `Lyo.Common.EntityRef` with generic or string-based creation:

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

- **tag.tag** – `id` (uuid), `for_entity_type`, `for_entity_id`, `tag`, `from_entity_type`, `from_entity_id`, `created_timestamp`
- Unique index on (for_entity_type, for_entity_id, tag)
- Index on (for_entity_type, for_entity_id)
- Index on tag

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Tag.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.EntityFrameworkCore.Design` | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Health`
- `Lyo.Postgres`
- `Lyo.Tag`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `InitialCreate`
- `PostgresTagOptions`
- `PostgresTagStore`
- `TagDbContext`
- `TagDbContextFactory`
- `TagEntity`
- `TagEntityConfiguration`

<!-- LYO_README_SYNC:END -->

