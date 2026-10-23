# Lyo.RequestSigning.AspNetCore

Reads the body, checks Lyo-Signature, and returns 401 when the signature is missing, malformed, stale, or does not match. A valid signature calls the next middleware.

## Features

- **UseRequestSignature checks the header before the endpoint runs.** The body stream is rewound so the endpoint can still read it.

## Examples

### Pipeline

```csharp
app.UseRequestSignature();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.RequestSigning` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)