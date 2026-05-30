# Lyo.Tag

Abstractions for tagging arbitrary entities. Tags are keyed off an `EntityRef`
(what is tagged) and optionally a second `EntityRef` (who applied the tag),
so any feature in the framework can attach tags without a foreign-key coupling.

## Surface

### `ITagStore`

All operations are tenant-scoped and use soft-delete semantics in the
default Postgres implementation.

- `AddTagAsync(EntityRef forEntity, string tag, string tagType = "tag", EntityRef? fromEntity = null, string? slug = null, CancellationToken ct = default)`
  — adds a tag. Idempotent for the same `(forEntity, tag, tagType, slug)` tuple.
  `slug` is normalized to empty when null or whitespace. When `fromEntity` is
  omitted, the well-known system actor is used.
- `RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)`
  — removes the matching assignment. `slug` must match the stored slug
  (empty when none was stored).
- `GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, CancellationToken ct = default)`
  — returns every `TagRecord` on the given entity, optionally filtered by `tagType`.
- `GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, CancellationToken ct = default)`
  — returns every entity carrying the given tag value, with optional filters on
  entity type and tag type.
- `GetAllTagsForEntityTypeAsync(string forEntityType, string? tagType = null, CancellationToken ct = default)`
  — returns the distinct set of tag values applied to any entity of the given type.
- `RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default)`
  — soft-deletes every tag assignment for an entity.

### `TagRecord`

Derives from **`EntityRelationRow`** (subject/actor endpoints + `TenantId`, lifecycle, `Visibility`, `MetadataJson`, `Context`; DB `for_entity_*` / `from_entity_*`) plus:

- `Name` — the tag display value (e.g. `"urgent"`).
- `TagType` — discriminator for the tag namespace; defaults to `"tag"`
  (`"category"`, `"label"`, etc. are common conventions).
- `Slug` — optional URL-friendly slug; empty when none was supplied.
- **`SubjectRef`** / **`ActorRef`** — `EntityRef` projections.

## Related projects

- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.Tag.Postgres`](../Lyo.Tag.Postgres/README.md)
