# Lyo.Authentication.Saml

SAML 2.0 service provider for `Lyo.Authentication.Sso`. Lyo is the service provider and still mints its own tokens after a successful login. There is no Lyo-as-IdP, no single logout, and no signed AuthnRequest. Assertion encryption stays off.

## Examples

### Register the protocol

```csharp
services.AddLyoSaml(builder.Configuration);
services.AddOktaProviderFromConfiguration(builder.Configuration);
services.AddEntraProviderFromConfiguration(builder.Configuration);
app.MapLyoAuthEndpoints();
```

## Login

- The AuthnRequest is written with an XML writer. It carries `ID`, `IssueInstant`, `Issuer` (the SP entity id), `Destination` (the metadata `SingleSignOnService` whose binding is HTTP-Redirect), `AssertionConsumerServiceURL`, and `ProtocolBinding` of HTTP-POST.
- `SAMLRequest` is raw RFC 1951 DEFLATE, then standard base64, then URI-encoded. The POST body is base64 only and is never inflated.
- SP-initiated login stores a single-use in-memory ticket and puts the ticket id in `RelayState`. The id is at most 80 bytes. A POST with no matching ticket is rejected, which also drops IdP-initiated login. The ticket is removed on the first POST, including when validation later fails.

## Assertion checks

- `EncryptedAssertion` and `EncryptedID` reject with `SamlEncrypted` before NameID or attributes are read.
- Exactly one `Assertion`. The signature reference must be that assertion's `ID`, or the `Response` `ID` when the response signature covers it. `GetIdElement` returns only that element.
- Transforms are enveloped-signature plus exclusive canonicalization without comments. SHA-1, HMAC, inclusive canonicalization, XPath, and XSLT are rejected.
- Trust is byte or SHA-256 thumbprint equality with a metadata `KeyDescriptor` whose `use` is `signing` or omitted. `use="encryption"` is ignored. `KeyInfo` alone is not trust.
- Status must be Success. Both issuers must equal the IdP `entityID`. `InResponseTo` must equal the stored AuthnRequest id. Every audience restriction must contain the SP entity id. Destination and bearer recipient must equal the ACS URL. Transient NameIDs are rejected.
- `EmailVerified` is true only for the one email attribute the profile maps.

## Metadata

Metadata fetch is HTTPS only, refuses a redirect to another host, and caps the body. Signing certificates are read from `IDPSSODescriptor`, not `SPSSODescriptor`. A failed fetch does not replace the last good entry.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication` (direct, lyo)
- `Lyo.Authentication.Sso` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
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