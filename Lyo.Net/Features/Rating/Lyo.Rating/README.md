# Lyo.Rating

Abstractions for rating and reviewing arbitrary entities, plus like/dislike
reactions on those ratings. Each rating is keyed by `(forEntity, fromEntity,
subject)`, so the same actor can rate the same target on multiple subject axes
(e.g. `"scary"`, `"action"`). Subjects of `null` represent a general rating.

## Surface

### `IRatingStore`

**Ratings**

- `SaveAsync(RatingRecord rating, CancellationToken ct = default)` — upserts.
  When an active row with the same `(forEntity, fromEntity, subject)` tuple
  already exists, its `Value`, `Title`, `Message`, `LikeCount`, and
  `DislikeCount` are updated; otherwise a new row is inserted.
- `GetByIdAsync(Guid id, CancellationToken ct = default)` — single rating by id.
- `GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` —
  every active rating for a target entity (across all raters and subjects).
- `GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)`
  — the specific rating a single actor left on a target for the given subject.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` —
  every rating authored by the given actor.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)`
  — every rating for a target *type*, optionally narrowed to a single target id.
- `DeleteAsync(Guid id, CancellationToken ct = default)` — soft-delete a rating
  (and remove its reactions).
- `DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)`
  — soft-delete the rating(s) for a `(forEntity, fromEntity, subject)` tuple.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` —
  soft-delete every rating attached to a target (and their reactions).

**Reactions**

- `AddReactionAsync(EntityRef ratingRef, EntityRef fromEntity, RatingReactionType reactionType, CancellationToken ct = default)`
  — one reaction per actor per rating; switching from `Like` to `Dislike` (or
  vice versa) updates the existing reaction and adjusts the cached counters on
  the parent `RatingRecord`. Calls against a non-existent rating no-op.
- `RemoveReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default)`
  — clears the actor's reaction and decrements the matching counter.
- `GetReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default)`
  — current reaction for the pair, or `null`.

### `RatingReactionType`

```csharp
public enum RatingReactionType
{
    Like = 0,
    Dislike = 1
}
```

### `RatingRef`

Helper for building the `EntityRef` to pass to reaction methods:

```csharp
public static EntityRef ForRating(Guid ratingId)
    => EntityRef.ForKey("Rating", ratingId.ToString());
```

### `RatingRecord`

Derives from `EntityRefRow` (standard `For*` / `From*` / `TenantId` /
`Context` / `Visibility` / lifecycle columns). Rating-specific fields:

- `Subject` — optional axis label (e.g. `"scary"`); `null` is a general rating.
- `Title` — optional review title.
- `Value` — optional `decimal` score.
- `Message` — optional review body.
- `LikeCount` / `DislikeCount` — cached counters maintained by the reaction
  methods.
- `UpdatedTimestamp` — last update time (UTC), nullable.

### `RatingReactionRecord`

Standalone row (not an `EntityRefRow`) that stores `(Id, ForEntityType,
ForEntityId, FromEntityType, FromEntityId, ReactionType, CreatedTimestamp)`.
`ForEntity` is always the parent rating (`EntityType == "Rating"`).

## Related projects

- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.Rating.Postgres`](../Lyo.Rating.Postgres/README.md)
