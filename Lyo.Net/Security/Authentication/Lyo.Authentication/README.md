# Lyo.Authentication

Server-side authentication services for Lyo. Two coexisting bearer formats behind a single contract:

- **Format B opaque API tokens** — `lyo_<kind>_<ring>_<id>_<secret>` (e.g. `lyo_pat_live_01hxy8k2qf9_4f3b...`). Store-backed, validated by DB lookup + constant-time SHA-256
  comparison. For CLIs, services, integrations, webhooks, and refresh tokens. - **Lyo-signed JWTs** — `Authorization: Bearer ey...` (EdDSA / Ed25519). Short-lived, locally
  validated via JWKS. Issued by the Lyo API after a successful external OIDC login. For browser/mobile frontends and direct API callers.

This package has zero ASP.NET, EF, or HTTP dependencies — but it **does** depend on `Lyo.KeyStore`, `Lyo.Hashing`, and BouncyCastle for key/hash operations. Reserve it for the
API/auth-server host. **Do not reference it from consumer-side libraries or Blazor WebAssembly clients** — use `Lyo.Authentication.Models` instead for wire-shape DTOs / format
helpers / JWT parsing.

It owns:

- `ApiTokenCodec` — mint + hash for Format-B tokens (parse-only helpers live in `Lyo.Authentication.Models`) - `IApiTokenIssuer` / `IApiTokenValidator` — opaque token lifecycle -
  `ILyoJwtIssuer` / `ILyoJwtValidator` — Ed25519 JWT lifecycle (backed by `Lyo.KeyStore` for the signing key) - `IUserStore` / `IExternalIdentityStore` — Lyo user + linked-identity
  persistence - `IScopeRegistry` — fine-grained scope contract (`{resource}.{action}`) - `IApiTokenStore` — token persistence (in-memory fallback included) -
  `Ed25519KeyBootstrapper` — `IHostedService` that auto-provisions a signing key on first run - `IAuthAuditRecorder` / `IAuthAuditContextAccessor` / `AuthAuditExtensions` —
  server-side audit recorder plumbing (the event records + enum taxonomy live in `Lyo.Authentication.Models`)

## Examples

### Register services

```csharp
services.AddLyoAuthentication(configuration.GetSection(AuthenticationOptions.SectionName));
services.AddLyoJwtIssuer(); // uses IKeyStore for the signing key
services.AddInMemoryAuthenticationStores(); // swap for .Postgres in production
services.AddScope("people.read", "Read people");
services.AddScope("people.write", "Modify people records", implies: "people.read");
```

### Configuration cheatsheet

```json
{
  "LyoAuthentication": { "Ring": "live" },
  "LyoJwt": { "Issuer": "https://auth.lyo", "Audience": "lyo-api", "SigningKeyId": "lyo-sig" },
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "https://app.example.com" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  },
  "PostgresUser": { "ConnectionString": "...", "EnableAutoMigrations": true }
}
```

## Package layering

The auth stack is split so consumer-side libraries (including Blazor WebAssembly clients) cannot see server-only types:

| Package                                    | What's in it                                                                                                                                                                                                                                                                                                                                                                                                                    | Safe to reference from a WASM client? |
|--------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------|
| **`Lyo.Authentication.Models`**            | Wire records (`LyoUser`, `LinkedIdentity`, `IssuedLyoJwt`, `IssuedApiToken`, `ApiTokenRecord`, `ApiTokenPrincipal`, `ApiTokenIssueRequest`), JWT claim-name constants, `LyoJwtClaimsParser`, format helpers (`Base64Url`, `Base32Crockford`, `ApiToken`, `ApiTokenKind`, `ApiTokenRing`), audit-event taxonomy (`AuthAuditEvent`, `AuthAuditEventKind`), `Scope` record. Only depends on `Lyo.Exceptions` + `System.Text.Json`. | **Yes**                               |
| **`Lyo.Authentication`** *(this package)*  | `Services/Jwt/*`, `Services/Opaque/*`, `Services/Refresh/*`, `Services/Users/*`, `ScopeRegistry`, `IAuthAuditRecorder` + impls, `ApiTokenCodec.Mint`/`ComputeSecretHash`, all `Options`, `AddLyoAuthentication` DI. Pulls in `Lyo.KeyStore`, `Lyo.Hashing`, BouncyCastle.                                                                                                                                                       | **No**                                |
| `Lyo.Authentication.Client`                | Consumer-side BFF runtime (handoff exchange, server-side session store, delegating handler, cookie auth handler). References `Models` only.                                                                                                                                                                                                                                                                                     | Yes (server-side host)                |
| `Lyo.Authentication.Web.Components`        | Host-agnostic Razor pages (login, debug, profile). References `Models` only.                                                                                                                                                                                                                                                                                                                                                    | Yes                                   |
| `Lyo.Authentication.Web.Components.Server` | Blazor Server host adapter — wires the shared pages to `Lyo.Authentication.Client`.                                                                                                                                                                                                                                                                                                                                             | n/a (server-only)                     |
| `Lyo.Authentication.Web.Components.Wasm`   | Blazor WebAssembly host adapter — pure-browser token flow. References `Models` only.                                                                                                                                                                                                                                                                                                                                            | n/a (it *is* the WASM client)         |

ASP.NET wiring lives in `Lyo.Authentication.AspNetCore`. Postgres persistence and the Postgres audit recorder live in `Lyo.Authentication.Postgres`. OIDC flow lives in
`Lyo.Authentication.OpenIdConnect` with provider profiles `Lyo.Authentication.Google` and `Lyo.Authentication.Keycloak`. The consumer-side BFF runtime lives in
`Lyo.Authentication.Client`.

## Full stack — typical host wiring (API/auth server)

A web host that issues both opaque tokens and JWTs, persists users + tokens + audit events to Postgres, and runs OIDC login through Google and Keycloak:

```csharp
services.AddLyoAuthentication(builder.Configuration);
services.AddPostgresAuthenticationStoresFromConfiguration(builder.Configuration);
services.AddLyoApiTokenAuthentication(); // ASP.NET schemes + LyoBearer policy scheme
services.AddAuthorization();
services.AddLyoOpenIdConnect(builder.Configuration);
services.AddGoogleProviderFromConfiguration(builder.Configuration);
services.AddKeycloakProviderFromConfiguration(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();
app.MapLyoJwks(); // /.well-known/jwks.json
app.MapLyoAuthEndpoints(); // /auth/login/{provider}, /auth/callback/{provider}, /auth/handoff/exchange, /auth/token, /auth/refresh, /auth/logout, /auth/me
app.MapLyoTokenManagementEndpoints(); // /tokens (PAT lifecycle)
```

## BFF flow — browser consumer (handoff)

- Browser hits `GET /auth/sign-in/{provider}?returnUrl=/dashboard` on the **consumer** (powered by `Lyo.Authentication.Client`). The consumer 302-redirects to
  `GET https://api/auth/login/{provider}?returnUrl=https://consumer/auth/handoff&mode=browser`.
- API seals PKCE+state+nonce in the HttpOnly `lyo_oidc_state` cookie and 302s to the IdP authorize endpoint.
- IdP redirects back to `GET https://api/auth/callback/{provider}?code=...&state=...`. The API unseals the cookie, exchanges the code for an `id_token`, validates
  signature/issuer/audience/nonce, runs the provider's claim mapper, looks up or JIT-provisions the `LyoUser`, links the external identity, mints a Lyo JWT + a rotating refresh
  token, stores them under a one-time **handoff code** (`lyoh_…`, TTL 30s), and 302-redirects to `https://consumer/auth/handoff?lyo_handoff=lyoh_...`.
- Consumer's handoff endpoint POSTs `{ code: "lyoh_..." }` to `https://api/auth/handoff/exchange` server-to-server with `Origin: https://consumer`. API verifies the origin matches
  the one the code was issued to, marks the code consumed, and returns `{ access_token, refresh_token, expires_in, token_type }`.
- Consumer stashes the tokens in its server-side `LyoAuthSessionStore` and sets an HttpOnly `lyo_session` cookie containing only the data-protected session id. The browser sees
  nothing else.
- Outbound calls from the consumer to the API go through `LyoAuthDelegatingHandler`, which attaches `Authorization: Bearer <access_token>` and transparently calls `/auth/refresh`
  (server-to-server) on `401` or near-expiry.
- `GET /auth/sign-out` on the consumer POSTs the refresh token to `/auth/logout`, then clears the local session cookie.

## API-client flow (no browser)

- Hit `GET https://api/auth/login/{provider}?mode=api&returnUrl=...` from a controllable browser or system-browser webview. The callback returns JSON
  `{ access_token, refresh_token, expires_in, token_type }` instead of a handoff redirect.
- Use `Authorization: Bearer <access_token>` against the API.
- When the access token nears expiry, `POST /auth/refresh` with body `{ "refresh_token": "lyo_rfr_live_..." }`. The response is a rotated pair.
- `POST /auth/logout` with body `{ "refresh_token": "..." }` to invalidate the family.

## Per-provider knobs

- **Google** — see [`Lyo.Authentication.Google/README.md`](../Lyo.Authentication.Google/README.md) (includes a full Google Cloud + `Lyo.TestApi` + `Lyo.Gateway` local walkthrough).
- **Keycloak** — see [`Lyo.Authentication.Keycloak/README.md`](../Lyo.Authentication.Keycloak/README.md) (includes a Docker-based local Keycloak walkthrough and the peer-vs-broker
  discussion).
- **Custom** — implement `IOpenIdConnectProvider` and register it as a singleton; `DefaultExternalLoginCoordinator` will pick it up by name.

## Auditing

Every meaningful auth state change emits an `AuthAuditEvent` through `IAuthAuditRecorder`. The default `NullAuthAuditRecorder` discards events; `Lyo.Authentication.Postgres` ships
`PostgresAuthAuditRecorder` which persists into the `[user].[event]` table (the `kind` column stores the enum's string name, e.g. `JwtIssued`). Ambient context (IP, User-Agent,
correlation id) is pulled from the registered `IAuthAuditContextAccessor` — defaults to `NullAuthAuditContextAccessor`;
`Lyo.Authentication.AspNetCore.AddLyoApiTokenAuthentication()` automatically swaps in `HttpAuthAuditContextAccessor` so rows carry the inbound caller's IP / User-Agent / trace id.

Closed taxonomy lives in `AuthAuditEventKind`:

| Kind                                                              | Source                                                |
|-------------------------------------------------------------------|-------------------------------------------------------|
| `UserProvisioned`, `IdentityLinked`, `IdentityUnlinked`           | `DefaultExternalLoginCoordinator` / admin tools       |
| `ExternalLoginSucceeded`, `ExternalLoginRejected`                 | `DefaultExternalLoginCoordinator.HandleCallbackAsync` |
| `HandoffCodeIssued`, `HandoffCodeConsumed`, `HandoffCodeRejected` | `AuthEndpointsMapper`                                 |
| `JwtIssued`                                                       | `Ed25519LyoJwtIssuer`                                 |
| `TokenIssued`, `TokenRejected`, `TokenRevoked`, `TokenValidated`  | `DefaultApiTokenIssuer` / `DefaultApiTokenValidator`  |
| `RefreshSucceeded`, `RefreshRejected`                             | `DefaultLyoRefreshTokenExchange`                      |
| `SignedOut`, `UserDisabled`, `UserEnabled`                        | `AuthEndpointsMapper` / admin tools                   |
| `SigningKeyBootstrapped`, `SigningKeyRotated`                     | `Ed25519KeyBootstrapper`                              |

Only append new members at the end of the enum — its integer values are part of the on-disk schema.

## Configuration cheatsheet

This package and its host-side siblings (`Lyo.Authentication.OpenIdConnect`, `Lyo.Authentication.Postgres`) own these sections: Provider-specific sections (`GoogleAuth`,
`KeycloakAuth`, …) and full local-development walkthroughs (Google Cloud OAuth client, Keycloak Docker setup, environment promotion) live alongside the provider implementations
themselves — see [`Lyo.Authentication.Google/README.md`](../Lyo.Authentication.Google/README.md) and [
`Lyo.Authentication.Keycloak/README.md`](../Lyo.Authentication.Keycloak/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication.Models` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Lyo.KeyStore` — (direct, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (direct, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (direct, microsoft)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)