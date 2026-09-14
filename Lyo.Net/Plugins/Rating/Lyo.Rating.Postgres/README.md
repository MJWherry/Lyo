# Lyo.Rating.Postgres

Entity Framework Core store for `Lyo.Rating` on PostgreSQL. Ratings live in `rating.rating` and reactions in `rating.rating_reaction` (`PostgresRatingOptions.Schema = "rating"`), and the package includes migrations. Ratings carry **subject** / **actor** (`for_entity_*` / `from_entity_*`) and an optional rating-axis Subject field (for example `"scary"`, `"action"`). One user may leave several ratings on the same entity, one per subject. `subject = null` is the general rating. `Value` is optional, so a review can be text-only. Reactions (`Like` / `Dislike`) live in a sibling table. Counts are cached on the parent rating.

`PostgresRatingStore` implements `IRatingStore` and `Lyo.Health.IHealth` (`HealthCheckName = "rating-postgres"`). Registering the store also registers a liveness probe.

## Examples

### Register the store

```csharp
services.AddPostgresRatingStore(new PostgresRatingOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### appsettings.json

```json
{
  "PostgresRating": {
    "ConnectionString": "Host=localhost;Database=rating;...",
    "EnableAutoMigrations": true
  }
}
```

### Bind from configuration

```csharp
services.AddPostgresRatingStoreFromConfiguration(configuration);
```

### Rate a movie, general and by subject

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

### Like or dislike a rating

```csharp
var ratingRef = RatingRef.ForRating(ratingId);
var actor = EntityRef.ForGuid("User", userId);

await ratingStore.AddReactionAsync(ratingRef, actor, RatingReactionType.Like);
await ratingStore.RemoveReactionAsync(ratingRef, actor);
```

### Add a migration

```bash
export RATING_CONNECTION_STRING="Host=localhost;Database=rating;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Rating/Lyo.Rating.Postgres
```

## Service registration

- `AddRatingDbContextFactory(Action<PostgresRatingOptions>)` / `AddRatingDbContextFactory(PostgresRatingOptions)` register the `IDbContextFactory<RatingDbContext>` only.
- `AddRatingDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresRatingOptions.SectionName)` does the same, bound from configuration. Default section: `PostgresRating`.
- `AddPostgresRatingStore(Action<PostgresRatingOptions>)` / `AddPostgresRatingStore(PostgresRatingOptions)` register the DbContext factory **and** the `IRatingStore` singleton.
- `AddPostgresRatingStoreFromConfiguration(IConfiguration, string sectionName = PostgresRatingOptions.SectionName)` registers the store by binding configuration.

## From configuration

The same registration from configuration:

## Building an EntityRef

Create refs with `Lyo.EntityReference.Models.EntityRef`, either generic or from strings:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);
var composite = EntityRef.For<Order>("ord-1", "line-2");

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Database schema

- **rating.rating.** `EntityRelationEntityBase`: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, `metadata` (jsonb), plus rating-specific `subject` (nullable), `title` (nullable), `value` (nullable `decimal`), `message`, `like_count`, `dislike_count`, and `updated_timestamp`.
- **rating.rating_reaction.** `id` (uuid); subject `for_entity_*` (always `"Rating"` + parent id); actor `from_entity_*`; `tenant_id` (nullable uuid, inherited from the parent rating at write time), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`), `created_timestamp`.

## Tenant isolation

Every read and write on `PostgresRatingStore` takes an optional `Guid? tenantId`. `TenancyResolver` applies the policy in `PostgresRatingOptions.Tenancy`. When that is unset, it inherits `EntityRefOptions.Mode`. The rating `tenant_id` column is non-null, so only `SingleTenantDefault` and `MultiTenantStrict` are valid. Construction rejects `SystemOnly`. On insert, reactions inherit the parent rating's `TenantId` so the reaction sub-table stays aligned with its parent. The policy matrix and an `appsettings.json` snippet live in [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy).

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

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Rating` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)