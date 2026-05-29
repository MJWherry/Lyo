# Lyo.Note.Postgres

PostgreSQL implementation of `Lyo.Note` using Entity Framework Core. Persists
notes to the `note.note` table (schema constant:
`PostgresNoteOptions.Schema = "note"`) with migrations support. Notes have
**For** (what the note is about) and **From** (who wrote it) entity references.

`PostgresNoteStore` implements `INoteStore` and `Lyo.Health.IHealth`
(`HealthCheckName = "note-postgres"`), so registering the store also wires up a
liveness probe.

## DI extensions

Defined in `Extensions.cs` as `IServiceCollection` extensions:

- `AddNoteDbContextFactory(Action<PostgresNoteOptions>)` /
  `AddNoteDbContextFactory(PostgresNoteOptions)` — register only the
  `IDbContextFactory<NoteDbContext>`.
- `AddNoteDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresNoteOptions.SectionName)`
  — same, bound from configuration (default section: `PostgresNote`).
- `AddPostgresNoteStore(Action<PostgresNoteOptions>)` /
  `AddPostgresNoteStore(PostgresNoteOptions)` — register the DbContext factory
  **and** the `INoteStore` singleton.
- `AddPostgresNoteStoreFromConfiguration(IConfiguration, string sectionName = PostgresNoteOptions.SectionName)`
  — register the store using configuration binding.

## Usage

```csharp
services.AddPostgresNoteStore(new PostgresNoteOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

Or with configuration:

```json
{
  "PostgresNote": {
    "ConnectionString": "Host=localhost;Database=note;...",
    "EnableAutoMigrations": true
  }
}
```

```csharp
services.AddPostgresNoteStoreFromConfiguration(configuration);
```

## Migrations

```bash
export NOTE_CONNECTION_STRING="Host=localhost;Database=note;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Note/Lyo.Note.Postgres
```

## Entity Reference

Uses `Lyo.EntityReference.Models.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);
var composite = EntityRef.For<Order>("ord-1", "line-2");

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Example: a user writes a note about a docket

```csharp
await noteStore.SaveAsync(new NoteRecord {
    ForEntityType = "Docket",
    ForEntityId = docketId,
    FromEntityType = "User",
    FromEntityId = userId,
    Content = "Follow up next week"
});

// Update by passing the same Id back through SaveAsync.
var existing = await noteStore.GetByIdAsync(noteId);
existing!.Content = "Follow up tomorrow";
await noteStore.SaveAsync(existing);
```

## Schema

Schema name: `note` (`PostgresNoteOptions.Schema`).

- **note.note** — derived from `EntityRefRow`, so it includes `id` (uuid),
  `for_entity_type`, `for_entity_id` (uuid), `from_entity_type`,
  `from_entity_id` (uuid), `tenant_id`, `context`, `visibility`, `created_at`,
  `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`,
  `metadata` (jsonb), plus note-specific `content` and `updated_timestamp`.

## Tenancy

`PostgresNoteStore` accepts an optional `Guid? tenantId` on every read/write
method and resolves it through `TenancyResolver` under the policy configured in
`PostgresNoteOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when
unset). The `tenant_id` column is non-null, so only `SingleTenantDefault` and
`MultiTenantStrict` modes are valid — `SystemOnly` is rejected at store
construction. The store applies a `WhereTenant` filter on every query so notes
from one tenant cannot leak into another. See
[`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy)
for the full policy matrix and `appsettings.json` snippet.

```json
{
  "PostgresNote": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

*(Synchronized from `Lyo.Note.Postgres.csproj`.)*

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
- [`Lyo.Note`](../Lyo.Note/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)