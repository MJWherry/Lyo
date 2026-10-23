# Lyo.Web.Automation.Client

Wraps `IApiClient` for the automation routes in `Lyo.Web.Automation.Core.Constants.Rest`. The browser stays on the API host. This client lists, creates, reads, and saves plans,
then posts a run and reads status, finished steps, the transcript, viewport snapshots, and one step screenshot.

## Examples

### Wire into DI

```csharp
services.AddLyoApiClient();
services.AddAutomationPlanClient<ApiClient>();

var client = sp.GetRequiredService<IAutomationPlanClient>();
var accepted = await client.StartAsync(new AutomationRunReq {
    Plan = plan,
    Headless = true
});
```

## Prefix on routes

If `AutomationPlanClientOptions.RoutePrefix` is set, every route is `{prefix}/Automation/...`, including `Plan` and `Run`. An empty prefix keeps routes relative and uses
`HttpClient.BaseAddress`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Web.Automation` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)