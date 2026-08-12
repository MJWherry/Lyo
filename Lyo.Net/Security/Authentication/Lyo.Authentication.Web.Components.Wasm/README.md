# Lyo.Authentication.Web.Components.Wasm

Blazor WebAssembly host adapter for [`Lyo.Authentication.Web.Components`](../Lyo.Authentication.Web.Components/README.md). Implements the same login / debug / profile pages over a **pure-browser** auth flow — no consumer-side server, no HttpOnly cookie. Tokens live in `Blazored.LocalStorage` and the browser talks directly to the Lyo API.

## Examples

### Quick start

```csharp
builder.Services.AddLyoAuthWebComponents(builder.Configuration);
builder.Services.AddLyoAuthWebComponentsWasm(builder.Configuration);
```

### Quick start (2)

```json
{
  "LyoAuthWebComponents": {
    "Providers": [
      { "Name": "google", "DisplayName": "Sign in with Google", "IconKey": "Icons.Material.Filled.Google" },
      { "Name": "keycloak:my-realm", "DisplayName": "Sign in with Keycloak", "IconKey": "Icons.Material.Filled.Lock" }
    ]
  },
  "LyoAuthWasmClient": {
    "AuthBaseUrl": "https://api.example.com",
    "HandoffCallbackPath": "/auth/handoff",
    "PostSignOutRedirectPath": "/",
    "StorageKey": "lyo_auth_session",
    "AccessTokenSkew": "00:00:30"
  }
}
```

### Quick start (3)

```json
{
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "https://spa.example.com" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  }
}
```

### Flow

```csharp
browser SPA ── click "Sign in with X" ──► WasmAuthSignInLauncher
                                                  │
                                                  ▼
browser ── GET /auth/login/{provider}?returnUrl=https://spa/auth/handoff&mode=browser ──► API
                                                                                            │
                                          (IdP roundtrip) ▼
browser ── 302 https://spa/auth/handoff?lyo_handoff=lyoh_... ──► WasmAuthHandoffPage
                                                                  │
                                                                  ▼
browser ── POST /auth/handoff/exchange { code } ──► API
                                                     │
                                                     ▼
                              { access_token, refresh_token, expires_in } ──► WasmAuthSessionStore (in-mem + LocalStorage)
```

## What's inside

| Service | Role |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `WasmAuthClientOptions` | `AuthBaseUrl`, `HandoffCallbackPath` (default `/auth/handoff`), `StorageKey`, `AccessTokenSkew`. |
| `WasmAuthSessionStore` | Singleton store with in-memory cache + `Blazored.LocalStorage` snapshot. Survives page reloads inside the SPA. |
| `WasmAuthApiClient` | Typed `HttpClient` for `/auth/handoff/exchange`, `/auth/refresh`, `/auth/logout`. |
| `WasmAuthDelegatingHandler` | Outbound bearer-injection + auto-refresh handler for any user `HttpClient`. |
| `WasmAuthStateProvider` | Blazor `AuthenticationStateProvider` that re-runs `LyoJwtClaimsParser` over the cached access token. |
| `WasmAuthSignInLauncher` | `IAuthSignInLauncher` implementation. Sign-in 302s to the API; sign-out revokes the refresh token and clears local state. |
| `WasmAuthUserClient` | `IAuthUserClient` implementation against `/auth/me` and `/auth/users/{id}`. |
| `WasmAuthSessionAccessor` | `IAuthSessionAccessor` implementation used by the debug page. |
| `Pages/WasmAuthHandoffPage` | Route `/auth/handoff` — redeems the `?lyo_handoff=...` code and stores the tokens. |

## Quick start

In the consuming WASM host's `Program.cs`: `appsettings.json` (served from the WASM client's `wwwroot/`): On the API side, the WASM origin **must** appear in `LyoOidcBff.AllowedReturnOrigins`:

## Outbound API calls

Add the delegating handler to any of your own typed clients so they automatically carry the bearer:

```csharp
builder.Services
    .AddHttpClient<MyApi>(c => c.BaseAddress = new("https://api.example.com"))
    .AddHttpMessageHandler<WasmAuthDelegatingHandler>();
```

## Caveats vs. the Server adapter

- Tokens live in **LocalStorage** on the browser. That's the standard SPA trade-off; XSS hijacks the session. If you can't accept that, use the Server (BFF) adapter instead, which stores tokens server-side and only puts a data-protected session id in the cookie.
- No cross-tab broadcast: a sign-out in one tab doesn't drop the session in another. (Possible follow-up via the LocalStorage `storage` event.)
- The handoff exchange happens in the browser (`fetch`), so the WASM origin must be in the API's allow-list.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication.Web.Components` — (direct, lyo)
- `Lyo.Diagnostic` — (direct, lyo)
- `Blazored.LocalStorage` `4.5.0` — (direct, third-party)
- `Microsoft.AspNetCore.Components.WebAssembly` `10.0.5` — (direct, microsoft)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `Lyo.Validation` — (transitive, lyo)
- `Lyo.Web.Components` — (transitive, lyo)
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