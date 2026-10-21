# Lyo.Authentication.Entra

Microsoft Entra SAML profile for `Lyo.Authentication.Saml`. Provider name `entra`, configuration section `EntraAuth`. This profile does not register an OpenID Connect client.

## Examples

### Register

```csharp
services.AddEntraProvider(o =>
{
    o.TenantId = "tenant-guid";
    o.ApplicationId = "app-guid";
    o.EntityId = "https://api.example.com/saml";
    o.AcsUrl = "https://api.example.com/auth/callback/entra";
});
```

## Metadata and subject

- The metadata URL is `https://login.microsoftonline.com/{tenantId}/federationmetadata/2007-06/federationmetadata.xml?appid={applicationId}`. The `appid` query selects that app's
  token-signing certificate.
- The expected issuer is `https://sts.windows.net/{tenantId}/` with the trailing slash.
- The subject is `http://schemas.microsoft.com/identity/claims/objectidentifier` when that attribute is present. The email attribute is exactly
  `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`.
- In the Entra enterprise app, the Identifier must match `EntityId` and the Reply URL must match `AcsUrl`. Leave request verification off and assertion encryption off.

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