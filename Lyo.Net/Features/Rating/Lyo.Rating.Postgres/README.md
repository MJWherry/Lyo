# Lyo.Rating.Postgres

PostgreSQL implementation of `Lyo.Rating` using Entity Framework Core. Persists
ratings to the `rating.rating` table and reactions to `rating.rating_reaction`
(schema constant: `PostgresRatingOptions.Schema = "rating"`) with migrations
support. Ratings have **For** (what is rated), **From** (who rated), and an
optional **Subject** (e.g. `"scary"`, `"action"`). Multiple ratings per entity
per user are allowed — one per subject, where `subject = null` is the general
rating. `Value` is optional (a review can be text-only), and reactions
(`Like` / `Dislike`) are kept in a sibling table while their counts are cached
back onto the parent rating.

`PostgresRatingStore` implements `IRatingStore` and `Lyo.Health.IHealth`
(`HealthCheckName = "rating-postgres"`), so registering the store also wires up
a liveness probe.

## DI extensions

Defined in `Extensions.cs` as `IServiceCollection` extensions:

- `AddRatingDbContextFactory(Action<PostgresRatingOptions>)` /
  `AddRatingDbContextFactory(PostgresRatingOptions)` — register only the
  `IDbContextFactory<RatingDbContext>`.
- `AddRatingDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresRatingOptions.SectionName)`
  — same, bound from configuration (default section: `PostgresRating`).
- `AddPostgresRatingStore(Action<PostgresRatingOptions>)` /
  `AddPostgresRatingStore(PostgresRatingOptions)` — register the DbContext
  factory **and** the `IRatingStore` singleton.
- `AddPostgresRatingStoreFromConfiguration(IConfiguration, string sectionName = PostgresRatingOptions.SectionName)`
  — register the store using configuration binding.

## Usage

```csharp
services.AddPostgresRatingStore(new PostgresRatingOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

Or with configuration:

```json
{
  "PostgresRating": {
    "ConnectionString": "Host=localhost;Database=rating;...",
    "EnableAutoMigrations": true
  }
}
```

```csharp
services.AddPostgresRatingStoreFromConfiguration(configuration);
```

## Migrations

```bash
export RATING_CONNECTION_STRING="Host=localhost;Database=rating;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Rating/Lyo.Rating.Postgres
```

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

## Example: a user rates a movie (general + subject-specific)

```csharp
await ratingStore.SaveAsync(new RatingRecord {
    ForEntityType = "Movie",
    ForEntityId = movieId,
    FromEntityType = "User",
    FromEntityId = userId,
    Subject = null,
    Title = "A must-see!",
    Value = 4,
    Message = "Great film!"
});

await ratingStore.SaveAsync(new RatingRecord {
    ForEntityType = "Movie",
    ForEntityId = movieId,
    FromEntityType = "User",
    FromEntityId = userId,
    Subject = "scary",
    Value = 5,
    Message = "Very tense!"
});

await ratingStore.SaveAsync(new RatingRecord {
    ForEntityType = "Movie",
    ForEntityId = movieId,
    FromEntityType = "User",
    FromEntityId = userId,
    Subject = "action",
    Value = null,
    Message = "Non-stop action, loved it."
});
```

## Example: like / dislike a rating

```csharp
var ratingRef = RatingRef.ForRating(ratingId);
var actor = EntityRef.ForGuid("User", userId);

await ratingStore.AddReactionAsync(ratingRef, actor, RatingReactionType.Like);
await ratingStore.RemoveReactionAsync(ratingRef, actor);
```

## Schema

Schema name: `rating` (`PostgresRatingOptions.Schema`).

- **rating.rating** — derived from `EntityRefRow`, so it includes
  `id` (uuid), `for_entity_type`, `for_entity_id` (uuid), `from_entity_type`,
  `from_entity_id` (uuid), `tenant_id`, `context`, `visibility`,
  `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`,
  `deleted_by_id`, `metadata` (jsonb), plus rating-specific `subject` (nullable),
  `title` (nullable), `value` (nullable `decimal`), `message`,
  `like_count`, `dislike_count`, and `updated_timestamp`.
- **rating.rating_reaction** — `id` (uuid), `for_entity_type` (always
  `"Rating"`), `for_entity_id` (the parent rating id), `from_entity_type`,
  `from_entity_id` (uuid), `reaction_type` (`int`; `0 = Like`, `1 = Dislike`),
  `created_timestamp`.

## Dependencies

*(Synchronized from `Lyo.Rating.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`            | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.Rating`](../Lyo.Rating/README.md)