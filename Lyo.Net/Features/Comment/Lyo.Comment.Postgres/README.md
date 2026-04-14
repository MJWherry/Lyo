# Lyo.Comment.Postgres

PostgreSQL implementation of Lyo.Comment using Entity Framework Core. Persists comments to `comment.comment` table with migrations support. Comments have **For** (what the comment
is about), **From** (who wrote it), optional **ReplyTo** (parent comment), and like/dislike counts.

## Usage

```csharp
services.AddPostgresCommentStore(new PostgresCommentOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

Or with configuration:

```json
{
  "PostgresComment": {
    "ConnectionString": "Host=localhost;Database=comment;...",
    "EnableAutoMigrations": true
  }
}
```

## Migrations

```bash
export COMMENT_CONNECTION_STRING="Host=localhost;Database=comment;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Comment/Lyo.Comment.Postgres
```

## Features

- **For/From entity refs** – Same dynamic entity structure as Rating and Note
- **Reply threads** – `ReplyToCommentId` links to parent comment; `GetRepliesAsync(parentId)` fetches direct replies
- **Reactions (like/dislike)** – Tracked per user via `comment_reaction` table; one reaction per user per comment. Use `AddReactionAsync`, `RemoveReactionAsync`, `GetReactionAsync`
  with EntityRef for comment and reactor.
- **IsEdited** – Set when content is updated
- **Delete with replies** – `DeleteAsync(id, deleteReplies: true)` cascades to all nested replies and their reactions

## Example

```csharp
// Top-level comment
await commentStore.SaveAsync(new CommentRecord {
    ForEntityType = "Docket",
    ForEntityId = docketId.ToString(),
    FromEntityType = "User",
    FromEntityId = "123",
    Content = "Great work on this case!"
});

// Reply
await commentStore.SaveAsync(new CommentRecord {
    ForEntityType = "Docket",
    ForEntityId = docketId.ToString(),
    FromEntityType = "User",
    FromEntityId = "456",
    Content = "I agree!",
    ReplyToCommentId = parentCommentId
});

// Like a comment (uses EntityRef: comment ref + user ref)
var commentRef = CommentRef.ForComment(commentId);
var userRef = EntityRef.ForKey("User", "123");
await commentStore.AddReactionAsync(commentRef, userRef, CommentReactionType.Like);

// Check if user has reacted
var reaction = await commentStore.GetReactionAsync(commentRef, userRef);
// Remove reaction
await commentStore.RemoveReactionAsync(commentRef, userRef);
```

## Schema

- **comment.comment** – `id` (uuid), `for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, `content`, `reply_to_comment_id`, `like_count`, `dislike_count`,
  `is_edited`, `created_timestamp`, `updated_timestamp`
- **comment.comment_reaction** – `id` (uuid), `for_entity_type`, `for_entity_id` (comment id), `from_entity_type`, `from_entity_id` (reactor), `reaction_type` (0=Like, 1=Dislike),
  `created_timestamp`. Unique on (for_entity_type, for_entity_id, from_entity_type, from_entity_id)

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Comment.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.EntityFrameworkCore.Design` | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.Comment`
- `Lyo.Exceptions`
- `Lyo.Health`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*10*). Nested types and file-scoped namespaces may omit some entries.

- `CommentDbContext`
- `CommentDbContextFactory`
- `CommentEntity`
- `CommentEntityConfiguration`
- `CommentReactionEntity`
- `CommentReactionEntityConfiguration`
- `Extensions`
- `InitialCreate`
- `PostgresCommentOptions`
- `PostgresCommentStore`

<!-- LYO_README_SYNC:END -->

