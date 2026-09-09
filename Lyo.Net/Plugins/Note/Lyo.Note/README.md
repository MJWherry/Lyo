# Lyo.Note

Contracts for notes hung on entities. A note has a **subject** (what it is about) and an **actor** (who wrote it), both as `EntityRef`. The default Postgres store maps `for_entity_*` / `from_entity_*` (nullable varchar) and uses soft-delete.

## `INoteStore`

- `SaveAsync(NoteRecord note, CancellationToken ct = default)` insert or update. If `note.Id` matches an active row, that row's subject/actor endpoints and `Content` are updated in place. Otherwise a new row is inserted. A new `Id` is generated when `Id == default`.
- `GetByIdAsync(Guid id, CancellationToken ct = default)` one note by id. Active rows only.
- `GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` every note on that target, ordered by `CreatedAt`.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` every note written by that actor, ordered by `CreatedAt`.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)` every note for a target *type*. Optionally restrict to one target id.
- `DeleteAsync(Guid id, CancellationToken ct = default)` soft-delete one note.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` soft-delete every note on that target.

## `NoteRecord`

- `Content`. Note body. A string, and it may be empty.
- `UpdatedTimestamp`. Last update time (UTC). Nullable.
- `SubjectRef` / `ActorRef`. Projected as `EntityRef`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)