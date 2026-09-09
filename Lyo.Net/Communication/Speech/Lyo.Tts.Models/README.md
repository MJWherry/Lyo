# Lyo.Tts.Models

Shared TTS requests, results, options, and event payloads. Provider packages depend on this package instead of on each other.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Types

| Type | Purpose |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `TtsRequest` | Abstract base: `Text`, plus protected internal backing fields (`VoiceIdInternal`, …) for enums / formats |
| `TtsResult<TRequest>` | `Result`-based outcome that may include `AudioData`, `RequestId`, `AudioSize` |
| `TtsSynthesisResult` | Small struct used by [`ITtsService`](../Lyo.Tts/README.md) facades |
| `TtsServiceOptions` | Defaults (`DefaultVoiceId`, `DefaultOutputFormat`), limits (`MaxTextLength`, bulk caps), and a metrics toggle |
| `TtsSynthesizingEventArgs<T>` / `TtsSynthesizedEventArgs<T>` | Per-request events |
| `TtsBulkSynthesizingEventArgs<T>` / `TtsBulkSynthesizedEventArgs<T>` | Batch events |

Implementations usually derive a typed request from `TtsRequest` (for example AWS Polly or Typecast) and keep the JSON shape under their control.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)