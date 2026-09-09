# Lyo.Config.Api.Models

Config HTTP API contracts: `ConfigResolveOutcome`, `ConfigResolveConditionalResult`, and `HttpStatusDescriptor`.

- `ResolvedConfigRecord` payloads reuse shared types from `Lyo.Config`. - `IConfigApiClient`, `ConfigPolling`, and the DI registration `AddConfigApiClientFromConfiguration` live in [`Lyo.Config.Api.Client`](../Lyo.Config.Api.Client) ([ `README`](../Lyo.Config.Api.Client/README.md)).

URL segment helpers that map `/api/config/{appKind}/{appId}` to `EntityRef("App", "kind:id")` sit on `AppConfigEntity` in [`Lyo.Config/AppConfigEntity.cs`](../../../Plugins/Config/Lyo.Config/AppConfigEntity.cs) (feature assembly, not tied to HTTP client packages).

Hosting integration (polling + `IOptionsMonitor<T>`) is [`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting).

HTTP endpoints are documented under [`../Lyo.Config.Api/README.md`](../Lyo.Config.Api/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Config` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)