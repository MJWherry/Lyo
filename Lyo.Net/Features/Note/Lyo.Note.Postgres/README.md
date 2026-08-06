# Lyo.Note.Postgres

PostgreSQL implementation of `Lyo.Note` using Entity Framework Core. Persists notes to the `note.note` table (schema constant: `PostgresNoteOptions.Schema = "note"`) with migrations support. Notes have **subject** / **actor** entity references (`for_entity_*` / `from_entity_*`).

`PostgresNoteStore` implements `INoteStore` and `Lyo.Health.IHealth` (`HealthCheckName = "note-postgres"`), so registering the store also wires up a liveness probe.

## Examples

### Usage

```csharp
services.AddPostgresNoteStore(new PostgresNoteOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Usage (2)

```json
{
  "PostgresNote": {
    "ConnectionString": "Host=localhost;Database=note;...",
    "EnableAutoMigrations": true
  }
}
```

### Usage (3)

```csharp
services.AddPostgresNoteStoreFromConfiguration(configuration);
```

### Example: a user writes a note about a docket

```csharp
await noteStore.SaveAsync(new NoteRecord {
    SubjectEntityType = "Docket",
    SubjectEntityId = docketId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = userId.ToString(),
    Content = "Follow up next week"
});

// Update by passing the same Id back through SaveAsync.
var existing = await noteStore.GetByIdAsync(noteId);
existing!.Content = "Follow up tomorrow";
await noteStore.SaveAsync(existing);
```

### Migrations

```bash
export NOTE_CONNECTION_STRING="Host=localhost;Database=note;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Note/Lyo.Note.Postgres
```

## DI extensions

- `AddNoteDbContextFactory(Action<PostgresNoteOptions>)` / `AddNoteDbContextFactory(PostgresNoteOptions)` — register only the `IDbContextFactory<NoteDbContext>`.
- `AddNoteDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresNoteOptions.SectionName)` — same, bound from configuration (default section: `PostgresNote`).
- `AddPostgresNoteStore(Action<PostgresNoteOptions>)` / `AddPostgresNoteStore(PostgresNoteOptions)` — register the DbContext factory **and** the `INoteStore` singleton.
- `AddPostgresNoteStoreFromConfiguration(IConfiguration, string sectionName = PostgresNoteOptions.SectionName)` — register the store using configuration binding.

## Usage

Or with configuration:

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

## Schema

- **note.note** — **`EntityRelationEntityBase`**: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id` — nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, `metadata` (jsonb), plus note-specific `content` and `updated_timestamp`.

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

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Note` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
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