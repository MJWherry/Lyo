# Lyo.Authentication.Okta

Okta SAML profile for `Lyo.Authentication.Saml`. Provider name `okta`, configuration section `OktaAuth`. This profile does not register an OpenID Connect client.

## Examples

### Register

```csharp
services.AddOktaProvider(o =>
{
    o.Domain = "lyo.okta.com";
    o.AppId = "exk123";
    o.EntityId = "https://api.example.com/saml";
    o.AcsUrl = "https://api.example.com/auth/callback/okta";
});
```

### OktaAuth

```json
{
  "OktaAuth": {
    "Domain": "lyo.okta.com",
    "AppId": "exk123",
    "EntityId": "https://api.example.com/saml",
    "AcsUrl": "https://api.example.com/auth/callback/okta",
    "EmailAttributeName": "email"
  }
}
```

## Metadata and subject

- `Domain` is a host (`{org}.okta.com`, `{org}.oktapreview.com`, or a custom domain) with no scheme and no path.
- `AppId` is the public `exk…` key from View IdP metadata, not the `0oa…` application id. The metadata URL is `https://{domain}/app/{appId}/sso/saml/metadata`.
- The subject is the Okta user id (`00u…`), configured as the NameID or as a `user.id` attribute (`SubjectAttributeName`). The email attribute defaults to `email`.
- In the Okta app, the Audience / IdP Identifier must match `EntityId` and the Single sign-on URL must match `AcsUrl`. Leave Signed requests off and assertion encryption off.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication.Saml` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Authentication` (transitive, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Authentication.Sso` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
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