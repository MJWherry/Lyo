# Lyo.Authentication.Sso

Protocol-neutral SSO host. OpenID Connect and SAML register an `ISsoProtocol` on `SsoProtocolRegistry`. This package owns the browser handoff, the `/auth/*` endpoints, and the user link plus JWT mint. It does not speak either protocol.

## Examples

### Map the endpoints

```csharp
using Lyo.Authentication.Sso.Endpoints;

services.AddLyoSso(builder.Configuration);
services.AddLyoOpenIdConnect();
app.MapLyoAuthEndpoints();
```

### LyoSso

```json
{
  "LyoSso": {
    "AllowedReturnOrigins": [ "https://app.example.com" ],
    "DefaultReturnUrl": "/",
    "HandoffCodeTtl": "00:00:30"
  },
  "LyoExternalLogin": {
    "Policy": "JustInTime",
    "RequireVerifiedEmail": true
  }
}
```

## Endpoints

- `GET /auth/login/{provider}` starts the protocol registered for that name. A non-empty `returnUrl` that fails `IsAllowedReturnUrl` is 400 `InvalidRequest` before `StartAsync`. The error names `LyoSso:AllowedReturnOrigins`.
- `GET /auth/callback/{provider}` is the OpenID Connect path. The state cookie is `lyo_oidc_state` (HttpOnly, essential, SameSite=Lax, 15 minutes). It is set only when the protocol returns a sealed value.
- `POST /auth/callback/{provider}` is the SAML HTTP-POST path. It calls `DisableAntiforgery`. The form fields are `SAMLResponse` and `RelayState`. A GET to a SAML provider, or a POST to an OpenID Connect provider, is rejected before the body is read.
- `POST /auth/handoff/exchange` consumes the `lyo_handoff` code. `IssuedTo` is the origin of the return URL stored at login start, or the API origin when that URL is relative. The callback request origin is never `IssuedTo`.
- `mode=api` returns token JSON and writes no handoff code. That branch is for OpenID Connect. SAML completion always uses the browser handoff.

## Options

`AddLyoSso(IConfiguration)` binds `LyoExternalLogin` and `LyoSso`. `AddLyoOpenIdConnect(IConfiguration)` does not bind these options itself.

## Completion

`IExternalLoginCompletion` links the provider subject and mints one Lyo JWT. Provider scopes are stored on the link and are not copied onto the JWT. `JitFromAllowedClaim` matches a string or any entry of an `IReadOnlyList<string>`. A rejected login is 401, writes no handoff, and does not redirect to the return URL.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Authentication` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)