# Lyo.Rating.Postgres

PostgreSQL implementation of `Lyo.Rating` using Entity Framework Core. Persists
ratings to the `rating.rating` table and reactions to `rating.rating_reaction`
(schema constant: `PostgresRatingOptions.Schema = "rating"`) with migrations
support. Ratings have **subject** / **actor** (`for_entity_*` / `from_entity_*`) and an
optional rating-axis **Subject** field (e.g. `"scary"`, `"action"`). Multiple ratings per entity
per user are allowed — one per subject, where `subject = null` is the general
rating. `Value` is optional (a review can be text-only), and reactions
(`Like` / `Dislike`) are kept in a sibling table while their counts are cached
back onto the parent rating.

`PostgresRatingStore` implements `IRatingStore` and `Lyo.Health.IHealth`
(`HealthCheckName = "rating-postgres"`), so registering the store also wires up
a liveness probe.

## Examples

### Usage

```csharp
services.AddPostgresRatingStore(new PostgresRatingOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Usage (2)

```json
{
  "PostgresRating": {
    "ConnectionString": "Host=localhost;Database=rating;...",
    "EnableAutoMigrations": true
  }
}
```

### Usage (3)

```csharp
services.AddPostgresRatingStoreFromConfiguration(configuration);
```

### Example: a user rates a movie (general + subject-specific)

```csharp
await ratingStore.SaveAsync(new RatingRecord {
    SubjectEntityType = "Movie",
    SubjectEntityId = movieId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = userId.ToString(),
    Subject = null,
    Title = "A must-see!",
    Value = 4,
    Message = "Great film!"
});

await ratingStore.SaveAsync(new RatingRecord {
    SubjectEntityType = "Movie",
    SubjectEntityId = movieId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = userId.ToString(),
    Subject = "scary",
    Value = 5,
    Message = "Very tense!"
});

await ratingStore.SaveAsync(new RatingRecord {
    SubjectEntityType = "Movie",
    SubjectEntityId = movieId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = userId.ToString(),
    Subject = "action",
    Value = null,
    Message = "Non-stop action, loved it."
});
```

### Example: like / dislike a rating

```csharp
var ratingRef = RatingRef.ForRating(ratingId);
var actor = EntityRef.ForGuid("User", userId);

await ratingStore.AddReactionAsync(ratingRef, actor, RatingReactionType.Like);
await ratingStore.RemoveReactionAsync(ratingRef, actor);
```

### Migrations

```bash
export RATING_CONNECTION_STRING="Host=localhost;Database=rating;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Rating/Lyo.Rating.Postgres
```

## DI extensions

- `AddRatingDbContextFactory(Action<PostgresRatingOptions>)` / `AddRatingDbContextFactory(PostgresRatingOptions)` — register only the `IDbContextFactory<RatingDbContext>`.
- `AddRatingDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresRatingOptions.SectionName)` — same, bound from configuration (default section: `PostgresRating`).
- `AddPostgresRatingStore(Action<PostgresRatingOptions>)` / `AddPostgresRatingStore(PostgresRatingOptions)` — register the DbContext factory **and** the `IRatingStore` singleton.
- `AddPostgresRatingStoreFromConfiguration(IConfiguration, string sectionName = PostgresRatingOptions.SectionName)` — register the store using configuration binding.

## Usage

Or with configuration:

## Entity Reference

Uses `Lyo.EntityReference.Models.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);
var composite = EntityRef.For<Order>("ord-1", "line-2");

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Schema

- **rating.rating** — **`EntityRelationEntityBase`**: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id` — nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, `metadata` (jsonb), plus rating-specific `subject` (nullable), `title` (nullable), `value` (nullable `decimal`), `message`, `like_count`, `dislike_count`, and `updated_timestamp`.
- **rating.rating_reaction** — `id` (uuid); subject `for_entity_*` (always `"Rating"` + parent id); actor `from_entity_*`; `tenant_id` (nullable uuid, inherited from the parent rating at write time), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`), `created_timestamp`.

## Tenancy

`PostgresRatingStore` accepts an optional `Guid? tenantId` on every read/write
method and resolves it through `TenancyResolver` under the policy configured in
`PostgresRatingOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when
unset). The rating `tenant_id` column is non-null, so only `SingleTenantDefault`
and `MultiTenantStrict` modes are valid — `SystemOnly` is rejected at store
construction. Reactions inherit the parent rating's `TenantId` on insert so the
reaction sub-table stays consistent with its parent. See
[`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy)
for the full policy matrix and `appsettings.json` snippet.

```json
{
  "PostgresRating": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Lyo.Rating` — (direct, lyo)
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