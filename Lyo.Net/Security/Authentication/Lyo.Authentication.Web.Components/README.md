# Lyo.Authentication.Web.Components

Host-agnostic Razor / MudBlazor components for Lyo authentication. Ships Login, Profile, Tokens, Auth Debug, and the hostable AuthAdminPanel (projected user/token/claim/scope/identity/audit grids). Pair the admin panel with `Lyo.Api.Authentication` and an authenticated `IApiClient`. Server and Wasm host adapters implement the self-service abstractions.

## Examples

### Configuration

```json
{
  "LyoAuthWebComponents": {
    "EnablePasswordSignIn": true,
    "ShowRememberMe": true,
    "Providers": [
      { "Name": "google", "DisplayName": "Sign in with Google", "IconKey": "Icons.Material.Filled.Google" },
      { "Name": "keycloak:my-realm", "DisplayName": "Sign in with Keycloak", "IconKey": "Icons.Material.Filled.Lock" }
    ]
  }
}
```

### Register services

```csharp
services.AddLyoAuthWebComponents(configuration);
// then pick exactly one host adapter:
services.AddLyoAuthWebComponentsServer();
// or
services.AddLyoAuthWebComponentsWasm(configuration);
```

## Pages

| Route(s) | Component | Purpose |
| ------------------------------------------------ | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `/auth/login`, `/auth/login/{*ReturnUrl}` | `LoginPage` | Provider buttons + optional username/password card. |
| `/auth/debug` | `DebugPage` | Active session inspector: expiry, scopes, claims, decoded JWT. |
| `/auth/profile`, `/auth/profile/{userId:guid}` | `ProfilePage` | Current user (no segment) or arbitrary user (with `{userId}`, requires `auth.users.read` scope). Edit is shown when `IApiClient` is registered. |
| `/auth/tokens` | `TokensPage` | Mint PATs via `POST /tokens`. Lists tokens in `AuthTokenGrid` (self-service revoke; no `internal` rows) when `IApiClient` is available. |
| `/auth/admin`, `/auth/admin/users/{userId:guid}` | `AuthAdminPanel` | Administrative users / tokens / claims / scopes / identities / audit. Token revoke keeps the row; hard-delete destroys the hash. |

Login, Debug, Profile, and Tokens are wrapped in [`LyoElementRoot`](../../../Apps/Web/Lyo.Web.Components/LyoElementRoot.razor) so each rendered root gets a deterministic DOM id. Override via `ElementId`. `AuthAdminPanel` talks to `Auth/User` (and siblings) through `IApiClient`; do not map those routes anonymous.

## Abstractions (`Abstractions/`)

- `IAuthSignInLauncher.` `SignInAsync(provider, returnUrl)` and `SignOutAsync()`.
- `IAuthUserClient.` `GetMeAsync()` and `GetUserAsync(id)`. Wraps `GET /auth/me` and `GET /auth/users/{id}`.
- `IAuthSessionAccessor.` `GetCurrentAsync()` and `RefreshAsync()`. Reads the host's session store. Never exposes the refresh token itself.
- `IAuthTokenManagementClient.` Self-service `GET/POST/DELETE /tokens` used by `TokensPage` mint and revoke.
- `IAuthProviderCatalog.` Source of the provider buttons. A config-bound implementation is registered by default.
- `IAuthPasswordSignIn` (optional). Local username/password sign-in. Not registered by default. When absent, the password card on `LoginPage` is hidden.

## Configuration

`Providers[].Name` must match the canonical OIDC provider name registered on the API (the same value used in `/auth/login/{provider}`).

## Registration

The consuming Blazor host discovers the Razor pages via the standard `Router AppAssembly=...` plus `AdditionalAssemblies` mechanism. Add `typeof(Lyo.Authentication.Web.Components.Pages.LoginPage).Assembly` to your router if your app does not auto-scan referenced assemblies.

## Password card (optional)

The username/password card on `LoginPage` renders only when `IAuthPasswordSignIn` is registered and `LyoAuthWebComponentsOptions.EnablePasswordSignIn` is `true` (default). The Lyo BFF / API stack does not ship a password grant. Consumers that want this card supply their own `IAuthPasswordSignIn`, for example against a custom `/account/login` endpoint.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Authentication.Models` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Web.Components` (direct, lyo)
- `Lyo.Web.Components.Export` (direct, lyo)
- `Lyo.Web.Components.Export.Csv` (direct, lyo)
- `Lyo.Web.Components.Export.Xlsx` (direct, lyo)
- `MudBlazor` `9.3` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Web.Primitives` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Blazored.LocalStorage` `4.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)