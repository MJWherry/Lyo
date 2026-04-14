# Lyo.Rating.Postgres

PostgreSQL implementation of Lyo.Rating using Entity Framework Core. Persists ratings to `rating.rating` table with migrations support. Ratings have **For** (what is rated), **From
** (who rated), and optional **Subject** (e.g. "scary", "action"). Multiple ratings per entity per user are allowed (one per subject). Value is optional (review without numeric
score). Ratings can be liked/disliked via reactions.

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
services.AddPostgresRatingStore(configuration);
```

## Migrations

```bash
export RATING_CONNECTION_STRING="Host=localhost;Database=rating;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Rating/Lyo.Rating.Postgres
```

## Entity Reference

Uses `Lyo.Common.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forDocket = EntityRef.For<Docket>(docketId);
var fromUser = EntityRef.For<User>(123);
var composite = EntityRef.For<Order>("ord-1", "line-2");

// String-based
var forEntity = EntityRef.ForGuid("Docket", docketGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Example: User rates a movie (general + subject-specific)

```csharp
// General rating with optional title
await ratingStore.SaveAsync(new RatingRecord {
    ForEntityType = "Movie",
    ForEntityId = movieId.ToString(),
    FromEntityType = "User",
    FromEntityId = "123",
    Subject = null,
    Title = "A must-see!",
    Value = 4,
    Message = "Great film!"
});

// Subject-specific: "scary" aspect
await ratingStore.SaveAsync(new RatingRecord {
    ForEntityType = "Movie",
    ForEntityId = movieId.ToString(),
    FromEntityType = "User",
    FromEntityId = "123",
    Subject = "scary",
    Value = 5,
    Message = "Very tense!"
});

// Review without numeric score
await ratingStore.SaveAsync(new RatingRecord {
    ForEntityType = "Movie",
    ForEntityId = movieId.ToString(),
    FromEntityType = "User",
    FromEntityId = "123",
    Subject = "action",
    Value = null,
    Message = "Non-stop action, loved it."
});
```

## Example: Like/dislike a rating

```csharp
var ratingRef = RatingRef.ForRating(ratingId);
await ratingStore.AddReactionAsync(ratingRef, EntityRef.ForKey("User", userId), RatingReactionType.Like);
await ratingStore.RemoveReactionAsync(ratingRef, EntityRef.ForKey("User", userId));
```

## Schema

- **rating.rating** – `id`, `for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, `subject` (nullable), `title` (nullable), `value` (nullable), `message`,
  `like_count`, `dislike_count`, `created_timestamp`, `updated_timestamp`
- **rating.rating_reaction** – `id`, `for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, `reaction_type`, `created_timestamp`
- Unique on (for_entity_type, for_entity_id, from_entity_type, from_entity_id, subject)

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Rating.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.EntityFrameworkCore.Design` | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Health`
- `Lyo.Postgres`
- `Lyo.Rating`

## Public API (generated)

Top-level `public` types in `*.cs` (*10*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `InitialCreate`
- `PostgresRatingOptions`
- `PostgresRatingStore`
- `RatingDbContext`
- `RatingDbContextFactory`
- `RatingEntity`
- `RatingEntityConfiguration`
- `RatingReactionEntity`
- `RatingReactionEntityConfiguration`

<!-- LYO_README_SYNC:END -->

