# Lyo.Config.Api.Models

Thin **contracts** for calling the central Config HTTP API: **`ConfigResolveOutcome`**, **`ConfigResolveConditionalResult`**, and **`HttpStatusDescriptor`**.

- **`ResolvedConfigRecord`** payloads use shared types from **`Lyo.Config`**.
- **`IConfigApiClient`**, **`ConfigPolling`**, and DI registration **`AddConfigApiClientFromConfiguration`** live in **[`Lyo.Config.Api.Client`](../Lyo.Config.Api.Client)** ([
  `README`](../Lyo.Config.Api.Client/README.md)).

URL segment helpers mapping **`/api/config/{appKind}/{appId}`** to **`EntityRef("App", "kind:id")`** are on **`AppConfigEntity`** in *
*[`Lyo.Config/AppConfigEntity.cs`](../../../Features/Config/Lyo.Config/AppConfigEntity.cs)** (feature assembly, not tied to HTTP client packages).

Hosting integration (**polling + `IOptionsMonitor<T>`**) is **[`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting)**.

HTTP surface is documented under **[`../Lyo.Config.Api/README.md`](../Lyo.Config.Api/README.md)**.
