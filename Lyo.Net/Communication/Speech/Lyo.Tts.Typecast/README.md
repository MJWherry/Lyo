# Lyo.Tts.Typecast

Typecast TTS via [`Lyo.Typecast.Client`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md). `TypecastTtsService` synthesizes audio through `TypecastClient`, can load the voice catalog for validation (`LoadVoicesAsync`), and uses the bulk pipeline and Typecast-namespaced metrics from [`Lyo.Tts`](../Lyo.Tts/README.md).

**Target frameworks:** `netstandard2.0`, `net10.0`

## Examples

### Register with DI

```csharp
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;

services.AddTypecastClientFromConfiguration(configuration);
services.AddTypecastTtsServiceFromConfiguration(configuration);
// Or with an inline configurator:
// services.AddTypecastTtsService(opts => { opts.DefaultVoiceId = "..."; });
```

## Prerequisites

- Configure API access with [`AddTypecastClientFromConfiguration`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md) (section `TypecastClient` by default).
- Add TTS options and the service (`TypecastOptions` section defaults to `TypecastOptions`).

## Dependency injection

Both `AddTypecastTtsService` and `AddTypecastTtsServiceFromConfiguration` register:

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

## Voices and validation

Call `await typecastService.LoadVoicesAsync()` during startup so `SynthesizeAsync` can verify `(model, voiceId)` pairs against the downloaded catalog; if voices are not loaded, validation is skipped (see logging in `TypecastTtsService`).

## Builder overload caveat

There is an overload named `SynthesizeToFileAsync(TypecastTtsRequestBuilder, …)` on `TypecastTtsService` that does not write to disk. It only builds a request and returns audio bytes. Use the base class `SynthesizeToFileAsync(TypecastTtsRequest, string, …)` with `builder.Build()` when you need a file.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Tts` (direct, lyo)
- `Lyo.Typecast.Client` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Tts.Models` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)