# Lyo.Diagnostic.AspNetCore

ASP.NET Core integration for **Lyo.Diagnostic**: scoped **breadcrumb** trails per request and **exception recording** to the in-memory error inbox plus structured logging, without replacing your existing problem-details middleware.

## Examples

### Installation

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

### Breadcrumbs in a controller

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

## Pipeline ordering

- This middleware sits **closer to route handlers** and runs **first when an exception unwinds**.
- It **records** the failure and calls **`IStructuredLogEnricher`**, then **rethrows**.
- Outer middleware can still build **Problem Details** / HTTP error bodies.

## Options (`DiagnosticWebOptions`)

| Property | Purpose |
| ----------------------------- | -------------------------------------------------------------------------------------------- |
| `MinimumSeverity` | Only occurrences at or above this severity are written to the inbox. |
| `RecordExpectedControlFlow` | When `false`, expected control-flow classifications are skipped. |
| `BreadcrumbCapacity` | Max breadcrumbs per request (`IBreadcrumbTrail`). |
| `InMemoryInboxMaxOccurrences` | Cap for the singleton `InMemoryErrorInbox`. |
| `CorrelationIdHeaders` | Headers tried in order for `RequestMetadata.CorrelationId`; falls back to `TraceIdentifier`. |

## Breadcrumbs in a controller

Inject `IBreadcrumbTrail` (scoped) and add entries before risky work:

Do **not** put secrets or PII in breadcrumb data. See the **`Lyo.Diagnostic`** README.

## Lyo.Api

`Lyo.Api` exposes **`AddLyoApiDiagnosticRecording`** as a convenience wrapper around **`AddLyoDiagnosticsWeb`**.