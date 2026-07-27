# Lyo.Tts

Contracts and shared behaviour for text-to-speech in Lyo: provider-agnostic interfaces, a non-generic
façade, and a base service with bulk synthesis, metrics hooks, and lifecycle events.

**Target frameworks:** `netstandard2.0;net10.0`

## Contracts at a glance

| Interface                                             | Surface                                                                                                                               |
|-------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| `ITtsService`                                         | Non-generic façade — `Task<TtsSynthesisResult> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)`. |
| `ITtsService<TRequest>` where `TRequest : TtsRequest` | Full provider surface: string overload, fully typed request, write-to-file, write-to-stream, bulk, `TestConnectionAsync`.             |

`ITtsService` is intentionally tiny — host code that only needs "string in → audio bytes out" depends on
it without caring which provider is wired up. Provider packages ship a small `*TtsAppService` adapter
(holding the typed service) and register `ITtsService` via that adapter alongside `ITtsService<TRequest>`.

## Provider DI matrix

| Provider package                                              | `ITtsService<TRequest>`             | `ITtsService` (non-generic)                   | App-service adapter     | DI entry points                                                                                                                                                                 |
|---------------------------------------------------------------|-------------------------------------|-----------------------------------------------|-------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md)           | `ITtsService<AwsPollyTtsRequest>` ✅ | ✅ (via `AwsPollyTtsAppService`)               | `AwsPollyTtsAppService` | `AddAwsPollyTtsService(Action<AwsPollyOptions>)`, `AddAwsPollyTtsServiceFromConfiguration(IConfiguration, string?)`, `AddAmazonPollyFromConfiguration(IConfiguration, string?)` |
| [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md)           | `ITtsService<TypecastTtsRequest>` ✅ | ✅ (via `TypecastTtsAppService`)               | `TypecastTtsAppService` | `AddTypecastTtsService(Action<TypecastOptions>?)`, `AddTypecastTtsServiceFromConfiguration(IConfiguration, string?)` (requires `TypecastClient` to be registered first)         |
| [`Lyo.Tts.WindowsSpeech`](../Lyo.Tts.WindowsSpeech/README.md) | `ITtsService<WindowsTtsRequest>` ✅  | ❌ — no `WindowsSpeechTtsAppService` ships yet | (none)                  | `AddWindowsSpeechTtsService(Action<TtsServiceOptions>?)`, `AddWindowsSpeechTtsService(TtsServiceOptions)`                                                                       |

`AwsPolly` and `Typecast` register **both** `ITtsService<TRequest>` and the non-generic `ITtsService`; the
non-generic interface is backed by the `*TtsAppService` adapter so the same singleton handles both calls.
`WindowsSpeech` currently registers only the typed interface — code that depends on the non-generic
`ITtsService` will not resolve when WindowsSpeech is the only TTS provider registered.

## Request types

Each provider ships its own `TtsRequest` subclass (see `Lyo.Tts.Models.TtsRequest`):

- `AwsPollyTtsRequest` — Polly voice, engine, sample rate, output format, etc.
- `TypecastTtsRequest` — model id, voice id, language, format, prosody.
- `WindowsTtsRequest` — voice id, `Volume`, `SpeechRate`, optional `OutputFormat` (WAV output is enforced
  by SAPI regardless).

## Base class (`TtsServiceBase<TRequest>`)

Provider services subclass `TtsServiceBase<TRequest>` and implement two abstract members:

- `Task<TtsResult<TRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)`
- `Task<TtsResult<TRequest>> SynthesizeCoreAsync(TRequest request, CancellationToken ct = default)`
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`

The base provides:

- `SynthesizeToFileAsync` / `SynthesizeToStreamAsync` overloads (`string` and `TRequest`).
- `SynthesizeBulkAsync` with a `SemaphoreSlim` sized by `TtsServiceOptions.BulkTtsConcurrencyLimit` and
  per-call enforcement of `TtsServiceOptions.MaxBulkTtsLimit`.
- Synthesis events (`Synthesizing`, `Synthesized`, `BulkSynthesizing`, `BulkSynthesized`).
- A `MetricNames` `ConcurrentDictionary` providers swap in their constructor to namespace metrics.

## Error codes (`TtsErrorCodes`)

| Constant             | Value                     |
|----------------------|---------------------------|
| `SynthesizeFailed`   | `TTS_SYNTHESIZE_FAILED`   |
| `OperationCancelled` | `TTS_OPERATION_CANCELLED` |
| `FileWriteFailed`    | `TTS_FILE_WRITE_FAILED`   |
| `StreamWriteFailed`  | `TTS_STREAM_WRITE_FAILED` |

## Default metric keys (`Lyo.Tts.Constants.Metrics`)

| Constant key                   | Metric                                 | Kind    |
|--------------------------------|----------------------------------------|---------|
| `SynthesizeDuration`           | `tts.synthesize.duration`              | Timer   |
| `SynthesizeSuccess`            | `tts.synthesize.success`               | Counter |
| `SynthesizeFailure`            | `tts.synthesize.failure`               | Counter |
| `BulkSynthesizeDuration`       | `tts.bulk.synthesize.duration`         | Timer   |
| `BulkSynthesizeTotal`          | `tts.bulk.synthesize.total`            | Counter |
| `BulkSynthesizeSuccess`        | `tts.bulk.synthesize.success`          | Counter |
| `BulkSynthesizeFailure`        | `tts.bulk.synthesize.failure`          | Counter |
| `BulkSynthesizeLastDurationMs` | `tts.bulk.synthesize.last_duration_ms` | Gauge   |

Providers typically remap these to a namespaced prefix (`tts.awspolly.*`, `tts.typecast.*`,
`tts.windowsspeech.*`).

## Related projects

- [`Lyo.Tts.Models`](../Lyo.Tts.Models/README.md) — requests, results, options, events
- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
