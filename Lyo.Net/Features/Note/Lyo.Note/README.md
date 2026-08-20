# Lyo.Note

Abstractions for notes attached to entities. Each note has a **subject** (what it is about) and an **actor** (who wrote it), expressed as `EntityRef`. The default Postgres store maps `for_entity_*` / `from_entity_*` (nullable varchar) and applies soft-delete semantics.

## `INoteStore`

- `SaveAsync(NoteRecord note, CancellationToken ct = default)` insert or update. When `note.Id` matches an existing active row, that row's subject/actor endpoints and `Content` are updated in place; otherwise a new row is inserted (a new `Id` is generated when `Id == default`).
- `GetByIdAsync(Guid id, CancellationToken ct = default)` single note by id (active rows only).
- `GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` all notes attached to the given target entity, ordered by `CreatedAt`.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` all notes authored by the given actor entity, ordered by `CreatedAt`.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)` all notes for a target *type*, optionally narrowed to a single target id.
- `DeleteAsync(Guid id, CancellationToken ct = default)` soft-delete a single note.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` soft-delete every note attached to the given target entity.

## `NoteRecord`

- `Content`. Note body (string, may be empty).
- `UpdatedTimestamp`. Last update time (UTC), nullable.
- `SubjectRef` / `ActorRef`. `EntityRef` projections.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)