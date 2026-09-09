# Lyo.Favorite

Contracts for "X favorited Y" ties between any two entities. The boundary takes `EntityRef`, so any feature can create a favorite. The default Postgres store writes subject/actor as nullable varchar (`for_entity_*` / `from_entity_*` columns). When callers put Guid strings in `EntityRef.EntityId`, it uses `EntityRefPersistedGuid.PersistedEntityId()`.

## `IFavoriteStore`

- `SaveAsync(FavoriteRecord favorite, Guid? tenantId = null, string? context = null, CancellationToken ct = default)` is idempotent. If an active row already exists for the same `(tenant, subject, actor, context)` tuple, the call does nothing.
- `DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)` soft-delete by row id.
- `DeleteAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)` soft-delete the single row for a `(forEntity, fromEntity, context)` pair.
- `DeleteForEntityAsync(EntityRef forEntity, ...)` soft-delete every row for a given target. You can also filter by `context`.
- `DeleteFromEntityAsync(EntityRef fromEntity, ...)` soft-delete every row added by a given actor. You can also filter by `context`.

## `FavoriteRecord`

Comes from `EntityRelationRow` (subject/actor endpoints: `SubjectEntityType` / `SubjectEntityId`, `ActorEntityType` / `ActorEntityId`; DB columns `for_entity_*` / `from_entity_*`), plus `TenantId`, `Context`, `Visibility`, and lifecycle fields. `SubjectRef` / `ActorRef` project `EntityRef` at the boundary.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)