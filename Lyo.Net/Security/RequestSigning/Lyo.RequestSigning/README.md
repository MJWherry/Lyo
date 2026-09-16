# Lyo.RequestSigning

Header Lyo-Signature is t=<unix seconds>,v1=<lowercase hex HMAC-SHA256>. v1 is the canonicalization scheme. Sign with Key. Verify accepts Key and, when set, PreviousKey. The default max age is 5 minutes. A timestamp exactly on the boundary is still valid.

## Features

- **Canonical lines are v1, the unix time, the method, the scheme, the host, the path and query, and the body hash.** The host is the host the request reports. No port is added or stripped.
- **A DelegatingHandler adds the header on outbound HttpRequestMessage.** The HMAC key is the UTF-8 bytes of Key.

## Examples

### Register

```csharp
services.AddRequestSigning(o => o.Key = configuration["RequestSigning:Key"]!);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Microsoft.Extensions.Configuration` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)