# Lyo.Authentication.Models

Wire-shape data for `Lyo.Authentication`. The half of the auth stack that's safe to ship to anyone, including Blazor WebAssembly clients. No services, no stores, no key material, no DI. Just records, format helpers, and a JWT parser.

## Examples

### What's in it

```csharp
Records/
  LyoUser, LinkedIdentity # /auth/me payload shapes
  IssuedLyoJwt, IssuedApiToken # issuer outputs
  ApiTokenRecord, ApiTokenPrincipal, ApiTokenIssueRequest # token-store / issuer shapes
  LyoJwtClaims # canonical claim-name constants ("lyo:uid", "lyo:scope", ...)
  LyoJwtClaimsParser # parse a JWT payload into IReadOnlyList<Claim>
Format/
  Base64Url # RFC 4648 §5, no padding (JWT segments + Format-B secrets)
  Base32Crockford # 11-char ids for Format-B tokens
  ApiToken # parsed lyo_kind_ring_id_secret record
  ApiTokenKind, ApiTokenRing # well-known segment values (pat / svc / cli / live / test / dev / …)
Audit/
  AuthAuditEvent, AuthAuditEventKind # closed taxonomy of audit-worthy moments
Scopes/
  Scope # single registered authorization scope record
```

### Verifying the boundary

```csharp
// _ServiceBleedProbe.cs (temporary)
namespace MyApp._Probe;

internal static class ServiceBleedProbe
{
    public static object? Bleed() => typeof(Lyo.Authentication.Services.Jwt.Ed25519LyoJwtIssuer);
}
```

## What's in it

> The minting/hashing parts of `ApiTokenCodec`, every `IApiTokenStore` / `IUserStore` / `ILyoJwtIssuer` / `ILyoJwtValidator`, the scope registry runtime, and the audit recorder live in `Lyo.Authentication`, not here. They depend on `Lyo.KeyStore`, `Lyo.Hashing`, and BouncyCastle, which have no business in a browser bundle.

## When to reference it

- A Blazor WebAssembly app that needs to decode a Lyo JWT, render a `LyoUser`, or describe an audit event.
- A non-Lyo client (e.g. a serverless function, a script, a CLI) that consumes the Lyo API and wants strongly-typed wire shapes without dragging in the server stack.
- A shared library that sits between the API and a consumer, for example `Lyo.Authentication.Client` or `Lyo.Authentication.Web.Components`.

## When not to reference it

- If you are the API/auth-server host, reference `Lyo.Authentication` directly. It transitively pulls Models in for you, and you'll need the service interfaces and DI helpers that only live in the server package.

## Verifying the boundary

Consumer-side projects must not compile against a server-only type. Assert that in CI for any consumer csproj:

If `dotnet build` reports `CS0234: The type or namespace name 'Services' does not exist in the namespace 'Lyo.Authentication'`, the boundary holds. Delete the probe.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)