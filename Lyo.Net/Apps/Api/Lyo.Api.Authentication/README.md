# Lyo.Api.Authentication

Administrative HTTP endpoints for Lyo Authentication. Maps QueryProject surfaces over `[user]` tables. Hosts supply `EndpointAuth` (default `RequireAuthorization()`). Self-service mint/list/revoke stays on `/tokens` and `/auth/me`.

Do not map these routes `Anonymous()` on a public host: they QueryProject every user and token, including `SecretHash` denial on Token projections.

## Examples

### Set up the host

```csharp
services.AddPostgresAuthenticationStoresFromConfiguration(configuration);
services.AddLyoApiAuthentication();
services.AddSingleton<AuthenticationLyoMapper>();
services.AddScoped<ILyoMapper>(sp => new CompositeLyoMapper(
    sp.GetRequiredService<AuthenticationLyoMapper>(),
    fallbackMapper));

var app = builder.Build();
app.BuildAuthenticationApi(); // RequireAuthorization on every surface
// or AuthenticationApiOptions.WithAuth(EndpointAuth.RequireAuthorization("AuthAdmin"))
```

## Authorization matrix

| Surface | Options property | Endpoints |
| --------------- | -------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| User | `UserAuth` | Query / Get / Patch / Export. No Create (OIDC provisions). |
| Token | `TokenAuth` | Query / Get / Patch / Delete / DeleteBulk. `SecretHash` is not selectable. Delete hard-removes the row. |
| Claim | `ClaimAuth` | Default CRUD. Reserved JWT names (`iss`, `sub`, `scope`, `lyo:*`, …) are rejected. |
| Scope | `ScopeAuth` | Default CRUD. Unique `(UserId, Name)`. Source of truth for JWT `scope`; provider/link scopes are not copied onto tokens. |
| Linked identity | `LinkedIdentityAuth` | Query / Get (read-only) |
| Event | `EventAuth` | Query / Get over `[user].[event]` |

Defaults are `EndpointAuth.RequireAuthorization()`. Prefer `AuthenticationApiOptions.WithAuth(auth)` when every surface shares one policy. Revoke is Patch `RevokedTimestamp`; hard-delete destroys the hash and emits `AuthAuditEventKind.TokenDeleted`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Authentication` (direct, lyo)
- `Lyo.Authentication.Models` (direct, lyo)
- `Lyo.Authentication.Postgres` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)