# Lyo.Config.Api.Models

Contracts for the Config HTTP API: `ConfigResolveOutcome`, `ConfigResolveConditionalResult`, and `HttpStatusDescriptor`.

- `ResolvedConfigRecord` payloads use shared types from `Lyo.Config`. - `IConfigApiClient`, `ConfigPolling`, and DI registration `AddConfigApiClientFromConfiguration` live in [`Lyo.Config.Api.Client`](../Lyo.Config.Api.Client) ([ `README`](../Lyo.Config.Api.Client/README.md)).

URL segment helpers mapping `/api/config/{appKind}/{appId}` to `EntityRef("App", "kind:id")` are on `AppConfigEntity` in [`Lyo.Config/AppConfigEntity.cs`](../../../Features/Config/Lyo.Config/AppConfigEntity.cs) (feature assembly, not tied to HTTP client packages).

Hosting integration (polling + `IOptionsMonitor<T>`) is [`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting).

HTTP endpoints are documented under [`../Lyo.Config.Api/README.md`](../Lyo.Config.Api/README.md).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Config` (direct, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)