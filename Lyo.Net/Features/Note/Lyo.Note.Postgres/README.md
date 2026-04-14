# Lyo.Note.Postgres

PostgreSQL implementation of Lyo.Note using Entity Framework Core. Persists notes to `note.note` table with migrations support. Notes have **For** (what the note is about) and *
*From** (who wrote it) entity references.

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
services.AddPostgresNoteStore(configuration);
```

## Migrations

```bash
export NOTE_CONNECTION_STRING="Host=localhost;Database=note;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Note/Lyo.Note.Postgres
```

## Entity Reference

Uses `Lyo.Common.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);
var composite = EntityRef.For<Order>("ord-1", "line-2");

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Example: User 123 creates note for Docket

```csharp
await noteStore.SaveAsync(new NoteRecord {
    ForEntityType = "Docket",
    ForEntityId = docketId.ToString(),
    FromEntityType = "User",
    FromEntityId = "123",
    Content = "Follow up next week"
});
```

## Schema

- **note.note** – `id` (uuid), `for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, `content`, `created_timestamp`, `updated_timestamp`

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Note.Postgres.csproj`.)*

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
- `Lyo.Note`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `InitialCreate`
- `NoteDbContext`
- `NoteDbContextFactory`
- `NoteEntity`
- `NoteEntityConfiguration`
- `PostgresNoteOptions`
- `PostgresNoteStore`

<!-- LYO_README_SYNC:END -->

