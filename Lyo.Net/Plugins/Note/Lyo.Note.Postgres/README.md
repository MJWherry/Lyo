# Lyo.Note.Postgres

Entity Framework Core store for `Lyo.Note` on PostgreSQL. Rows live in `note.note` (`PostgresNoteOptions.Schema = "note"`), and the package includes migrations. Notes carry **subject** / **actor** entity references (`for_entity_*` / `from_entity_*`).

`PostgresNoteStore` implements `INoteStore` and `Lyo.Health.IHealth` (`HealthCheckName = "note-postgres"`). Registering the store also registers a liveness probe.

## Examples

### Register the store

```csharp
services.AddPostgresNoteStore(new PostgresNoteOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### appsettings.json

```json
{
  "PostgresNote": {
    "ConnectionString": "Host=localhost;Database=note;...",
    "EnableAutoMigrations": true
  }
}
```

### Bind from configuration

```csharp
services.AddPostgresNoteStoreFromConfiguration(configuration);
```

### A user notes a docket

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

### Add a migration

```bash
export NOTE_CONNECTION_STRING="Host=localhost;Database=note;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Note/Lyo.Note.Postgres
```

## Service registration

- `AddNoteDbContextFactory(Action<PostgresNoteOptions>)` / `AddNoteDbContextFactory(PostgresNoteOptions)` register the `IDbContextFactory<NoteDbContext>` only.
- `AddNoteDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresNoteOptions.SectionName)` does the same, bound from configuration. Default section: `PostgresNote`.
- `AddPostgresNoteStore(Action<PostgresNoteOptions>)` / `AddPostgresNoteStore(PostgresNoteOptions)` register the DbContext factory **and** the `INoteStore` singleton.
- `AddPostgresNoteStoreFromConfiguration(IConfiguration, string sectionName = PostgresNoteOptions.SectionName)` registers the store by binding configuration.

## From configuration

The same registration from configuration:

## Building an EntityRef

Create refs with `Lyo.EntityReference.Models.EntityRef`, either generic or from strings:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);
var composite = EntityRef.For<Order>("ord-1", "line-2");

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Database schema

- **note.note.** `EntityRelationEntityBase`: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, `metadata` (jsonb), plus note-specific `content` and `updated_timestamp`.

## Tenant isolation

Every read and write on `PostgresNoteStore` takes an optional `Guid? tenantId`. `TenancyResolver` applies the policy in `PostgresNoteOptions.Tenancy`. When that is unset, it inherits `EntityRefOptions.Mode`. Because `tenant_id` is non-null, only `SingleTenantDefault` and `MultiTenantStrict` are valid. Construction rejects `SystemOnly`. Every query gets a `WhereTenant` filter, so one tenant's notes cannot appear in another. The policy matrix and an `appsettings.json` snippet live in [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy).

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

- `Lyo.Configuration` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Note` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)