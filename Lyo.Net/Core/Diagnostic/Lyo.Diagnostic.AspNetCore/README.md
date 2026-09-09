# Lyo.Diagnostic.AspNetCore

ASP.NET Core integration for `Lyo.Diagnostic`. Per-request scoped breadcrumb trails and exception recording to the in-memory error inbox plus structured logging, without replacing existing problem-details middleware.

## Examples

### Install

```csharp
using Lyo.Diagnostic.AspNetCore;

builder.Services.AddLyoDiagnosticsWeb(o =>
{
    o.MinimumSeverity = Lyo.Diagnostic.Classification.ExceptionSeverity.Low;
    o.BreadcrumbCapacity = 100;
    o.InMemoryInboxMaxOccurrences = 5_000;
    o.CorrelationIdHeaders = ["X-Correlation-Id", "X-Request-Id"];
});

// After outer middleware that catches and formats errors (e.g. LoggingMiddleware):
app.UseDiagnosticExceptionRecording();
```

### Controller breadcrumbs

```csharp
public sealed class OrdersController(IBreadcrumbTrail breadcrumbs)
{
    public async Task<IActionResult> Get(string id)
    {
        breadcrumbs.Add("orders", "Loading order", new Dictionary<string, string> { ["id"] = id });
        // ...
    }
}
```

## Order in the pipeline

- Placed nearer the route handlers, this middleware is first in line when an exception unwinds.
- It records the failure, calls `IStructuredLogEnricher`, then rethrows.
- Problem Details / HTTP error bodies can still be built by outer middleware.

## Options (`DiagnosticWebOptions`)

| Property | What it does |
| ----------------------------- | -------------------------------------------------------------------------------------- |
| `MinimumSeverity` | The inbox only stores occurrences at this severity or higher. |
| `RecordExpectedControlFlow` | Expected control-flow classifications are skipped when this is `false`. |
| `BreadcrumbCapacity` | Per-request breadcrumb ceiling (`IBreadcrumbTrail`). |
| `InMemoryInboxMaxOccurrences` | Upper bound for the singleton `InMemoryErrorInbox`. |
| `CorrelationIdHeaders` | Tried in order for `RequestMetadata.CorrelationId`; `TraceIdentifier` is the fallback. |

## Controller breadcrumbs

Resolve scoped `IBreadcrumbTrail` and record entries before risky work:

Keep secrets and PII out of breadcrumb data. See the `Lyo.Diagnostic` README.

## Lyo.Api

`AddLyoApiDiagnosticRecording` on `Lyo.Api` wraps `AddLyoDiagnosticsWeb`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Diagnostic` (direct, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)