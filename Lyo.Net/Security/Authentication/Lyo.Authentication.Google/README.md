# Lyo.Authentication.Google

Google profile for `Lyo.Authentication.OpenIdConnect`. Registers `https://accounts.google.com` as a confidential OIDC client in the BFF login flow.

## Usage

```csharp
services.AddLyoAuthentication(configuration);
services.AddLyoOpenIdConnect(configuration);
services.AddGoogleProvider(configuration); // reads GoogleAuth:* by default
```

`appsettings.json`:

```json
{
  "GoogleAuth": {
    "ClientId":     "1234.apps.googleusercontent.com",
    "ClientSecret": "***",
    "RedirectUri":  "https://api.lyolabs.io/auth/callback/google",
    "HostedDomain": "lyolabs.io"
  }
}
```

When `HostedDomain` is set the provider rejects login attempts whose id_token `hd` claim does not match — useful for Google Workspace-only deployments. Personal `@gmail.com` accounts are rejected.

## Claim mapping

| id_token claim | Lyo property            |
|----------------|-------------------------|
| `sub`          | `LinkedIdentity.Subject` |
| `email`        | `LyoUser.Email`         |
| `email_verified` | `LyoUser.EmailVerified` |
| `name`         | `LyoUser.DisplayName`   |
| `picture`      | `LyoUser.AvatarUrl`     |
| `locale`       | `LyoUser.PreferredLanguageBcp47` |

Google does not emit roles, so `LinkedIdentity.Scopes` is always empty here — give the user baseline scopes via `LyoUser.Scopes` instead.

## Local development setup

End-to-end recipe for exercising the BFF flow on your laptop against real Google, with `Lyo.TestApi` as the API/auth server (`http://localhost:5251`) and `Lyo.Gateway` as the browser consumer (`http://localhost:5138`). Google explicitly allows plain `http://localhost` (and `http://127.0.0.1`) as OAuth redirect targets, so you don't need a tunnel or HTTPS cert for local dev.

### 1. Create the OAuth client in Google Cloud

1. Go to <https://console.cloud.google.com/> and create (or pick) a project — e.g. `lyo-dev-local`.
2. **APIs & Services → OAuth consent screen**:
   - User type: **External** for personal `@gmail.com` testing, **Internal** if everyone is in a single Workspace.
   - App name + support email + developer contact email (anything sensible).
   - Scopes: leave the defaults; the provider only requests `openid email profile`.
   - **Test users**: while the app is in *Testing* status, only emails listed here can log in — add your own Google account(s) now or the callback will 403.
3. **APIs & Services → Credentials → Create credentials → OAuth client ID**:
   - Application type: **Web application**.
   - Name: `Lyo TestApi local`.
   - **Authorized JavaScript origins**: `http://localhost:5251`.
   - **Authorized redirect URIs**: `http://localhost:5251/auth/callback/google` — must match `GoogleAuth:RedirectUri` byte-for-byte (scheme, host, port, path, no trailing slash). The Gateway origin (`http://localhost:5138`) is **not** a Google redirect URI; the browser only goes through the API.
   - Copy the generated **Client ID** and **Client secret** — you'll paste them in the next step.

### 2. Wire the secrets into Lyo.TestApi

The TestApi only registers the Google provider when `GoogleAuth:ClientId` is non-empty. Do **not** put real secrets in the committed `appsettings.json` / `appsettings.Development.json`. Use `dotnet user-secrets`:

```bash
cd Lyo.Net/Tools/Lyo.TestApi
dotnet user-secrets init
dotnet user-secrets set "GoogleAuth:ClientId"     "1234-abc.apps.googleusercontent.com"
dotnet user-secrets set "GoogleAuth:ClientSecret" "GOCSPX-..."
dotnet user-secrets set "GoogleAuth:RedirectUri"  "http://localhost:5251/auth/callback/google"
```

Leave `HostedDomain` **unset** for personal `@gmail.com` accounts. For Workspace-only:

```bash
dotnet user-secrets set "GoogleAuth:HostedDomain" "your-workspace.com"
```

Also seed the JWT issuer/audience so locally-issued tokens are accepted by the Gateway:

```bash
dotnet user-secrets set "LyoJwt:Issuer"        "http://localhost:5251"
dotnet user-secrets set "LyoJwt:Audience"      "lyo-test-api"
dotnet user-secrets set "LyoJwt:SigningKeyId"  "lyo-sig"
```

The Gateway origin must be on the BFF allow-list — already present in `Lyo.TestApi/appsettings.Development.json`:

```json
{
  "LyoOidcBff": {
    "AllowedReturnOrigins": [ "http://localhost:5138", "https://localhost:5138" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  }
}
```

### 3. Point the Gateway at the TestApi

The Gateway uses `Lyo.Authentication.Client`. The relevant `appsettings.json` block:

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

Optionally pin the JWT validation values so resource APIs in the Gateway can validate access tokens locally too:

```bash
cd Lyo.Net/Tools/Lyo.Gateway
dotnet user-secrets init
dotnet user-secrets set "LyoJwt:Issuer"   "http://localhost:5251"
dotnet user-secrets set "LyoJwt:Audience" "lyo-test-api"
```

### 4. Run both services and exercise the flow

```bash
# terminal 1
cd Lyo.Net/Tools/Lyo.TestApi && dotnet run --launch-profile http
# terminal 2
cd Lyo.Net/Tools/Lyo.Gateway && dotnet run --launch-profile http
```

Sanity-check the auth surface directly:

- `http://localhost:5251/.well-known/jwks.json` — should return a single Ed25519 JWK.
- Visit `http://localhost:5138` and click **Sign in with Google** (or hit `http://localhost:5138/auth/sign-in/google?returnUrl=/`). The Gateway 302s to the TestApi, which 302s to Google, which 302s back to the TestApi callback, which 302s to `http://localhost:5138/auth/handoff?lyo_handoff=lyoh_...`. The Gateway redeems the code server-side, sets `lyo_session`, and lands you on `/`.
- After login, `GET http://localhost:5251/auth/me` with `Authorization: Bearer <access_token>` (grab it from the session store via diagnostic tooling or from the API log line) returns the principal.
- `POST http://localhost:5138/auth/sign-out` revokes the refresh token at the API and clears `lyo_session`.

For raw API-client testing (no Gateway):

- `http://localhost:5251/auth/login/google?mode=api&returnUrl=http://localhost:5251/` — callback returns JSON `{access_token, refresh_token, expires_in, token_type}` instead of redirecting.
- `POST http://localhost:5251/auth/refresh` with body `{"refresh_token":"..."}` — rotates the pair.

### 5. Common local pitfalls

| Symptom | Cause / fix |
|---|---|
| `redirect_uri_mismatch` | The URI in Google Cloud doesn't exactly equal `GoogleAuth:RedirectUri`. Check scheme (`http` vs `https`), port, trailing slash. |
| `Error 403: access_denied` after consent | App is in *Testing* and the Google account you logged in with isn't in the **Test users** list. |
| `HostedDomainMismatch` from the callback | `HostedDomain` is set but the account's `hd` claim doesn't match (personal `@gmail.com` accounts have no `hd`). |
| Callback redirects back to the API instead of the Gateway | `returnUrl` was rejected by the BFF allow-list and fell back to `DefaultReturnUrl`. Confirm the Gateway origin is in `LyoOidcBff:AllowedReturnOrigins` (exact `scheme://host:port`, no trailing slash). |
| `/auth/handoff/exchange` returns 403 `origin_mismatch` | Consumer's `Origin` header didn't match the origin the code was issued to. Ensure browser, Gateway, and API agree on `localhost` (not a mix of `localhost` / `127.0.0.1`). |
| `lyo_session` not sent on Gateway requests | The cookie is `SameSite=Lax`; cross-site iframe scenarios won't carry it. Plain top-level navigation works. |
| JWT signature fails | Mismatched `LyoJwt:Issuer`/`Audience`/`SigningKeyId` between TestApi and Gateway. Both must agree, and both `AddLocalKeyStore` calls must seed the same `lyo-sig` material (they do by default). |

### 6. Promote to deployed environments

When you move off localhost, replace the user-secrets values with the deployed equivalents and **add a second OAuth client** in Google Cloud (or extra redirect URIs on the same client) for each environment:

- Staging: `https://api-staging.example.com/auth/callback/google` + `LyoOidcBff:AllowedReturnOrigins` includes `https://app-staging.example.com`
- Production: `https://api.example.com/auth/callback/google` + `LyoOidcBff:AllowedReturnOrigins` includes `https://app.example.com`

Production must be on HTTPS — Google rejects non-`localhost` plain-HTTP redirect URIs. Submit the OAuth consent screen for verification before you remove the *Testing* gate, otherwise external users will still get the unverified-app screen (and personal accounts beyond the 100-test-user cap will be blocked).
