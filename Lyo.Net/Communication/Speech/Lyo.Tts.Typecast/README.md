# Lyo.Tts.Typecast

Typecast TTS through [`Lyo.Typecast.Client`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md). `TypecastTtsService` synthesizes audio through `TypecastClient`, can load the voice catalog for validation (`LoadVoicesAsync`), and uses the bulk pipeline and Typecast-namespaced metrics from [`Lyo.Tts`](../Lyo.Tts/README.md).

**Target frameworks:** `netstandard2.0`, `net10.0`

## Examples

### Wire into DI

```csharp
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;

services.AddTypecastClientFromConfiguration(configuration);
services.AddTypecastTtsServiceFromConfiguration(configuration);
// Or with an inline configurator:
// services.AddTypecastTtsService(opts => { opts.DefaultVoiceId = "..."; });
```

## Prerequisites

- Set up API access with [`AddTypecastClientFromConfiguration`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md) (section `TypecastClient` by default).
- Add TTS options plus the service (`TypecastOptions` section defaults to `TypecastOptions`).

## Service registration

Both `AddTypecastTtsService` and `AddTypecastTtsServiceFromConfiguration` wire:

- `TypecastTtsService` (singleton; subclass of `TtsServiceBase<TypecastTtsRequest>`).
- `ITtsService<TypecastTtsRequest>` resolved from the singleton above.
- `TypecastTtsAppService` converts `TtsResult<TypecastTtsRequest>` into
  `TtsSynthesisResult`.
- `ITtsService` (non-generic) backed by `TypecastTtsAppService`, so consumers that only depend on the
  simple contract can resolve it directly.

`DefaultVoiceId`, `DefaultModel`, `MaxTextLength`, and bulk limits come from `TypecastOptions` (which
inherits shared fields from [`TtsServiceOptions`](../Lyo.Tts.Models/README.md)). `TypecastTtsService`
requires a registered `TypecastClient`. Register it first via `AddTypecastClientFromConfiguration` or
`AddTypecastClient` from [`Lyo.Typecast.Client`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md).

## Voice catalog checks

During startup, call `await typecastService.LoadVoicesAsync()` so `SynthesizeAsync` can verify `(model, voiceId)` pairs against the downloaded catalog; validation is skipped when voices are not loaded (see logging in `TypecastTtsService`).

## Builder overload warning

One overload is named `SynthesizeToFileAsync(TypecastTtsRequestBuilder, …)` on `TypecastTtsService` that never writes to disk. It only builds a request and returns audio bytes. Use the base class `SynthesizeToFileAsync(TypecastTtsRequest, string, …)` with `builder.Build()` when you need a file.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Tts` (direct, lyo)
- `Lyo.Typecast.Client` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Tts.Models` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)