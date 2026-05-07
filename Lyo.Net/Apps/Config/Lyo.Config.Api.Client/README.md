# Lyo.Config.Api.Client

Typed HTTP client for **`/api/config/{appKind}/{appId}`**: **`IConfigApiClient`**, **`ConfigPolling`**, and **`AddConfigApiClientFromConfiguration`** (bind section **`ConfigApi`**
via [`ConfigApiClientOptions`](./ConfigApiClientOptions.cs)).

References **[`Lyo.Config.Api.Models`](../Lyo.Config.Api.Models)** for **`ConfigResolveConditionalResult`** / **`ConfigResolveOutcome`**, and **`Lyo.Config`** for *
*`AppConfigEntity`** and **`ResolvedConfigRecord`**.

See **[`Lyo.Config.Api.Models/README.md`](../Lyo.Config.Api.Models/README.md)** (contracts) and **[`../Lyo.Config.Api/README.md`](../Lyo.Config.Api/README.md)** (routing). Optional
**[`Lyo.Config.Api.Hosting`](../Lyo.Config.Api.Hosting/README.md)** wraps polling + **`IOptionsMonitor<T>`**.

## Quick start

Reference `Lyo.Config.Api.Client`.

```csharp
using Lyo.Config.Api.Client;

services.AddConfigApiClientFromConfiguration(configuration);

var client = provider.GetRequiredService<IConfigApiClient>();
var resolved = await client.ResolveForAppAsync("api", "checkout", cancellationToken: ct);
```

## `appsettings`

```json
{
  "ConfigApi": {
    "BaseUrl": "https://config.internal.example/",
    "ApiKey": "optional-shared-secret",
    "PollInterval": "00:01:30"
  }
}
```

**`PollInterval`** is informational for your app unless you consume it manually; **`ConfigPolling.PollUntilChangedAsync`** passes an explicit **`TimeSpan`**.
