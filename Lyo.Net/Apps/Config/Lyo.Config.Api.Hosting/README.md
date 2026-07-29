# Lyo.Config.Api.Hosting

Bridges **`IConfigApiClient`** ([`Lyo.Config.Api.Client`](../Lyo.Config.Api.Client/README.md)) into **`Microsoft.Extensions.DependencyInjection`** and *
*`Microsoft.Extensions.Options`**: a **`BackgroundService`** keeps a shared **`ResolvedConfigRecord`** ledger (ETags + **304** polling), then **one definition key JSON blob** binds
each **`IOptionsMonitor<TOptions>`**.

Prefer **`IOptionsMonitor<TOptions>.CurrentValue`** (or **`OnChange`**) for values that reload at runtime. **`IOptions<TOptions>`** is not registered here and would not observe
remote updates anyway.

## Examples

### Registration order

```csharp
using Lyo.Config.Api.Client;
using Lyo.Config.Api.Hosting;

// 1 — HTTP client (`BaseUrl`, optional `ApiKey`)
services.AddConfigApiClientFromConfiguration(configuration);

// 2 — ledger + polling (binds configuration section defaults below)
services.AddConfigApiPolling(configuration);

// 3 — one registrant per POCO keyed by Config API definition Key
services.AddConfigApiOptions<MyFeatureOptions>(
    definitionKey: "myFeature",
    missingDefinitionKeyBehavior: ConfigApiMissingDefinitionKeyBehavior.Throw);
```

### Configuration

```json
{
  "ConfigApi": {
    "BaseUrl": "http://localhost:5088/",
    "ApiKey": ""
  },
  "ConfigApiPolling": {
    "Enabled": true,
    "AppKind": "worker",
    "AppId": "image-processor",
    "DelayWhenNotModified": "00:00:15",
    "StartupTimeout": "00:05:00",
    "RequireSuccessOnStartup": true
  }
}
```

## Registration order

Reference the project `Lyo.Config.Api.Hosting` from your worker/API host (`Microsoft.Extensions.Hosting` is assumed).

## Configuration

- **`StartupTimeout`** — omit or **`null`** to wait indefinitely for the first **200**.
- **`RequireSuccessOnStartup`** — **`false`** allows the host to start after **`StartupTimeout`** even if no snapshot arrived (ledger stays empty unless you later reload manually; prefer keeping **`true`** unless you tolerate cold-start without remote config).

## Polling enabled vs disabled

`ConfigApiPollingOptions.Enabled` (defaults to `true`) gates the entire background poller:

| `Enabled` | `ConfigApiPollingHostedService` behavior | `ConfigApiResolvedLedger.Current` at startup | Behavior of `IOptionsMonitor<T>` from `AddConfigApiOptions<T>` |
| --------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `true` | `StartAsync` validates `AppKind` / `AppId` and **blocks until the first 200** (or `StartupTimeout` if set). `ExecuteAsync` then loops `ResolveForAppAsync` with the latest ETag and `DelayWhenNotModified`. Errors retry after fixed back-offs and are logged. | Populated with the first successful payload after `StartAsync` returns. | `CurrentValue` returns the bound `TOptions`. `OnChange` fires after each ledger swap. |
| `false` | `StartAsync` skips validation and the first probe; `ExecuteAsync` returns immediately. The hosted service stays in the DI container but does not touch the network. **No ledger updates from the network ever occur.** Some other code path (typically tests) may still call `ConfigApiResolvedLedger.SetResolved` manually. | `null` unless something else calls `SetResolved` directly. | Materialization sees a `null` ledger and falls back to `ConfigApiMissingDefinitionKeyBehavior`: `Throw` raises `InvalidOperationException`; `UseDefaultInstance` returns `new TOptions()`. |

> The polling service performs the **only** writes to `ConfigApiResolvedLedger` in production. With `Enabled == false`, every options monitor is effectively driven by
> `missingDefinitionKeyBehavior`. Use that mode for test hosts or for services that consume config exclusively via REST.

## `ConfigApiResolvedLedger` — the in-process resolved-config cache

[`ConfigApiResolvedLedger`](./ConfigApiResolvedLedger.cs) is the shared in-process snapshot that ties the background poller to all `IOptionsMonitor<T>` instances. It is
registered as a **singleton** by `AddConfigApiPolling` (and also `TryAddSingleton`-ed by `AddConfigApiOptions<T>`, so you can register options without polling).

| Surface | Purpose |
| --------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ResolvedConfigRecord? Current` | Latest payload. `null` until the first successful resolve. Thread-safe getter (lock-protected). |
| `string? CurrentEtag` | Opaque ETag from the last successful 200 response. Passed back to the server as `If-None-Match` on every subsequent probe. |
| `IChangeToken GetReloadToken()` | Returns a `CancellationChangeToken` invalidated on the next swap. Used by `ConfigApiOptionsMonitor<T>` to rebuild its cached `TOptions`. |
| `void SetResolved(ResolvedConfigRecord resolved, string? etag)` | Atomically updates `Current` + `CurrentEtag`, cancels the previous reload token (notifying every subscriber), and disposes the old `CancellationTokenSource`. |

`ConfigApiOptionsMonitor<T>` subscribes to `GetReloadToken` via `ChangeToken.OnChange`, so every successful ledger swap triggers re-materialization of *all* registered options
types in lock-step.

## Missing definition key behaviour

Materialization runs through `ConfigApiResolvedLedger.Current.TryGetValue(definitionKey, …)` and then `configValue.GetValue<T>(ConfigJsonSerializerOptions.Default)`. The
`ConfigApiMissingDefinitionKeyBehavior` selected at registration controls the failure modes:

| Condition | `Throw` (default) | `UseDefaultInstance` |
| ----------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- | -------------------- |
| Ledger is empty (`Current == null`). | `InvalidOperationException("No Config API snapshot is available yet (ledger empty). …")` | `new TOptions()` |
| Definition key absent from the resolved payload, or its JSON value is `null`. | `InvalidOperationException("Definition key '<key>' is missing from resolved Config API payload …")` | `new TOptions()` |
| Key present, JSON not assignable to `TOptions` (deserialize returns `null`). | `InvalidOperationException("JSON for definition key '<key>' did not deserialize to <TOptions>.")` | `new TOptions()` |

Choose `UseDefaultInstance` for features that may have no bindings yet; keep `Throw` for required configuration so misconfiguration fails fast at startup.

## Limitations

- **No named options.** `ConfigApiOptionsMonitor<T>` ignores the `name` argument on `IOptionsMonitor<T>.Get(string?)`: requests for `Options.DefaultName` (or `null` / empty) return the cached `CurrentValue`, **anything else throws `InvalidOperationException`**. There is no way to map several names onto different definition keys through this monitor.
- **One definition key per `TOptions` type per host.** `AddConfigApiOptions<TOptions>(definitionKey, …)` registers an unkeyed singleton `IOptionsMonitor<TOptions>`. Calling it twice with the same `TOptions` and different `definitionKey` values **replaces** the prior registration (last call wins); you cannot bind one POCO to two definition keys in the same host. Use distinct `TOptions` types (or sub-records) for each definition key.
- **`Enabled = false` leaves the ledger empty.** Without polling, `ConfigApiResolvedLedger.Current` stays `null` unless something else calls `SetResolved` directly. Options monitors then fall back to `ConfigApiMissingDefinitionKeyBehavior` semantics — see the table above.
- **`IOptions<T>` and `IOptionsSnapshot<T>` are not registered** by this package. Inject `IOptionsMonitor<T>` (or, in scoped consumers, `IOptionsSnapshot<T>` if you register your own adapter) so changes from the polling service propagate.

## Runtime behaviour summary

```mermaid
flowchart LR
    HS[BackgroundService_poll] --> Ledger[Resolved_ledger_ETag]
    Ledger --> Mon[OptionsMonitor_builder]
```

1. Hosted service probes **`IConfigApiClient.ResolveForAppAsync`** with **`AppKind`/`AppId`** and **`If-None-Match`** from **`ConfigApiResolvedLedger.CurrentEtag`**.
2. On **200**, the ledger swaps in the new **`ResolvedConfigRecord`** and invalidates **`IChangeToken`** listeners.
3. Each **`ConfigApiOptionsMonitor<T>`** reacts by re-materializing **`T`** via **`ResolvedConfigRecord.TryGetValue(definitionKey, …)`** + JSON deserialize (same *
   *`ConfigJsonSerializerOptions.Default`** semantics as **`GetValue<T>`** elsewhere).

REST paths remain in **[`../Lyo.Config.Api/README.md`](../Lyo.Config.Api/README.md)**. Resolve outcomes (**`ConfigResolveOutcome`**) are in *
*[`Lyo.Config.Api.Models`](../Lyo.Config.Api.Models/README.md)**; HTTP registration and **`ConfigPolling`** in **[`Lyo.Config.Api.Client`](../Lyo.Config.Api.Client/README.md)**.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Config.Api.Client` — (direct, lyo)
- `Microsoft.Extensions.Hosting` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (direct, microsoft)
- `Lyo.Api.Client` — (transitive, lyo)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Config` — (transitive, lyo)
- `Lyo.Config.Api.Models` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft)