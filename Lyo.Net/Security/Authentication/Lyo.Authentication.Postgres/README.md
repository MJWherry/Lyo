# Lyo.Authentication.Postgres

PostgreSQL persistence for `Lyo.Authentication`. Replaces the in-memory stores from the base lib with EF Core-backed implementations of `IApiTokenStore`, `IUserStore`, and `IExternalIdentityStore`.

Owns the `[user]` schema. Three tables in this lib:

- `[user].[user]` — Lyo internal users
- `[user].[token]` — Format-B opaque API tokens
- `[user].[linked_identity]` — external OIDC identity links

`__EFMigrationsHistory` lives inside the `[user]` schema.

## Registration

```csharp
services.AddLyoAuthentication();
services.AddPostgresAuthenticationStoresFromConfiguration(configuration);
```

Or each store individually:

```csharp
services.AddPostgresApiTokenStoreFromConfiguration(configuration);
services.AddPostgresUserStoreFromConfiguration(configuration);
services.AddPostgresExternalIdentityStoreFromConfiguration(configuration);
```

`appsettings.json`:

```json
{
  "PostgresUser": {
    "ConnectionString": "Host=...;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```
