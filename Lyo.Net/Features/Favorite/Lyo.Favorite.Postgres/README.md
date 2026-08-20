# Lyo.Favorite.Postgres

PostgreSQL implementation of `Lyo.Favorite` using Entity Framework Core. Persists favorites to the `favorite.favorite` table (`PostgresFavoriteOptions.Schema = "favorite"`) and ships migrations. Favorites have **subject** / **actor** (`for_entity_*` / `from_entity_*`). Duplicate active rows for the same `(tenant, ForEntity, FromEntity, context)` tuple are blocked by the `SaveAsync` idempotency check.

`PostgresFavoriteStore` implements `IFavoriteStore` and `Lyo.Health.IHealth` (`HealthCheckName = "favorite-postgres"`). Registering the store also exposes a database liveness probe.

## Examples

### Usage

```csharp
services.AddPostgresFavoriteStore(new PostgresFavoriteOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Usage (2)

```json
{
  "PostgresFavorite": {
    "ConnectionString": "Host=localhost;Database=favorite;...",
    "EnableAutoMigrations": true
  }
}
```

### Usage (3)

```csharp
services.AddPostgresFavoriteStoreFromConfiguration(configuration);
```

### Example: user favorites an article

```csharp
await favoriteStore.SaveAsync(new FavoriteRecord {
    SubjectEntityType = "Article",
    SubjectEntityId = articleId.ToString(),
    ActorEntityType = "User",
    ActorEntityId = userId.ToString()
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

### Migrations

```bash
export FAVORITE_CONNECTION_STRING="Host=localhost;Database=favorite;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Features/Favorite/Lyo.Favorite.Postgres
```

## DI extensions

- `AddFavoriteDbContextFactory(Action<PostgresFavoriteOptions>)` / `AddFavoriteDbContextFactory(PostgresFavoriteOptions)` register only the `IDbContextFactory<FavoriteDbContext>`.
- `AddFavoriteDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresFavoriteOptions.SectionName)` same, bound from configuration (default section: `PostgresFavorite`).
- `AddPostgresFavoriteStore(Action<PostgresFavoriteOptions>)` / `AddPostgresFavoriteStore(PostgresFavoriteOptions)` register the DbContext factory **and** the `IFavoriteStore` singleton.
- `AddPostgresFavoriteStoreFromConfiguration(IConfiguration, string sectionName = PostgresFavoriteOptions.SectionName)` register the store using configuration binding.

## Usage

Or with configuration:

## Entity reference

Uses `Lyo.EntityReference.Models.EntityRef` with generic or string-based creation:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forArticle = EntityRef.For<Article>(articleId);
var fromUser = EntityRef.For<User>(userId);

// String-based
var forEntity = EntityRef.ForGuid("Article", articleGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Tenancy

`PostgresFavoriteStore` accepts an optional `Guid? tenantId` on every read/write
method and resolves it through `TenancyResolver` under the policy configured in
`PostgresFavoriteOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when
unset). The `tenant_id` column is non-null, so only `SingleTenantDefault` and
`MultiTenantStrict` modes are valid. `SystemOnly` is rejected at store
construction. The store applies a `WhereTenant` filter on every query so
favorites from one tenant cannot leak into another. See
[`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy)
for the policy matrix and `appsettings.json` snippet.

```json
{
  "PostgresFavorite": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Schema

- **favorite.favorite.** `EntityRelationEntityBase`: `id` (uuid), subject/actor columns (`for_entity_type`, `for_entity_id`, `from_entity_type`, `from_entity_id`, nullable varchar 128/256), `tenant_id`, `context`, `visibility`, `created_at`, `expires_at`, `deleted_at`, `deleted_by_type`, `deleted_by_id`, and `metadata` (jsonb).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Favorite` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)