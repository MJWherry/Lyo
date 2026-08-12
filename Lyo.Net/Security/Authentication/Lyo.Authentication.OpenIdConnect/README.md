# Lyo.Authentication.OpenIdConnect

OpenID Connect client base for Lyo. The Lyo API is the OIDC **confidential client** (BFF pattern); the frontend never sees the IdP and never receives tokens by URL fragment. After a successful external login, the API either:

- issues tokens directly as JSON (for **API clients** calling `/auth/login/{provider}?mode=api`), or - mints a single-use **handoff code** and 302-redirects the browser to a whitelisted consumer origin which then exchanges that code server-to-server for the tokens (for **browser clients** via `Lyo.Authentication.Client`).

## Examples

### Register services

```csharp
services.AddLyoOpenIdConnect(builder.Configuration);
services.AddGoogleProviderFromConfiguration(builder.Configuration);
services.AddKeycloakProviderFromConfiguration(builder.Configuration);
```

## Building blocks

- `IOpenIdConnectProvider` — abstraction describing a provider (discovery URL, client id/secret, scope/claim mapping)
- `OpenIdConnectProviderRegistry` — keyed by name, resolved at `/auth/login/{name}`
- `OidcDiscoveryCache` — hourly-refreshed OpenID Configuration cache
- `OidcJwksResolver` — fetches and caches the provider's JWKS for `id_token` signature verification
- `PkceCodes` + `StateNonceProtector` — generate and seal PKCE/state/nonce in an HTTP-only cookie via `IDataProtector`
- `OidcAuthorizationUrlBuilder` — composes the `/authorize` URL with `code_challenge`, `state`, `nonce`
- `OidcTokenExchangeClient` — typed `HttpClient` that POSTs the authorization code back for tokens
- `OidcIdTokenValidator` — validates issuer, audience, nonce, exp, signature
- `IExternalLoginCoordinator` / `DefaultExternalLoginCoordinator` — wraps the whole flow: discover-or-link-or-create the Lyo user, refresh `linked_identity.scopes_json`, and call `ILyoJwtIssuer.IssueAsync`. Emits `AuthAuditEventKind.{ExternalLoginSucceeded,ExternalLoginRejected,UserProvisioned,IdentityLinked}` along the way.
- `IHandoffCodeStore` / `InMemoryHandoffCodeStore` — single-use, TTL-bounded handoff codes (browser handoff path)

## Endpoints

`app.MapLyoAuthEndpoints()` wires:

| Method | Path | Purpose |
| ------ | --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| GET | `/auth/login/{provider}?returnUrl=...&mode=browser\ | api` |
| GET | `/auth/callback/{provider}` | IdP redirects back here. Browser mode → mint handoff code + 302 to `{returnUrl}?lyo_handoff=lyoh_...`. API mode → 200 OK with `{access_token, refresh_token, expires_in, token_type}`. |
| POST | `/auth/handoff/exchange` | Body `{ code }`. Consumes a handoff code once, returns tokens. Audited as `HandoffCodeConsumed` / `HandoffCodeRejected`. |
| POST | `/auth/token` | Reserved for first-party API client grants. |
| POST | `/auth/refresh` | Body `{ refresh_token }`. Returns a rotated `{access_token, refresh_token, expires_in}`. |
| POST | `/auth/logout` | Body `{ refresh_token }`. Revokes the token; audited as `TokenRevoked` + `SignedOut`. |
| GET | `/auth/me` | Returns the principal for the bearer access token. |
| GET | `/auth/users/{id}` | Returns the same shape as `/auth/me` for an arbitrary user id. Requires the `auth.users.read` scope (policy `scope:auth.users.read`). `Scopes` reflects the target user's baseline scopes, not the caller's. |

## Registration

That call binds two option sections: - `LyoExternalLogin` — `Sealing.{Purpose,DefaultExpiration}` and cookie name for the PKCE/state envelope. - `LyoOidcBff` — see [BFF options](#bff-options) below. The per-provider packages register their `IOpenIdConnectProvider` implementation under their canonical name (`google`, `keycloak:<realm>`).

## BFF options

```json
{
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "http://localhost:5138", "https://app.example.com" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  }
}
```

- `AllowedReturnOrigins` — exact origin (`scheme://host[:port]`) match for absolute `returnUrl` values and `Origin` checks on `/auth/handoff/exchange`. Same-origin (relative
  `returnUrl` starting with `/`) is always allowed. Anything outside the allowlist falls back to `DefaultReturnUrl`.
- `DefaultReturnUrl` — used when `returnUrl` is missing or rejected.
- `HandoffCodeTtl` — short. 30s is plenty: the consumer redeems immediately on the redirect.

## Auditing

Every meaningful state transition emits an `AuthAuditEvent`. With `Lyo.Authentication.Postgres` wired up these land in `[user].[event]` (the `kind` column stores the enum's string
name — `JwtIssued`, `HandoffCodeIssued`, …); otherwise they hit whatever `IAuthAuditRecorder` is registered (defaults to `NullAuthAuditRecorder`). IP / User-Agent / correlation
come from the registered `IAuthAuditContextAccessor` — call `services.AddLyoApiTokenAuthentication()` (or `services.AddLyoAuthHttpContextAccessor()` standalone) on an ASP.NET host
to swap in `HttpAuthAuditContextAccessor`.

| Kind | When |
| -------------------------------------- | ---------------------------------------------------------------- |
| `ExternalLoginSucceeded` | Callback validated, tokens minted. |
| `ExternalLoginRejected` | State/nonce/signature/policy failure. Carries a stable `reason`. |
| `UserProvisioned` | First-time JIT user creation. |
| `IdentityLinked` | New `(provider, subject)` linked to a user. |
| `HandoffCodeIssued` | Mint succeeded; included in the browser redirect. |
| `HandoffCodeConsumed` | Successful `/auth/handoff/exchange`. |
| `HandoffCodeRejected` | Wrong origin, expired, unknown, or already consumed. |
| `JwtIssued` | Access token minted. |
| `RefreshSucceeded` / `RefreshRejected` | `/auth/refresh` outcome. |
| `TokenRevoked` / `SignedOut` | `/auth/logout` outcome. |

## Talking to it

- **Browser consumers** — use `Lyo.Authentication.Client` (handoff redemption, session cookie, `LyoAuthDelegatingHandler` for outbound refresh).
- **API clients** — call `/auth/login/{provider}?mode=api` and consume the JSON token response directly; call `/auth/refresh` when the access token nears expiry.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (direct, third-party)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)