# Lyo.Rating

Contracts for rating and reviewing entities, plus like/dislike reactions on those ratings. Each rating is keyed by `(forEntity, fromEntity, subject)`, so one actor can rate the same target on several subject axes (for example `"scary"`, `"action"`). A `null` subject is a general rating.

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

## `IRatingStore`

- `SaveAsync(RatingRecord rating, CancellationToken ct = default)` upserts. If an active row already exists for the same `(forEntity, fromEntity, subject)` tuple, its `Value`, `Title`, `Message`, `LikeCount`, and `DislikeCount` are updated. Otherwise a new row is inserted.
- `GetByIdAsync(Guid id, CancellationToken ct = default)` one rating by id.
- `GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` every active rating for a target (all raters and subjects).
- `GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)` the rating one actor left on a target for that subject.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` every rating authored by that actor.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)` every rating for a target *type*. Optionally restrict to one target id.
- `DeleteAsync(Guid id, CancellationToken ct = default)` soft-delete a rating and remove its reactions.
- `DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)` soft-delete the rating(s) for a `(forEntity, fromEntity, subject)` tuple.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` soft-delete every rating on a target, and their reactions.

## `RatingRecord`

- `Subject`. Optional axis label, such as `"scary"`. `null` is a general rating.
- `Title`. Optional review title.
- `Value`. Optional `decimal` score.
- `Message`. Optional review body.
- `LikeCount` / `DislikeCount`. Cached counters kept current by the reaction methods.
- `UpdatedTimestamp`. Last update time (UTC). Nullable.

## `RatingReactionRecord`

A standalone row (not `EntityRelationRow`) with subject/actor columns (parent rating on `for_entity_*`; reactor on `from_entity_*`), plus `ReactionType` and `CreatedTimestamp`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)