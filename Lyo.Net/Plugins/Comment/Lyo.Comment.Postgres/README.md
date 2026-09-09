# Lyo.Comment.Postgres

Entity Framework Core store for `Lyo.Comment` on PostgreSQL. Comments live in `comment.comment` and reactions in `comment.comment_reaction` (`PostgresCommentOptions.Schema = "comment"`), and the package includes migrations. Comments carry **subject** / **actor** (`for_entity_*` / `from_entity_*`), optional `ReplyToCommentId` (parent comment), and cached `LikeCount` / `DislikeCount` counters.

`PostgresCommentStore` implements `ICommentStore` and `Lyo.Health.IHealth` (`HealthCheckName = "comment-postgres"`). Registering the store also registers a liveness probe.

## Features

- **Subject/actor.** Same relation endpoint shape as Rating and Note.
- **Reply threads.** `ReplyToCommentId` points to the parent comment. `GetRepliesAsync(parentId)` returns direct replies. `DeleteAsync(id, deleteReplies: true)` walks the descendant tree and soft-deletes every nested reply (plus their reactions).
- **Reactions (like/dislike).** Tracked per user via the `comment_reaction` table. Exactly one reaction per user per comment. Flipping `Like` ↔ `Dislike` mutates the existing row and adjusts the cached counters on the parent comment.
- **IsEdited.** Automatically set to `true` by `SaveAsync` whenever an existing row is updated.

## Examples

### Register the store

```csharp
services.AddPostgresCommentStore(new PostgresCommentOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### appsettings.json

```json
{
  "PostgresComment": {
    "ConnectionString": "Host=localhost;Database=comment;...",
    "EnableAutoMigrations": true
  }
}
```

### Bind from configuration

```csharp
services.AddPostgresCommentStoreFromConfiguration(configuration);
```

### Post, reply, and react

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

### Add a migration

```bash
export COMMENT_CONNECTION_STRING="Host=localhost;Database=comment;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Comment/Lyo.Comment.Postgres
```

## Service registration

- `AddCommentDbContextFactory(Action<PostgresCommentOptions>)` / `AddCommentDbContextFactory(PostgresCommentOptions)` register the `IDbContextFactory<CommentDbContext>` only.
- `AddCommentDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresCommentOptions.SectionName)` does the same, bound from configuration. Default section: `PostgresComment`.
- `AddPostgresCommentStore(Action<PostgresCommentOptions>)` / `AddPostgresCommentStore(PostgresCommentOptions)` register the DbContext factory **and** the `ICommentStore` singleton.
- `AddPostgresCommentStoreFromConfiguration(IConfiguration, string sectionName = PostgresCommentOptions.SectionName)` registers the store by binding configuration.

## From configuration

The same registration from configuration:

## Database schema

- **comment.comment.** `EntityRelationEntityBase`: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, `metadata` (jsonb), plus comment-specific `content`, `reply_to_comment_id` (nullable uuid), `like_count`, `dislike_count`, `is_edited`, and `updated_timestamp`.
- **comment.comment_reaction.** `id` (uuid); subject `for_entity_*` (always `"Comment"` + parent id); actor `from_entity_*`; `tenant_id` (nullable uuid, inherited from the parent comment at write time), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`), `created_timestamp`.

## Tenant isolation

Every read and write on `PostgresCommentStore` takes an optional `Guid? tenantId` (same shape as `IFavoriteStore`). `TenancyResolver` applies the policy in `PostgresCommentOptions.Tenancy`. When that is unset, it inherits `EntityRefOptions.Mode`. The comment `tenant_id` column is non-null, so only `SingleTenantDefault` and `MultiTenantStrict` are valid. Construction rejects `SystemOnly`. Every query gets a `WhereTenant` filter. Reactions inherit the parent comment's `TenantId` on insert so the sub-table stays aligned with the parent. The policy matrix and an `appsettings.json` snippet live in [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy).

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

- `Lyo.Comment` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)