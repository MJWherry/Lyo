# Lyo.Idempotency.AspNetCore

UseIdempotency runs after routing so Endpoint.Metadata includes RequireIdempotencyKey on a controller, an action, or a minimal API. Safe methods ignore the header. The scope is api|{subject}|{method}|{path}. Subject is the lyo:user claim, otherwise sub.

## Features

- **[RequireIdempotencyKey]** on a controller applies to every action. A marked endpoint with no header returns 400.
- **Same key, different body is 409.** The hash is the raw query plus the raw body. Replay writes the stored status and Idempotency-Replayed: true.

## Examples

### Controller and minimal API

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseIdempotency();

[RequireIdempotencyKey]
[HttpPost("/pay")]
public IActionResult Pay() => Ok();

app.MapPost("/pay", Pay).WithMetadata(new RequireIdempotencyKeyAttribute());
```

## Headers

The request headers are Idempotency-Key and X-Idempotency-Key. A missing subject with a present header returns 400 idempotency_subject_required. GET, HEAD, OPTIONS, and TRACE ignore the header.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Idempotency` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)