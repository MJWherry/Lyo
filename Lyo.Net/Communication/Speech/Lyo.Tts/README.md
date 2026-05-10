# Lyo.Tts

Contracts and shared behaviour for text-to-speech in Lyo: **provider-agnostic interfaces**, an optional **non-generic façade**, a **base service** with bulk synthesis, metrics hooks, and lifecycle.

**Target frameworks:** `netstandard2.0`, `net10.0`

## What to use when

| Situation | Use |
|-----------|-----|
| Typed provider request (Polly, Typecast, …) | `ITtsService<TRequest>` and a concrete `TtsServiceBase<TRequest>` implementation |
| Single registered backend, minimal surface | `ITtsService` (implemented by *AppService* types in provider packages) |

## Main types

| Type | Role |
|------|------|
| `ITtsService` | `SynthesizeAsync` returning `TtsSynthesisResult` from [`Lyo.Tts.Models`](../Lyo.Tts.Models/README.md) |
| `ITtsService<TRequest>` | Full API: string/request synthesis, file/stream output, bulk, `TestConnectionAsync` |
| `TtsServiceBase<TRequest>` | Default bulk implementation, events, metrics; subclass implements `SynthesizeCoreAsync` |
| `TtsErrorCodes` | Stable string codes surfaced on failures |
| `Constants.Metrics` | Default metric keys (providers usually remap to namespaced constants) |

## Related projects

- [`Lyo.Tts.Models`](../Lyo.Tts.Models/README.md) — requests, results, options, events
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
