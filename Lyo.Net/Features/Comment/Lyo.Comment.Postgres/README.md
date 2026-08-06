# Lyo.Comment.Postgres

PostgreSQL implementation of `Lyo.Comment` using Entity Framework Core. Persists comments to the `comment.comment` table and reactions to `comment.comment_reaction` (schema constant: `PostgresCommentOptions.Schema = "comment"`) with migrations support. Comments have **subject** / **actor** (`for_entity_*` / `from_entity_*`), optional **ReplyToCommentId** (parent comment), and cached `LikeCount` / `DislikeCount` counters.

`PostgresCommentStore` implements `ICommentStore` and `Lyo.Health.IHealth` (`HealthCheckName = "comment-postgres"`), so registering the store also wires up a liveness probe.

## Features

- **Subject/actor** — same relation endpoint shape as Rating and Note.
- **Reply threads** — `ReplyToCommentId` points to the parent comment; `GetRepliesAsync(parentId)` returns direct replies; `DeleteAsync(id, deleteReplies: true)` walks the descendant tree and soft-deletes every nested reply (plus their reactions).
- **Reactions (like/dislike)** — tracked per user via the `comment_reaction` table; exactly one reaction per user per comment. Flipping `Like` ↔ `Dislike` mutates the existing row and adjusts the cached counters on the parent comment.
- **IsEdited** — automatically set to `true` by `SaveAsync` whenever an existing row is updated.

## Examples

### Usage

```csharp
services.AddPostgresCommentStore(new PostgresCommentOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Usage (2)

```json
{
  "PostgresComment": {
    "ConnectionString": "Host=localhost;Database=comment;...",
    "EnableAutoMigrations": true
  }
}
```

### Usage (3)

```csharp
services.AddPostgresCommentStoreFromConfiguration(configuration);
```

### Example

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

### Migrations

```bash
export COMMENT_CONNECTION_STRING="Host=localhost;Database=comment;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Comment/Lyo.Comment.Postgres
```

## DI extensions

- `AddCommentDbContextFactory(Action<PostgresCommentOptions>)` / `AddCommentDbContextFactory(PostgresCommentOptions)` — register only the `IDbContextFactory<CommentDbContext>`.
- `AddCommentDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresCommentOptions.SectionName)` — same, bound from configuration (default section: `PostgresComment`).
- `AddPostgresCommentStore(Action<PostgresCommentOptions>)` / `AddPostgresCommentStore(PostgresCommentOptions)` — register the DbContext factory **and** the `ICommentStore` singleton.
- `AddPostgresCommentStoreFromConfiguration(IConfiguration, string sectionName = PostgresCommentOptions.SectionName)` — register the store using configuration binding.

## Usage

Or with configuration:

## Schema

- **comment.comment** — **`EntityRelationEntityBase`**: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id` — nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, `metadata` (jsonb), plus comment-specific `content`, `reply_to_comment_id` (nullable uuid), `like_count`, `dislike_count`, `is_edited`, and `updated_timestamp`.
- **comment.comment_reaction** — `id` (uuid); subject `for_entity_*` (always `"Comment"` + parent id); actor `from_entity_*`; `tenant_id` (nullable uuid, inherited from the parent comment at write time), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`), `created_timestamp`.

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

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Comment` — (direct, lyo)
- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)