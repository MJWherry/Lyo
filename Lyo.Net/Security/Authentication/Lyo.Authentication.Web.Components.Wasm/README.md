# Lyo.Authentication.Web.Components.Wasm

Blazor WebAssembly host adapter for [`Lyo.Authentication.Web.Components`](../Lyo.Authentication.Web.Components/README.md). Implements the same login / debug / profile pages over a
**pure-browser** auth flow — no consumer-side server, no HttpOnly cookie. Tokens live in `Blazored.LocalStorage` and the browser talks directly to the Lyo API.

## Flow

```
browser SPA ── click "Sign in with X" ──► WasmAuthSignInLauncher
                                                  │
                                                  ▼
browser ── GET /auth/login/{provider}?returnUrl=https://spa/auth/handoff&mode=browser ──► API
                                                                                            │
                                          (IdP roundtrip)                                   ▼
browser ── 302 https://spa/auth/handoff?lyo_handoff=lyoh_... ──► WasmAuthHandoffPage
                                                                  │
                                                                  ▼
browser ── POST /auth/handoff/exchange { code } ──► API
                                                     │
                                                     ▼
                              { access_token, refresh_token, expires_in } ──► WasmAuthSessionStore (in-mem + LocalStorage)
```

## What's inside

| Service                     | Role                                                                                                                      |
|-----------------------------|---------------------------------------------------------------------------------------------------------------------------|
| `WasmAuthClientOptions`     | `AuthBaseUrl`, `HandoffCallbackPath` (default `/auth/handoff`), `StorageKey`, `AccessTokenSkew`.                          |
| `WasmAuthSessionStore`      | Singleton store with in-memory cache + `Blazored.LocalStorage` snapshot. Survives page reloads inside the SPA.            |
| `WasmAuthApiClient`         | Typed `HttpClient` for `/auth/handoff/exchange`, `/auth/refresh`, `/auth/logout`.                                         |
| `WasmAuthDelegatingHandler` | Outbound bearer-injection + auto-refresh handler for any user `HttpClient`.                                               |
| `WasmAuthStateProvider`     | Blazor `AuthenticationStateProvider` that re-runs `LyoJwtClaimsParser` over the cached access token.                      |
| `WasmAuthSignInLauncher`    | `IAuthSignInLauncher` implementation. Sign-in 302s to the API; sign-out revokes the refresh token and clears local state. |
| `WasmAuthUserClient`        | `IAuthUserClient` implementation against `/auth/me` and `/auth/users/{id}`.                                               |
| `WasmAuthSessionAccessor`   | `IAuthSessionAccessor` implementation used by the debug page.                                                             |
| `Pages/WasmAuthHandoffPage` | Route `/auth/handoff` — redeems the `?lyo_handoff=...` code and stores the tokens.                                        |

## Quick start

In the consuming WASM host's `Program.cs`:

```csharp
builder.Services.AddLyoAuthWebComponents(builder.Configuration);
builder.Services.AddLyoAuthWebComponentsWasm(builder.Configuration);
```

`appsettings.json` (served from the WASM client's `wwwroot/`):

```json
{
  "LyoAuthWebComponents": {
    "Providers": [
      { "Name": "google",            "DisplayName": "Sign in with Google",   "IconKey": "Icons.Material.Filled.Google" },
      { "Name": "keycloak:my-realm", "DisplayName": "Sign in with Keycloak", "IconKey": "Icons.Material.Filled.Lock"   }
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

On the API side, the WASM origin **must** appear in `LyoOidcBff.AllowedReturnOrigins`:

```json
{
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "https://spa.example.com" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  }
}
```

## Outbound API calls

Add the delegating handler to any of your own typed clients so they automatically carry the bearer:

```csharp
builder.Services
    .AddHttpClient<MyApi>(c => c.BaseAddress = new("https://api.example.com"))
    .AddHttpMessageHandler<WasmAuthDelegatingHandler>();
```

## Caveats vs. the Server adapter

* Tokens live in **LocalStorage** on the browser. That's the standard SPA trade-off; XSS hijacks the session. If you can't accept that, use the Server (BFF) adapter instead, which
  stores tokens server-side and only puts a data-protected session id in the cookie.
* No cross-tab broadcast: a sign-out in one tab doesn't drop the session in another. (Possible follow-up via the LocalStorage `storage` event.)
* The handoff exchange happens in the browser (`fetch`), so the WASM origin must be in the API's allow-list.
