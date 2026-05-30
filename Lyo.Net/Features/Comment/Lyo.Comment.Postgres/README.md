# Lyo.Comment.Postgres

PostgreSQL implementation of `Lyo.Comment` using Entity Framework Core. Persists
comments to the `comment.comment` table and reactions to
`comment.comment_reaction` (schema constant:
`PostgresCommentOptions.Schema = "comment"`) with migrations support. Comments
have **subject** / **actor** (`for_entity_*` / `from_entity_*`), optional
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

- **Subject/actor** — same relation endpoint shape as Rating and Note.
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
    SubjectEntityType = "Docket",
    SubjectEntityId = docketId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = userId.ToString(),
    Content = "Great work on this case!"
});

await commentStore.SaveAsync(new CommentRecord {
    SubjectEntityType = "Docket",
    SubjectEntityId = docketId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = otherUserId.ToString(),
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

- **comment.comment** — **`EntityRelationEntityBase`**: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id` — nullable varchar 128/256), `tenant_id`, `context`, `visibility`,
  `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`,
  `deleted_by_id`, `metadata` (jsonb), plus comment-specific `content`,
  `reply_to_comment_id` (nullable uuid), `like_count`, `dislike_count`,
  `is_edited`, and `updated_timestamp`.
- **comment.comment_reaction** — `id` (uuid); subject `for_entity_*` (always `"Comment"` + parent id); actor `from_entity_*`; `tenant_id` (nullable uuid, inherited from the parent
  comment at write time), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`),
  `created_timestamp`.

## Tenancy

`PostgresCommentStore` accepts an optional `Guid? tenantId` on every
read/write method (mirroring `IFavoriteStore`) and resolves it through
`TenancyResolver` under the policy configured in
`PostgresCommentOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when
unset). The comment `tenant_id` column is non-null, so only
`SingleTenantDefault` and `MultiTenantStrict` modes are valid — `SystemOnly` is
rejected at store construction. The store applies a `WhereTenant` filter on
every query, and reactions inherit the parent comment's `TenantId` on insert
so the sub-table stays consistent with the parent. See
[`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy)
for the full policy matrix and `appsettings.json` snippet.

```json
{
  "PostgresComment": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

*(Synchronized from `Lyo.Comment.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.Comment`](../Lyo.Comment/README.md)
- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)