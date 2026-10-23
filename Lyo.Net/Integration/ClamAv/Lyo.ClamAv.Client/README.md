# Lyo.ClamAv.Client

HTTP client for a self-hosted [clamav-rest](https://github.com/solita/clamav-rest) service. `ClamAvClient` subclasses `LyoHttpClient`.

**Archetype C (vendor client).** The host points `BaseUrl` at the service (for example `http://127.0.0.1:8080/`). This package does not talk to clamd's INSTREAM socket.

`ScanAsync` posts the stream as multipart field `file`. A body status of `OK` is clean. `FOUND` is infected and `description` is the signature. clamav-rest returns HTTP 406 for a hit; that response is still a verdict. Any other non-success status throws `LyoHttpException`. `GetVersionAsync` reads the plain-text `version` endpoint.

## Features

- **Scan.** `POST {BaseUrl}/scan` via `PostFileAsAsync`. The part name is `file`.
- **Verdicts.** `OK` → clean. `FOUND` → infected, signature from `description`. Any other JSON status → error. HTTP 406 is read as a verdict.
- **Version.** `GET {BaseUrl}/version` returns the trimmed engine version string.
- **Registration.** `AddClamAvClient`, the options instance overload, and `AddClamAvClientFromConfiguration`. Also registers transient `IClamAvClient`.

## Examples

### Register

```csharp
services.AddClamAvClient(o => o.BaseUrl = "http://127.0.0.1:8080/");

services.AddClamAvClientFromConfiguration(configuration);
```

### Scan a stream

```csharp
var result = await client.ScanAsync(stream, "upload.bin", ct);
if (result.Status == ClamScanStatus.Infected)
    Console.WriteLine(result.Signature);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Http.Client` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.RateLimit` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)