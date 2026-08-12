# Lyo.Authentication.Keycloak

Keycloak profile for `Lyo.Authentication.OpenIdConnect`. Wires one or more Keycloak realms as confidential OIDC clients in the BFF login flow.

## Examples

### Usage

```csharp
services.AddLyoAuthentication(configuration);
services.AddLyoOpenIdConnect(configuration);
services.AddKeycloakProvider(configuration); // reads KeycloakAuth:* by default
```

### Usage (2)

```json
{
  "KeycloakAuth": {
    "BaseUrl": "https://sso.lyolabs.io",
    "Realm": "lyo",
    "ClientId": "lyo-api",
    "ClientSecret": "***",
    "RedirectUri": "https://api.lyolabs.io/auth/callback/keycloak:lyo",
    "RolesToScopes": {
      "lyo-admin": ["admin"],
      "lyo-people-rw": ["people.read", "people.write"]
    }
  }
}
```

### 1. Run Keycloak locally

```bash
docker run --name lyo-keycloak --rm -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin \
  -e KC_BOOTSTRAP_ADMIN_USERNAME=admin -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest start-dev
```

### 4. Wire the secrets into Lyo.TestApi

```bash
cd Lyo.Net/Tools/Lyo.TestApi
dotnet user-secrets init
dotnet user-secrets set "KeycloakAuth:BaseUrl" "http://localhost:8080"
dotnet user-secrets set "KeycloakAuth:Realm" "lyo"
dotnet user-secrets set "KeycloakAuth:ClientId" "lyo-api"
dotnet user-secrets set "KeycloakAuth:ClientSecret" "***"
dotnet user-secrets set "KeycloakAuth:RedirectUri" "http://localhost:5251/auth/callback/keycloak:lyo"
```

### 4. Wire the secrets into Lyo.TestApi (2)

```json
{
  "KeycloakAuth": {
    "RolesToScopes": {
      "lyo-admin": [ "admin" ],
      "lyo-people-rw": [ "people.read", "people.write" ]
    }
  }
}
```

### 5. Run everything and exercise the flow

```bash
# terminal 1 — Keycloak (from step 1, leave running)
# terminal 2
cd Lyo.Net/Tools/Lyo.TestApi && dotnet run --launch-profile http
# terminal 3
cd Lyo.Net/Tools/Lyo.Gateway && dotnet run --launch-profile http
```

## Usage

`appsettings.json`: The provider name is `keycloak:<realm>` (e.g. `keycloak:lyo`) so multiple realms can coexist as distinct providers.

## Peer vs broker

- **Peer mode** — register `AddGoogleProvider` AND `AddKeycloakProvider`; the login chooser routes to `/auth/login/google` or `/auth/login/keycloak:lyo`.
- **Broker mode** — register only `AddKeycloakProvider`; configure Google (etc.) as a federated IdP inside Keycloak.

## Realm-role mapping

Keycloak emits realm roles under `realm_access.roles` in the id_token. The Lyo provider extracts that list, looks each role up in `RolesToScopes`, and writes the union into `LinkedIdentity.Scopes`. Demoting a user in Keycloak (removing a role from their realm membership) takes effect on their next login — the next minted Lyo JWT will not carry the removed scopes. Roles that are not in the mapping table are silently dropped so adding a Keycloak role can never accidentally grant Lyo scopes.

## Local development setup

End-to-end recipe for exercising the BFF flow against a Keycloak running on your laptop, with `Lyo.TestApi` as the API/auth server (`http://localhost:5251`) and `Lyo.Gateway` as the browser consumer (`http://localhost:5138`). Keycloak runs on `http://localhost:8080` and is reachable directly from both your browser and the TestApi.

## 1. Run Keycloak locally

The official `quay.io/keycloak/keycloak` image in dev mode (HTTP, in-memory DB — wiped on container restart) is the fastest way to get a working IdP: Both env-var name pairs are set on purpose: Keycloak ≤25 reads `KEYCLOAK_ADMIN*`, Keycloak ≥26 reads `KC_BOOTSTRAP_ADMIN_*`. Leaving both in place keeps the snippet image-version-agnostic. Once it's up, open <http://localhost:8080/> and sign in to the admin console as `admin` / `admin`.

## 2. Create the realm and the Lyo client

- **Create realm** (top-left dropdown → *Create realm*): name = `lyo`. Switch the active realm to `lyo`.
- **Clients → Create client** → *General settings*:
- Client type = `OpenID Connect`
- Client ID = `lyo-api`
- Name = anything friendly.
- *Capability config*:
- Client authentication = **ON** (confidential client — required because the BFF holds the secret).
- Authorization = OFF.
- Authentication flow: leave **Standard flow** ticked; untick everything else.
- *Login settings*:
- Valid redirect URIs: `http://localhost:5251/auth/callback/keycloak:lyo` **Must include the `:lyo` suffix** — the Lyo provider name defaults to `keycloak:{realm}`, and that's the segment the API uses to route the callback.
- Web origins: `http://localhost:5251` (or `+` to mirror the redirect-URI list).
- Root URL / Home URL / Admin URL can stay empty.
- *Credentials* tab → copy the **Client secret**. You'll paste it into user-secrets in the next step.

## 3. Create realm roles + a test user

- **Realm roles → Create role** for each role you want to be able to map → for example `lyo-admin`, `lyo-people-rw`. These names go on the *Keycloak* side; `RolesToScopes` (next step) decides what Lyo scopes they grant.
- **Users → Add user**: username = `dev@lyolabs.io` (or whatever), Email verified = ON.
- *Credentials* tab → Set password (non-temporary).
- *Role mappings* tab → Assign role → tick the realm roles you created in step 1.

## 4. Wire the secrets into Lyo.TestApi

The TestApi only registers the Keycloak provider when `KeycloakAuth:ClientId` is non-empty. Use `dotnet user-secrets` instead of committing secrets to `appsettings.json`:

Role-to-scope mapping must live in JSON (user-secrets can hold the keys but the array values are awkward to set on the CLI) — add this to
`Lyo.TestApi/appsettings.Development.json` (or to the secrets file directly):

Also seed the JWT issuer/audience and Gateway allow-list as documented in the Google walkthrough (`LyoJwt:Issuer`, `LyoJwt:Audience`, `LyoJwt:SigningKeyId`,
`LyoOidcBff:AllowedReturnOrigins`). See [`Lyo.Authentication.Google/README.md` §2–§3](../Lyo.Authentication.Google/README.md#2-wire-the-secrets-into-lyotestapi) for the full
block — it is identical irrespective of which IdP you pair Lyo with.

## 5. Run everything and exercise the flow

- `http://localhost:8080/realms/lyo/.well-known/openid-configuration` — should return the realm's discovery document.
- `http://localhost:5251/.well-known/jwks.json` — Lyo signing keys.
- Visit `http://localhost:5138`, click **Sign in with Keycloak** (or hit `http://localhost:5138/auth/sign-in/keycloak:lyo?returnUrl=/`). The Gateway 302s to the TestApi, which 302s to Keycloak's login page, which 302s back to the TestApi callback, which 302s to `http://localhost:5138/auth/handoff?lyo_handoff=lyoh_...`. The Gateway redeems the code server-side, drops `lyo_session`, and lands you on `/`.
- After login, `GET http://localhost:5251/auth/me` returns the principal — `LinkedIdentity.Scopes` will contain the union of scope arrays mapped from the user's realm roles.

## 6. Common local pitfalls

| Symptom | Cause / fix |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `redirect_uri_mismatch` (Keycloak error page) | The URI in *Valid redirect URIs* doesn't byte-for-byte equal `KeycloakAuth:RedirectUri`. Most often the `:lyo` suffix was dropped or the port mismatches. |
| `Invalid client or Invalid client credentials` | Client secret mismatch (regenerate in Keycloak's *Credentials* tab and re-set the user-secret), or the realm name differs from `KeycloakAuth:Realm`. |
| Login succeeds but `LinkedIdentity.Scopes` is empty | Either the user has no realm roles assigned, the `roles` scope wasn't requested (Lyo requests it by default — only an issue if you've overridden `KeycloakAuth:Scopes`), or none of the user's roles are present in `RolesToScopes`. |
| `Realm does not exist` from Lyo on startup | `KeycloakAuth:BaseUrl` includes a `/realms/...` segment. Keep `BaseUrl` at the server root (e.g. `http://localhost:8080`); the provider appends `/realms/{Realm}` itself. |
| Callback redirects to the API origin instead of the Gateway | Same root cause as in the Google guide — Gateway origin isn't in `LyoOidcBff:AllowedReturnOrigins`. |
| Keycloak loses all state between restarts | Expected with `start-dev` (in-memory dev DB). For persistence, swap the run command for `start` with `KC_DB=postgres`, mount a Postgres volume, and supply real DB credentials. |

## 7. Peer vs broker (recap)

- **Peer mode** — register `AddGoogleProviderFromConfiguration` AND `AddKeycloakProviderFromConfiguration`; the Lyo login chooser routes to `/auth/login/google` or `/auth/login/keycloak:lyo`. Both providers issue Lyo JWTs of the same shape; downstream APIs never need to care which IdP minted the original id_token.
- **Broker mode** — register only `AddKeycloakProviderFromConfiguration` on the Lyo side; configure Google (or any other IdP) as a *federated identity provider* inside Keycloak. Lyo only ever talks to Keycloak; Keycloak's `identity_provider` claim carries the upstream IdP name for auditing.

## 8. Promote to deployed environments

Replace localhost URLs with the deployed equivalents and add **additional** `Valid redirect URIs` on the Keycloak client (one per environment):

- Staging: `https://api-staging.example.com/auth/callback/keycloak:lyo` + `LyoOidcBff:AllowedReturnOrigins` includes `https://app-staging.example.com`
- Production: `https://api.example.com/auth/callback/keycloak:lyo` + `LyoOidcBff:AllowedReturnOrigins` includes `https://app.example.com`

Production Keycloak must terminate TLS (either directly via `KC_HTTPS_*` settings or behind a reverse proxy with `KC_PROXY=edge`/`KC_HOSTNAME=...`). The Lyo provider only requires
HTTPS in non-loopback deployments — confirm with `curl https://sso.example.com/realms/lyo/.well-known/openid-configuration` from the API host before flipping any traffic over.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication.OpenIdConnect` — (direct, lyo)
- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Authentication` — (transitive, lyo)
- `Lyo.Authentication.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party)
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