# Lyo.Tts.Models

Shared **requests**, **results**, **options**, and **event payloads** for Lyo text-to-speech. Provider assemblies reference this package instead of coupling to each other.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Types

| Type | Purpose |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `TtsRequest` | Abstract base: `Text`, protected internal backing fields (`VoiceIdInternal`, …) for enums / formats |
| `TtsResult<TRequest>` | `Result`-based outcome with optional `AudioData`, `RequestId`, `AudioSize` |
| `TtsSynthesisResult` | Lightweight struct for [`ITtsService`](../Lyo.Tts/README.md) facades |
| `TtsServiceOptions` | Defaults (`DefaultVoiceId`, `DefaultOutputFormat`), limits (`MaxTextLength`, bulk caps), metrics toggle |
| `TtsSynthesizingEventArgs<T>` / `TtsSynthesizedEventArgs<T>` | Single-request events |
| `TtsBulkSynthesizingEventArgs<T>` / `TtsBulkSynthesizedEventArgs<T>` | Bulk events |

Implementations normally derive a typed request from `TtsRequest` (for example AWS Polly or Typecast) and keep JSON shape under their control.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)