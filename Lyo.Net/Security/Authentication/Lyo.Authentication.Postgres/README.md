# Lyo.Authentication.Postgres

PostgreSQL persistence for `Lyo.Authentication`. Replaces the in-memory stores from the base lib with EF Core-backed implementations of `IApiTokenStore`, `IUserStore`, and
`IExternalIdentityStore`.

Owns the `[user]` schema. Four tables in this lib:

- `[user].[user]` — Lyo internal users - `[user].[token]` — Format-B opaque API tokens - `[user].[linked_identity]` — external OIDC identity links - `[user].[event]` — auth audit
  events (`UserEventEntity`)

`__EFMigrationsHistory` lives inside the `[user]` schema.

## Examples

### Register services

```csharp
services.AddLyoAuthentication();
services.AddPostgresAuthenticationStoresFromConfiguration(configuration);
```

### Register services (2)

```csharp
services.AddPostgresApiTokenStoreFromConfiguration(configuration);
services.AddPostgresUserStoreFromConfiguration(configuration);
services.AddPostgresExternalIdentityStoreFromConfiguration(configuration);
```

### Register services (3)

```json
{
  "PostgresUser": {
    "ConnectionString": "Host=...;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

## Registration

Or each store individually: `appsettings.json`:

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

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)