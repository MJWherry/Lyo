# Lyo.Authentication.Web.Components

Host-agnostic Razor / MudBlazor components for Lyo authentication. Ships the **Login**, **Auth Debug**, and **Profile** pages plus the abstractions that the host adapter (`Lyo.Authentication.Web.Components.Server` or `Lyo.Authentication.Web.Components.Wasm`) plugs into.

## Pages

| Route(s) | Component | Purpose |
|---|---|---|
| `/auth/login`, `/auth/login/{*ReturnUrl}` | `LoginPage` | Provider buttons + optional username/password card. |
| `/auth/debug` | `DebugPage` | Active session inspector: expiry, scopes, claims, decoded JWT. |
| `/auth/profile`, `/auth/profile/{userId:guid}` | `ProfilePage` | Current user (no segment) or arbitrary user (with `{userId}`, requires `auth.users.read` scope). |

All three are wrapped in [`LyoElementRoot`](../../../Integration/Web/Lyo.Web.Components/LyoElementRoot.razor) so each rendered root gets a deterministic DOM id (override via `ElementId`).

## Abstractions (`Abstractions/`)

Host adapters MUST register these. The shared library only registers `IAuthProviderCatalog`.

- `IAuthSignInLauncher` — `SignInAsync(provider, returnUrl)` + `SignOutAsync()`.
- `IAuthUserClient` — `GetMeAsync()` + `GetUserAsync(id)`. Wraps `GET /auth/me` and `GET /auth/users/{id}`.
- `IAuthSessionAccessor` — `GetCurrentAsync()` + `RefreshAsync()`. Reads the host's session store; never exposes the refresh token itself.
- `IAuthProviderCatalog` — Source of the provider buttons. Default config-bound impl is registered.
- `IAuthPasswordSignIn` (**optional**) — local username/password sign-in. Not registered by default. When absent, the password card on `LoginPage` is hidden.

## Configuration

```json
{
  "LyoAuthWebComponents": {
    "EnablePasswordSignIn": true,
    "ShowRememberMe": true,
    "Providers": [
      { "Name": "google",            "DisplayName": "Sign in with Google",   "IconKey": "Icons.Material.Filled.Google" },
      { "Name": "keycloak:my-realm", "DisplayName": "Sign in with Keycloak", "IconKey": "Icons.Material.Filled.Lock"   }
    ]
  }
}
```

`Providers[].Name` must match the canonical OIDC provider name registered on the API (the same value used in `/auth/login/{provider}`).

## Registration

```csharp
services.AddLyoAuthWebComponents(configuration);
// then pick exactly one host adapter:
services.AddLyoAuthWebComponentsServer();
// or
services.AddLyoAuthWebComponentsWasm(configuration);
```

The Razor pages are automatically discovered by the consuming Blazor host via the standard `Router AppAssembly=...` plus `AdditionalAssemblies` mechanism — add `typeof(Lyo.Authentication.Web.Components.Pages.LoginPage).Assembly` to your router if your app does not auto-scan referenced assemblies.

## Password card (optional)

The username/password card on `LoginPage` is purely additive: it renders only when **both**

1. `IAuthPasswordSignIn` is registered, AND
2. `LyoAuthWebComponentsOptions.EnablePasswordSignIn` is `true` (default).

The Lyo BFF / API stack does **not** currently ship a password grant. Consumers that want this card supply their own `IAuthPasswordSignIn` (e.g. against a custom `/account/login` endpoint).
