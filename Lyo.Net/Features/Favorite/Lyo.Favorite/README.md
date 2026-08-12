# Lyo.Favorite

Abstractions for "X favorited Y" relationships across any two entities. The API accepts `EntityRef` at the boundary (so any feature can produce a favorite); the default Postgres
store persists subject/actor as **nullable varchar** (`for_entity_*` / `from_entity_*` columns), using **`EntityRefPersistedGuid.PersistedEntityId()`** when callers pass Guid
strings in `EntityRef.EntityId`.

## Surface — `IFavoriteStore`

- `SaveAsync(FavoriteRecord favorite, Guid? tenantId = null, string? context = null, CancellationToken ct = default)` — idempotent: if the same `(tenant, subject, actor, context)`
  tuple already has an active row, the call is a no-op.
- `DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)` — soft-delete by row id.
- `DeleteAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)` — soft-delete the single row for a
  `(forEntity, fromEntity, context)` pair.
- `DeleteForEntityAsync(EntityRef forEntity, ...)` — soft-delete every row for a given target entity (optionally filtered by `context`).
- `DeleteFromEntityAsync(EntityRef fromEntity, ...)` — soft-delete every row added by a given actor (optionally filtered by `context`).

## Surface — `FavoriteRecord`

Derives from **`EntityRelationRow`** (subject/actor endpoints: `SubjectEntityType` / `SubjectEntityId`, `ActorEntityType` / `ActorEntityId`; DB columns `for_entity_*` /
`from_entity_*`), plus `TenantId`, `Context`, `Visibility`, and lifecycle fields. **`SubjectRef`** / **`ActorRef`** project `EntityRef` at the boundary.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)