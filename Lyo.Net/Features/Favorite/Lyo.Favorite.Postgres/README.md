# Lyo.Favorite.Postgres

PostgreSQL implementation of `Lyo.Favorite` using Entity Framework Core.
Persists favorites to the `favorite.favorite` table (schema constant:
`PostgresFavoriteOptions.Schema = "favorite"`) with migrations support.
Favorites have **For** (what is being favorited) and **From** (who favorited it)
entity references. Duplicate active rows for the same
`(tenant, ForEntity, FromEntity, context)` tuple are prevented by the
`SaveAsync` idempotency check.

`PostgresFavoriteStore` implements `IFavoriteStore` and `Lyo.Health.IHealth`
(`HealthCheckName = "favorite-postgres"`), so registering the store also exposes
a database liveness probe.

## DI extensions

Defined in `Extensions.cs` as `IServiceCollection` extensions:

- `AddFavoriteDbContextFactory(Action<PostgresFavoriteOptions>)` /
  `AddFavoriteDbContextFactory(PostgresFavoriteOptions)` — register only the
  `IDbContextFactory<FavoriteDbContext>`.
- `AddFavoriteDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresFavoriteOptions.SectionName)`
  — same, bound from configuration (default section: `PostgresFavorite`).
- `AddPostgresFavoriteStore(Action<PostgresFavoriteOptions>)` /
  `AddPostgresFavoriteStore(PostgresFavoriteOptions)` — register the DbContext
  factory **and** the `IFavoriteStore` singleton.
- `AddPostgresFavoriteStoreFromConfiguration(IConfiguration, string sectionName = PostgresFavoriteOptions.SectionName)`
  — register the store using configuration binding.

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

Uses `Lyo.EntityReference.Models.EntityRef` with generic or string-based creation:

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
    ForEntityId = articleId,
    FromEntityType = "User",
    FromEntityId = userId
});

var isFavorited = await favoriteStore.IsFavoritedAsync(
    EntityRef.ForGuid("Article", articleId),
    EntityRef.ForGuid("User", userId));

var count = await favoriteStore.GetCountForEntityAsync(
    EntityRef.ForGuid("Article", articleId));

// Batch count multiple targets in one round-trip.
var counts = await favoriteStore.GetFavoriteCountsForEntitiesAsync(
    "Article", new[] { id1, id2, id3 });
```

## Tenancy

`PostgresFavoriteStore` accepts an optional `Guid? tenantId` on every read/write
method and resolves it through `TenancyResolver` under the policy configured in
`PostgresFavoriteOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when
unset). The `tenant_id` column is non-null, so only `SingleTenantDefault` and
`MultiTenantStrict` modes are valid — `SystemOnly` is rejected at store
construction. The store applies a `WhereTenant` filter on every query so
favorites from one tenant cannot leak into another. See
[`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy)
for the full policy matrix and `appsettings.json` snippet.

```json
{
  "PostgresFavorite": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Schema

Schema name: `favorite` (`PostgresFavoriteOptions.Schema`).

- **favorite.favorite** — derived from `EntityRefRow`, so it includes
  `id` (uuid), `for_entity_type`, `for_entity_id` (uuid), `from_entity_type`,
  `from_entity_id` (uuid), `tenant_id`, `context`, `visibility`,
  `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`,
  `deleted_by_id`, and `metadata` (jsonb).

## Dependencies

*(Synchronized from `Lyo.Favorite.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Favorite`](../Lyo.Favorite/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)