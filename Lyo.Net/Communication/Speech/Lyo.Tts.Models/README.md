# Lyo.Tts.Models

Shared **requests**, **results**, **options**, and **event payloads** for Lyo text-to-speech. Provider assemblies reference this package instead of coupling to each other.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Types

| Type | Purpose |
|------|---------|
| `TtsRequest` | Abstract base: `Text`, protected internal backing fields (`VoiceIdInternal`, …) for enums / formats |
| `TtsResult<TRequest>` | `Result`-based outcome with optional `AudioData`, `RequestId`, `AudioSize` |
| `TtsSynthesisResult` | Lightweight struct for [`ITtsService`](../Lyo.Tts/README.md) facades |
| `TtsServiceOptions` | Defaults (`DefaultVoiceId`, `DefaultOutputFormat`), limits (`MaxTextLength`, bulk caps), metrics toggle |
| `TtsSynthesizingEventArgs<T>` / `TtsSynthesizedEventArgs<T>` | Single-request events |
| `TtsBulkSynthesizingEventArgs<T>` / `TtsBulkSynthesizedEventArgs<T>` | Bulk events |

Implementations normally derive a typed request from `TtsRequest` (for example AWS Polly or Typecast) and keep JSON shape under their control.

## Related projects

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md) — enums (`AudioFormat`, `Sex`, …) and language records
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md) — `Result<T>` and `Error`
