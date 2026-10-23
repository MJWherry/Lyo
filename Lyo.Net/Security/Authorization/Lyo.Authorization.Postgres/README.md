# Lyo.Authorization.Postgres

One active row per subject, action, resource type, resource id, and tenant. The unique index uses NULLS NOT DISTINCT and ignores revoked rows. An expired row still occupies the
index until Save revokes it and inserts the replacement. A losing concurrent insert becomes ConflictException. The provider exception is not surfaced.

## Features

- **Health name authorization.grants** probes with CanConnectAsync. The store opens a context per call.

## Examples

### Register

```csharp
services.AddPostgresAuthorization(new PostgresAuthorizationOptions {
    ConnectionString = connectionString,
    EnableAutoMigrations = true
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authorization` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)