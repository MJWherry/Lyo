# Lyo.Favorite

Abstractions for "X favorited Y" relationships across any two entities. The API
accepts `EntityRef` at the boundary (so any feature can produce a favorite);
the default Postgres store persists the underlying ids as a single `Guid` per
"Option A" persistence.

## Surface

### `IFavoriteStore`

All methods accept an optional `tenantId` (null resolves to
`EntityRefOptions.DefaultTenantId`) and an optional `context` string for
scoping to a workspace / personal / module-specific bucket.

**Writes**

- `SaveAsync(FavoriteRecord favorite, Guid? tenantId = null, string? context = null, CancellationToken ct = default)`
  — idempotent: if the same `(tenant, ForEntity, FromEntity, context)` tuple
  already has an active row, the call is a no-op.
- `DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)`
  — soft-delete by row id.
- `DeleteAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)`
  — soft-delete the single row for a `(forEntity, fromEntity, context)` pair.
- `DeleteForEntityAsync(EntityRef forEntity, ...)` — soft-delete every row for
  a given target entity (optionally filtered by `context`).
- `DeleteFromEntityAsync(EntityRef fromEntity, ...)` — soft-delete every row
  added by a given actor (optionally filtered by `context`).

**Reads**

- `GetByIdAsync(Guid id, Guid? tenantId = null, ...)` — single row by id
  (non-deleted only).
- `GetAsync(EntityRef forEntity, EntityRef fromEntity, ...)` — the specific
  row for a target/actor pair, or `null` when not favorited.
- `IsFavoritedAsync(EntityRef forEntity, EntityRef fromEntity, ...)` — boolean
  membership probe.
- `GetForEntityAsync(EntityRef forEntity, ...)` — everyone who favorited the
  target entity.
- `GetFromEntityAsync(EntityRef fromEntity, ...)` — everything an actor has
  favorited.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default)`
  — all favorites for a target *type*, optionally narrowed to a single target id.

**Counts**

- `GetCountForEntityAsync(EntityRef forEntity, ...)` — favorite count for a
  single entity.
- `GetFavoriteCountsForEntitiesAsync(string forEntityType, IReadOnlyList<Guid> forEntityIds, Guid? tenantId = null, CancellationToken ct = default)`
  — batch counts keyed by `forEntityId`. Ids missing from the result have a
  count of zero (the row is omitted entirely).

### `FavoriteRecord`

Derives from `Lyo.EntityReference.Models.EntityRefRow`, so each row carries the
standard `ForEntityType` / `ForEntityId` / `FromEntityType` / `FromEntityId` /
`TenantId` / `Context` / `Visibility` / lifecycle columns. No favorite-specific
columns are added; `ForEntity` and `FromEntity` are convenience projections.

## Related projects

- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.Favorite.Postgres`](../Lyo.Favorite.Postgres/README.md)
