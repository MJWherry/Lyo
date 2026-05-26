# Lyo.Comment.Postgres

PostgreSQL implementation of `Lyo.Comment` using Entity Framework Core. Persists
comments to the `comment.comment` table and reactions to
`comment.comment_reaction` (schema constant:
`PostgresCommentOptions.Schema = "comment"`) with migrations support. Comments
have **For** (what the comment is about), **From** (who wrote it), optional
**ReplyToCommentId** (parent comment), and cached `LikeCount` / `DislikeCount`
counters.

`PostgresCommentStore` implements `ICommentStore` and `Lyo.Health.IHealth`
(`HealthCheckName = "comment-postgres"`), so registering the store also wires
up a liveness probe.

## DI extensions

Defined in `Extensions.cs` as `IServiceCollection` extensions:

- `AddCommentDbContextFactory(Action<PostgresCommentOptions>)` /
  `AddCommentDbContextFactory(PostgresCommentOptions)` — register only the
  `IDbContextFactory<CommentDbContext>`.
- `AddCommentDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresCommentOptions.SectionName)`
  — same, bound from configuration (default section: `PostgresComment`).
- `AddPostgresCommentStore(Action<PostgresCommentOptions>)` /
  `AddPostgresCommentStore(PostgresCommentOptions)` — register the DbContext
  factory **and** the `ICommentStore` singleton.
- `AddPostgresCommentStoreFromConfiguration(IConfiguration, string sectionName = PostgresCommentOptions.SectionName)`
  — register the store using configuration binding.

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

```csharp
services.AddPostgresCommentStoreFromConfiguration(configuration);
```

## Migrations

```bash
export COMMENT_CONNECTION_STRING="Host=localhost;Database=comment;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Comment/Lyo.Comment.Postgres
```

## Features

- **For/From entity refs** — same dynamic entity-ref structure used by Rating
  and Note.
- **Reply threads** — `ReplyToCommentId` points to the parent comment;
  `GetRepliesAsync(parentId)` returns direct replies; `DeleteAsync(id,
  deleteReplies: true)` walks the descendant tree and soft-deletes every
  nested reply (plus their reactions).
- **Reactions (like/dislike)** — tracked per user via the `comment_reaction`
  table; exactly one reaction per user per comment. Flipping `Like` ↔ `Dislike`
  mutates the existing row and adjusts the cached counters on the parent
  comment.
- **IsEdited** — automatically set to `true` by `SaveAsync` whenever an
  existing row is updated.

## Example

```csharp
await commentStore.SaveAsync(new CommentRecord {
    ForEntityType = "Docket",
    ForEntityId = docketId,
    FromEntityType = "User",
    FromEntityId = userId,
    Content = "Great work on this case!"
});

await commentStore.SaveAsync(new CommentRecord {
    ForEntityType = "Docket",
    ForEntityId = docketId,
    FromEntityType = "User",
    FromEntityId = otherUserId,
    Content = "I agree!",
    ReplyToCommentId = parentCommentId
});

var commentRef = CommentRef.ForComment(commentId);
var userRef = EntityRef.ForGuid("User", userId);

await commentStore.AddReactionAsync(commentRef, userRef, CommentReactionType.Like);
var reaction = await commentStore.GetReactionAsync(commentRef, userRef);
await commentStore.RemoveReactionAsync(commentRef, userRef);
```

## Schema

Schema name: `comment` (`PostgresCommentOptions.Schema`).

- **comment.comment** — derived from `EntityRefRow`, so it includes
  `id` (uuid), `for_entity_type`, `for_entity_id` (uuid), `from_entity_type`,
  `from_entity_id` (uuid), `tenant_id`, `context`, `visibility`,
  `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`,
  `deleted_by_id`, `metadata` (jsonb), plus comment-specific `content`,
  `reply_to_comment_id` (nullable uuid), `like_count`, `dislike_count`,
  `is_edited`, and `updated_timestamp`.
- **comment.comment_reaction** — `id` (uuid), `for_entity_type` (always
  `"Comment"`), `for_entity_id` (the parent comment id), `from_entity_type`,
  `from_entity_id` (uuid), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`),
  `created_timestamp`.

## Dependencies

*(Synchronized from `Lyo.Comment.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`            | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- [`Lyo.Comment`](../Lyo.Comment/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)