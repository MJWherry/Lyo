# Lyo.Authentication.Models

Wire-shape data for `Lyo.Authentication` — the half of the auth stack that's safe to ship to anyone, including Blazor WebAssembly clients. No services, no stores, no key material, no DI; just records, format helpers, and a JWT parser.

## What's in it

```
Records/
  LyoUser, LinkedIdentity                                  # /auth/me payload shapes
  IssuedLyoJwt, IssuedApiToken                             # issuer outputs
  ApiTokenRecord, ApiTokenPrincipal, ApiTokenIssueRequest  # token-store / issuer shapes
  LyoJwtClaims                                             # canonical claim-name constants ("lyo:uid", "lyo:scope", ...)
  LyoJwtClaimsParser                                       # parse a JWT payload into IReadOnlyList<Claim>
Format/
  Base64Url                                                # RFC 4648 §5, no padding (JWT segments + Format-B secrets)
  Base32Crockford                                          # 11-char ids for Format-B tokens
  ApiToken                                                 # parsed lyo_kind_ring_id_secret record
  ApiTokenKind, ApiTokenRing                               # well-known segment values (pat / svc / cli / live / test / dev / …)
Audit/
  AuthAuditEvent, AuthAuditEventKind                       # closed taxonomy of audit-worthy moments
Scopes/
  Scope                                                    # single registered authorization scope record
```

> The minting/hashing parts of `ApiTokenCodec`, every `IApiTokenStore` / `IUserStore` / `ILyoJwtIssuer` / `ILyoJwtValidator`, the scope registry runtime, and the audit recorder live in **`Lyo.Authentication`** — not here. They depend on `Lyo.Keystore`, `Lyo.Hashing`, and BouncyCastle, which have no business in a browser bundle.

## Dependencies

Only `Lyo.Exceptions` (for `ArgumentHelpers`). On `netstandard2.0` an extra `System.Text.Json` package reference is pulled in for `LyoJwtClaimsParser`; `net10.0` consumes the BCL build of `System.Text.Json` directly.

Multi-targets `netstandard2.0;net10.0`.

## When to reference it

- A Blazor WebAssembly app that needs to decode a Lyo JWT, render a `LyoUser`, or describe an audit event.
- A non-Lyo client (e.g. a serverless function, a script, a CLI) that consumes the Lyo API and wants strongly-typed wire shapes without dragging in the server stack.
- A shared library that sits between the API and a consumer — e.g. `Lyo.Authentication.Client`, `Lyo.Authentication.Web.Components`.

## When NOT to reference it

- If you are the API/auth-server host, reference `Lyo.Authentication` directly. It transitively pulls Models in for you, and you'll need the service interfaces and DI helpers that only live in the server package.

## Verifying the boundary

The intent of the split is that consumer-side projects can never accidentally compile against a server-only type. Quick way to assert this in CI for any consumer csproj:

```csharp
// _ServiceBleedProbe.cs (temporary)
namespace MyApp._Probe;

internal static class ServiceBleedProbe
{
    public static object? Bleed() => typeof(Lyo.Authentication.Services.Jwt.Ed25519LyoJwtIssuer);
}
```

If `dotnet build` reports `CS0234: The type or namespace name 'Services' does not exist in the namespace 'Lyo.Authentication'`, the boundary holds. Delete the probe.
