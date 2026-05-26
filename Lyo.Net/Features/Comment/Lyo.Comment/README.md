# Lyo.Comment

Abstractions for attaching threaded, reactable comments to any entity. Each
comment carries a **For** entity (what the comment is about), a **From** entity
(the author), optional **ReplyToCommentId** for threads, and cached
like/dislike counters maintained via the reaction methods.

## Surface

### `ICommentStore`

**Comments**

- `SaveAsync(CommentRecord comment, CancellationToken ct = default)` — insert
  or update. When `comment.Id` matches an existing active row, that row's
  `ForEntity`, `FromEntity`, `Content`, and `ReplyToCommentId` are updated and
  `IsEdited` is forced to `true`; otherwise a new row is inserted (and an `Id`
  is generated if `Id == default`).
- `GetByIdAsync(Guid id, CancellationToken ct = default)` — single comment by
  id.
- `GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, CancellationToken ct = default)`
  — every comment attached to a target entity, ordered by `CreatedAt`. With
  `includeReplies: false`, only top-level comments (rows where
  `ReplyToCommentId == null`) are returned.
- `GetRepliesAsync(Guid replyToCommentId, CancellationToken ct = default)` —
  direct replies to a given comment.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` —
  every comment authored by the given actor.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)`
  — every comment for a target *type*, optionally narrowed to a single target id.
- `DeleteAsync(Guid id, bool deleteReplies = false, CancellationToken ct = default)`
  — soft-deletes the comment. When `deleteReplies: true`, recursively
  soft-deletes every descendant in the reply tree and removes their reactions.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` —
  soft-delete every comment attached to a target (and remove their reactions).

**Reactions**

- `AddReactionAsync(EntityRef commentRef, EntityRef fromEntity, CommentReactionType reactionType, CancellationToken ct = default)`
  — one reaction per actor per comment; switching `Like` ↔ `Dislike` updates
  the existing reaction in place and adjusts the cached counters on the
  parent `CommentRecord`. No-ops when the comment doesn't exist.
- `RemoveReactionAsync(EntityRef commentRef, EntityRef fromEntity, CancellationToken ct = default)`
  — removes the actor's reaction and decrements the matching counter.
- `GetReactionAsync(EntityRef commentRef, EntityRef fromEntity, CancellationToken ct = default)`
  — current reaction or `null`.

### `CommentReactionType`

```csharp
public enum CommentReactionType
{
    Like = 0,
    Dislike = 1
}
```

### `CommentRef`

Helper for building the comment `EntityRef` to pass to the reaction methods:

```csharp
public static EntityRef ForComment(Guid commentId)
    => EntityRef.ForKey("Comment", commentId.ToString());
```

### `CommentRecord`

Derives from `EntityRefRow` (standard `For*` / `From*` / `TenantId` /
`Context` / `Visibility` / lifecycle columns). Comment-specific fields:

- `Content` — comment body.
- `ReplyToCommentId` — parent comment id when this is a reply, `null` for
  top-level.
- `LikeCount` / `DislikeCount` — cached counters maintained by the reaction
  methods.
- `UpdatedTimestamp` — last update time (UTC), nullable.
- `IsEdited` — set to `true` by `SaveAsync` whenever an existing row is
  updated.

### `CommentReactionRecord`

Standalone row (not an `EntityRefRow`) that stores `(Id, ForEntityType,
ForEntityId, FromEntityType, FromEntityId, ReactionType, CreatedTimestamp)`.
`ForEntity` is always the parent comment (`EntityType == "Comment"`).

## Related projects

- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.Comment.Postgres`](../Lyo.Comment.Postgres/README.md)
