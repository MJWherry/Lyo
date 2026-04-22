# Lyo.Favorite.Postgres

PostgreSQL implementation of Lyo.Favorite using Entity Framework Core. Persists favorites to `favorite.favorite` table with migrations support. Favorites have **For** (what is
being favorited) and **From** (who favorited it) entity references. A unique constraint prevents duplicate favorites for the same pair.

## Usage

```csharp
services.AddPostgresFavoriteStore(new PostgresFavoriteOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

Or with configuration:

```json
{
  "PostgresFavorite": {
    "ConnectionString": "Host=localhost;Database=favorite;...",
    "EnableAutoMigrations": true
  }
}
```

```csharp
services.AddPostgresFavoriteStoreFromConfiguration(configuration);
```

## Migrations

```bash
export FAVORITE_CONNECTION_STRING="Host=localhost;Database=favorite;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Favorite/Lyo.Favorite.Postgres
```

## Entity Reference

Uses `Lyo.Common.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forArticle = EntityRef.For<Article>(articleId);
var fromUser = EntityRef.For<User>(userId);

// String-based
var forEntity = EntityRef.ForGuid("Article", articleGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Example: User favorites an article

```csharp
await favoriteStore.SaveAsync(new FavoriteRecord {
    ForEntityType = "Article",
    ForEntityId = articleId.ToString(),
    FromEntityType = "User",
    FromEntityId = userId.ToString()
});

var isFavorited = await favoriteStore.IsFavoritedAsync(
    EntityRef.ForGuid("Article", articleId),
    EntityRef.ForKey("User", userId.ToString()));

var count = await favoriteStore.GetCountForEntityAsync(
    EntityRef.ForGuid("Article", articleId));
```

## Schema

- **favorite.favorite** – `id` (uuid), `for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, `created_timestamp`
- Unique index on `(for_entity_type, for_entity_id, from_entity_type, from_entity_id)` prevents duplicate favorites.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Favorite.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`            | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Favorite`
- `Lyo.Health`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `FavoriteDbContext`
- `FavoriteDbContextFactory`
- `FavoriteEntity`
- `FavoriteEntityConfiguration`
- `InitialCreate`
- `PostgresFavoriteOptions`
- `PostgresFavoriteStore`

<!-- LYO_README_SYNC:END -->
