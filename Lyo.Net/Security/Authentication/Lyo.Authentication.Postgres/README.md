# Lyo.Authentication.Postgres

PostgreSQL persistence for `Lyo.Authentication`. Replaces the in-memory stores from the base lib with EF Core-backed implementations of `IApiTokenStore`, `IUserStore`, and
`IExternalIdentityStore`.

Owns the `[user]` schema. Four tables in this lib:

- `[user].[user]` — Lyo internal users
- `[user].[token]` — Format-B opaque API tokens
- `[user].[linked_identity]` — external OIDC identity links
- `[user].[event]` — auth audit events (`UserEventEntity`)

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

## Tenancy

All four entities (`UserEntity`, `TokenEntity`, `LinkedIdentityEntity`, `UserEventEntity`) carry a nullable `tenant_id` (uuid) column with filtered indexes
(`ix_<table>_tenant_id`). `null` represents cross-tenant / system records; non-null values scope the row to a specific tenant. Unique constraints on
`UserEntity.Email` and `LinkedIdentityEntity (provider, subject)` include `tenant_id`, so the same email or external subject can exist per tenant.

`IUserStore`, `IApiTokenStore`, `IExternalIdentityStore`, and `PostgresAuthAuditRecorder` all accept an explicit `Guid? tenantId` and run it through
`TenancyResolver.Resolve` using the policy configured in `PostgresUserOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset):

- `SystemOnly` — every row is persisted with `tenant_id = NULL` and reads filter to system rows.
- `SingleTenantDefault` *(default)* — caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict` — caller must supply a non-empty `tenantId`; otherwise the store throws `ArgumentNullException` (the audit recorder swallows and logs).

See [`Lyo.EntityReference.Postgres` — Tenancy](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) for the full matrix.

```json
{
  "PostgresUser": {
    "ConnectionString": "Host=localhost;Database=auth;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

*(Synchronized from `Lyo.Authentication.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version | Notes                                              |
|---------------------------------------------|---------|----------------------------------------------------|
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` | `PrivateAssets=all`; consumed only at design time. |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |                                                    |

### Project references

- [`Lyo.Authentication`](../Lyo.Authentication/README.md)
- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
