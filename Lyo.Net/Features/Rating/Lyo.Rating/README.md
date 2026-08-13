# Lyo.Rating

Abstractions for rating and reviewing arbitrary entities, plus like/dislike reactions on those ratings. Each rating is keyed by `(forEntity, fromEntity, subject)`, so the same actor can rate the same target on multiple subject axes (e.g. `"scary"`, `"action"`). Subjects of `null` represent a general rating.

## Examples

### `RatingReactionType`

```csharp
public enum RatingReactionType
{
    Like = 0,
    Dislike = 1
}
```

### `RatingRef`

```csharp
public static EntityRef ForRating(Guid ratingId)
    => EntityRef.ForKey("Rating", ratingId.ToString());
```

## Surface — `IRatingStore`

- `SaveAsync(RatingRecord rating, CancellationToken ct = default)` — upserts. When an active row with the same `(forEntity, fromEntity, subject)` tuple already exists, its `Value`, `Title`, `Message`, `LikeCount`, and `DislikeCount` are updated; otherwise a new row is inserted.
- `GetByIdAsync(Guid id, CancellationToken ct = default)` — single rating by id.
- `GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` — every active rating for a target entity (across all raters and subjects).
- `GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)` — the specific rating a single actor left on a target for the given subject.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` — every rating authored by the given actor.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)` — every rating for a target *type*, optionally narrowed to a single target id.
- `DeleteAsync(Guid id, CancellationToken ct = default)` — soft-delete a rating (and remove its reactions).
- `DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)` — soft-delete the rating(s) for a `(forEntity, fromEntity, subject)` tuple.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` — soft-delete every rating attached to a target (and their reactions).

## Surface — `RatingRecord`

- `Subject` — optional axis label (e.g. `"scary"`); `null` is a general rating.
- `Title` — optional review title.
- `Value` — optional `decimal` score.
- `Message` — optional review body.
- `LikeCount` / `DislikeCount` — cached counters maintained by the reaction methods.
- `UpdatedTimestamp` — last update time (UTC), nullable.

## Surface — `RatingReactionRecord`

Standalone row (not **`EntityRelationRow`**) with subject/actor columns (parent rating on `for_entity_*`; reactor on `from_entity_*`), plus `ReactionType` and `CreatedTimestamp`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)