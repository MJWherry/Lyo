# Lyo.Authentication.Web.Components.Server

Blazor Server host adapter for [`Lyo.Authentication.Web.Components`](../Lyo.Authentication.Web.Components/README.md). Plugs the shared login / debug / profile pages into the BFF-cookie auth runtime in [`Lyo.Authentication.Client`](../Lyo.Authentication.Client/README.md).

## Examples

### Register services

```csharp
builder.Services.AddLyoAuthClient(builder.Configuration);
builder.Services.AddLyoAuthBlazorStateProvider();
builder.Services.AddLyoAuthWebComponents(builder.Configuration);
builder.Services.AddLyoAuthWebComponentsServer();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapLyoAuthSignIn();
app.MapLyoAuthHandoffCallback();
app.MapLyoAuthSignOut();
```

### Register services (2)

```razor
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(Lyo.Authentication.Web.Components.Pages.LoginPage).Assembly }">
```

## What's inside

| Service | Role |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ServerAuthSignInLauncher` | Sign-in 302s through the consumer's `/auth/sign-in/{provider}` endpoint; sign-out submits a POST to `/auth/sign-out` via a dynamically constructed form (CSRF-safe). |
| `ServerAuthSessionAccessor` | Unseals the HttpOnly session cookie via `IDataProtectionProvider`, reads the active `LyoAuthSession` from `LyoAuthSessionStore`, and rotates it through `/auth/refresh` for the debug page's "Refresh token" button. |
| `ServerAuthUserClient` | Typed `HttpClient` with `AddLyoAuthHandler()` so calls to `GET /auth/me` and `GET /auth/users/{id}` automatically carry the bearer and auto-refresh on 401. |

## Registration

Make sure the consuming Blazor Server router picks up this package's pages — either via auto-scan or by adding the assembly to `AdditionalAssemblies`:

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication.Client` — (direct, lyo)
- `Lyo.Authentication.Web.Components` — (direct, lyo)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Lyo.Web.Components` — (transitive, lyo)
- `Blazored.LocalStorage` `4.5.0` — (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.AspNetCore.Components.Authorization` `10.0.5` — (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `MudBlazor` `9.3` — (transitive, third-party)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)