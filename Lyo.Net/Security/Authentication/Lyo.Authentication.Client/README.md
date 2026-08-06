# Lyo.Authentication.Client

Consumer-side runtime for the Lyo BFF auth flow. Plugs a web host (typically a Blazor Server gateway or a server-rendered API consumer) into a Lyo authentication API without ever exposing tokens to the browser.

## Examples

### Quick start

```csharp
builder.Services.AddLyoAuthClient(builder.Configuration);
builder.Services.AddLyoAuthBlazorStateProvider();
builder.Services
    .AddLyoApiClient(...)
    .AddLyoAuthHandler();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapLyoAuthSignIn();
app.MapLyoAuthHandoffCallback();
app.MapLyoAuthSignOut();
```

### Quick start (2)

```json
{
  "LyoAuthClient": {
    "AuthBaseUrl": "http://localhost:5251",
    "HandoffCallbackPath": "/auth/handoff",
    "SignInPath": "/auth/sign-in",
    "SignOutPath": "/auth/sign-out",
    "PostSignOutRedirectPath": "/",
    "CookieName": "lyo_session",
    "SessionAbsoluteExpiration": "30.00:00:00",
    "AccessTokenSkew": "00:00:30"
  }
}
```

### Quick start (3)

```json
{
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "http://localhost:5138" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  }
}
```

### Quick start (4)

```razor
<a href="/auth/sign-in/google?returnUrl=/dashboard">Sign in with Google</a>
<form method="post" action="/auth/sign-out"><button type="submit">Sign out</button></form>
```

## What it does

The Lyo API (running `Lyo.Authentication.OpenIdConnect.Endpoints.AuthEndpointsMapper`)
handles `/auth/login`, `/auth/callback`, `/auth/handoff/exchange`, `/auth/token`,
`/auth/refresh`, `/auth/logout`, and `/auth/me`. It issues Lyo-signed JWTs after a
successful external login (Google, Keycloak, …).

For browser clients the callback redirects to
`{returnUrl}?lyo_handoff=lyoh_…` instead of dropping tokens directly. This
package picks up the redirect on the consumer origin, exchanges the handoff code
server-to-server for the tokens, stashes them in a `LyoAuthSessionStore`,
issues an HttpOnly cookie containing only the data-protected session id, and
projects the JWT claims into ASP.NET's `ClaimsPrincipal` and Blazor's
`AuthenticationStateProvider`.

Outbound API calls go through `LyoAuthDelegatingHandler` which injects
`Authorization: Bearer <access_token>` and transparently refreshes on 401.

```
browser ── GET /auth/sign-in/google ─────────────► consumer (this lib)
              ▲ 302 https://api/auth/login/google?returnUrl=https://consumer/auth/handoff
              │
              │
consumer ── GET /auth/login/google ─────────────► API
              ▲ 302 https://accounts.google.com/...
              │
              │
browser ── (Google) ─────────────────────────────► API /auth/callback/google
              ▲ 302 https://consumer/auth/handoff?lyo_handoff=lyoh_...
              │
              │
consumer ── POST /auth/handoff/exchange ──────► API (server-to-server)
              ◄──── {access_token, refresh_token}
              │
              │
browser receives Set-Cookie: lyo_session=...
```

## Quick start

In the consuming host's `Program.cs`: `appsettings.json`: On the API side, add the consumer's origin to the allow-list: Then in your UI:

## API client flow (no browser)

For non-browser callers, hit `POST /auth/token` (grant_type=refresh_token) and `POST /auth/refresh` directly on the API. Both return JSON `{access_token, expires_in, refresh_token, token_type}`. This package isn't needed for that flow — it exists purely to bridge the browser handoff into a server-managed session.

## Production swap-out points

- `LyoAuthSessionStore` is in-process by default. For multi-instance deployments override with a Redis/Postgres-backed subclass and register it as a singleton.
- `IHandoffCodeStore` on the API side is the same story.
- Set `CookieDomain` only if the consumer is one of several subdomains that must share the session (e.g. `.lyo.app`).

## Security checklist

- Cookies are `HttpOnly`, `Secure` (when the request is HTTPS), `SameSite=Lax`, and carry only the data-protected session id — never tokens.
- The handoff code is single-use, TTL-bounded (default 30s), and bound to the consumer's `Origin` header on the exchange.
- Tokens never appear in URL fragments, server logs, or the browser DOM.
- Sign-out revokes the refresh token at the API and clears the local session.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication.Models` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)