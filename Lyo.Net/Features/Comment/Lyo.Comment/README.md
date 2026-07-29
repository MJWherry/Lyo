# Lyo.Comment

Abstractions for attaching threaded, reactable comments to any entity. Each
comment carries a **subject** (what it is about), an **actor** (author), optional **ReplyToCommentId** for threads, and cached
like/dislike counters maintained via the reaction methods.

## Examples

### `CommentReactionType`

```csharp
public enum CommentReactionType
{
    Like = 0,
    Dislike = 1
}
```

### `CommentRef`

```csharp
public static EntityRef ForComment(Guid commentId)
    => EntityRef.ForKey("Comment", commentId.ToString());
```

## Surface — `ICommentStore`

- `SaveAsync(CommentRecord comment, CancellationToken ct = default)` — insert or update. When `comment.Id` matches an existing active row, that row's subject/actor endpoints, `Content`, and `ReplyToCommentId` are updated and `IsEdited` is forced to `true`; otherwise a new row is inserted (and an `Id` is generated if `Id == default`).
- `GetByIdAsync(Guid id, CancellationToken ct = default)` — single comment by id.
- `GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, CancellationToken ct = default)` — every comment attached to a target entity, ordered by `CreatedAt`. With `includeReplies: false`, only top-level comments (rows where `ReplyToCommentId == null`) are returned.
- `GetRepliesAsync(Guid replyToCommentId, CancellationToken ct = default)` — direct replies to a given comment.
- `GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)` — every comment authored by the given actor.
- `GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)` — every comment for a target *type*, optionally narrowed to a single target id.
- `DeleteAsync(Guid id, bool deleteReplies = false, CancellationToken ct = default)` — soft-deletes the comment. When `deleteReplies: true`, recursively soft-deletes every descendant in the reply tree and removes their reactions.
- `DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)` — soft-delete every comment attached to a target (and remove their reactions).

## Surface — `CommentRecord`

- `Content` — comment body.
- `ReplyToCommentId` — parent comment id when this is a reply, `null` for top-level.
- `LikeCount` / `DislikeCount` — cached counters maintained by the reaction methods.
- `UpdatedTimestamp` — last update time (UTC), nullable.
- `IsEdited` — set to `true` by `SaveAsync` whenever an existing row is updated.

## Surface — `CommentReactionRecord`

Standalone row (not **`EntityRelationRow`**) with subject/actor columns (`SubjectEntityType` / `SubjectEntityId` → parent comment; `ActorEntityType` / `ActorEntityId` → reactor; DB `for_entity_*` / `from_entity_*`), plus `ReactionType` and `CreatedTimestamp`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)