# Lyo.Favorite.Postgres

Entity Framework Core store for `Lyo.Favorite` on PostgreSQL. Rows live in `favorite.favorite` (`PostgresFavoriteOptions.Schema = "favorite"`), and the package includes migrations. Favorites carry **subject** / **actor** (`for_entity_*` / `from_entity_*`). `SaveAsync` blocks a second active row for the same `(tenant, ForEntity, FromEntity, context)` tuple.

`PostgresFavoriteStore` implements `IFavoriteStore` and `Lyo.Health.IHealth` (`HealthCheckName = "favorite-postgres"`). Registering the store also exposes a database liveness probe.

## Examples

### Register the store

```csharp
services.AddPostgresFavoriteStore(new PostgresFavoriteOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### appsettings.json

```json
{
  "PostgresFavorite": {
    "ConnectionString": "Host=localhost;Database=favorite;...",
    "EnableAutoMigrations": true
  }
}
```

### Bind from configuration

```csharp
services.AddPostgresFavoriteStoreFromConfiguration(configuration);
```

### A user favorites an article

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

### Add a migration

```bash
export FAVORITE_CONNECTION_STRING="Host=localhost;Database=favorite;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Favorite/Lyo.Favorite.Postgres
```

## Service registration

- `AddFavoriteDbContextFactory(Action<PostgresFavoriteOptions>)` / `AddFavoriteDbContextFactory(PostgresFavoriteOptions)` register the `IDbContextFactory<FavoriteDbContext>` only.
- `AddFavoriteDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresFavoriteOptions.SectionName)` does the same, bound from configuration. Default section: `PostgresFavorite`.
- `AddPostgresFavoriteStore(Action<PostgresFavoriteOptions>)` / `AddPostgresFavoriteStore(PostgresFavoriteOptions)` register the DbContext factory **and** the `IFavoriteStore` singleton.
- `AddPostgresFavoriteStoreFromConfiguration(IConfiguration, string sectionName = PostgresFavoriteOptions.SectionName)` registers the store by binding configuration.

## From configuration

The same registration from configuration:

## Building an EntityRef

Create refs with `Lyo.EntityReference.Models.EntityRef`, either generic or from strings:

```csharp
// Generic: uses typeof(T).FullName, keys joined with ":"
var forArticle = EntityRef.For<Article>(articleId);
var fromUser = EntityRef.For<User>(userId);

// String-based
var forEntity = EntityRef.ForGuid("Article", articleGuid);
var fromEntity = EntityRef.ForKey("User", "123");
```

## Tenant isolation

Every read and write on `PostgresFavoriteStore` takes an optional `Guid? tenantId`. `TenancyResolver` applies the policy in `PostgresFavoriteOptions.Tenancy`. When that is unset, it inherits `EntityRefOptions.Mode`. Because `tenant_id` is non-null, only `SingleTenantDefault` and `MultiTenantStrict` are valid. Construction rejects `SystemOnly`. Every query gets a `WhereTenant` filter, so one tenant's favorites cannot appear in another. The policy matrix and an `appsettings.json` snippet live in [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy).

```json
{
  "PostgresFavorite": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Database schema

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