# Lyo.Tts.Typecast

[Lyo.Typecast.Client](../../../Integration/Typecast/Lyo.Typecast.Client/README.md)–backed synthesis: `TypecastTtsService` resolves audio via `TypecastClient`, supports optional voice catalog loading for validation (`LoadVoicesAsync`), bulk flows from [`Lyo.Tts`](../Lyo.Tts/README.md), and Typecast-namespaced metrics.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Prerequisites

1. Configure API access with [`AddTypecastClientFromConfiguration`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md) (section `TypecastClient` by default).
2. Add TTS options and the service (`TypecastOptions` section defaults to `TypecastOptions`).

## Dependency injection

```csharp
using Lyo.Tts.Typecast;
using Lyo.Typecast.Client;

services.AddTypecastClientFromConfiguration(configuration);
services.AddTypecastTtsServiceFromConfiguration(configuration);
```

`DefaultVoiceId`, `DefaultModel`, `MaxTextLength`, and bulk limits come from `TypecastOptions` (which inherits shared fields from [`TtsServiceOptions`](../Lyo.Tts.Models/README.md)).

## Voices and validation

Call `await typecastService.LoadVoicesAsync()` during startup so `SynthesizeAsync` can verify `(model, voiceId)` pairs against the downloaded catalog; if voices are not loaded, validation is skipped (see logging in `TypecastTtsService`).

## Builder overload caveat

There is an overload named `SynthesizeToFileAsync(TypecastTtsRequestBuilder, …)` on `TypecastTtsService` that **does not write to disk**—it only builds a request and returns audio bytes. Use the base class `SynthesizeToFileAsync(TypecastTtsRequest, string, …)` with `builder.Build()` when you need a file.

## Dependencies

*(Aligned with [`Lyo.Tts.Typecast.csproj`](Lyo.Tts.Typecast.csproj).)*

### NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `System.Text.Json` | `[10,)` (netstandard2.0) |

### Project references

- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Tts`](../Lyo.Tts/README.md)
- [`Lyo.Typecast.Client`](../../../Integration/Typecast/Lyo.Typecast.Client/README.md)
