# Lyo.Tts

Contracts and shared TTS behavior: provider-agnostic interfaces, a non-generic facade, and a base service with bulk synthesis, metrics, and lifecycle events.

**Target frameworks:** `netstandard2.0;net10.0`

## Contracts

| Interface | Methods |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `ITtsService` | Non-generic facade. `Task<TtsSynthesisResult> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)`. |
| `ITtsService<TRequest>` where `TRequest : TtsRequest` | Typed methods: string overload, fully typed request, write-to-file, write-to-stream, bulk, `TestConnectionAsync`. |

`ITtsService` is intentionally tiny. Host code that only needs string-in, audio-bytes-out depends on
it without caring which provider is registered. Provider packages ship a small `*TtsAppService` adapter
(holding the typed service) and register `ITtsService` via that adapter alongside `ITtsService<TRequest>`.

## Provider DI matrix

| Provider package | `ITtsService<TRequest>` | `ITtsService` (non-generic) | App-service adapter | DI entry points |
| ------------------------------------------------------------- | --------------------------------- | ----------------------------------------- | ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md) | `ITtsService<AwsPollyTtsRequest>` | (via `AwsPollyTtsAppService`) | `AwsPollyTtsAppService` | `AddAwsPollyTtsService(Action<AwsPollyOptions>)`, `AddAwsPollyTtsServiceFromConfiguration(IConfiguration, string?)`, `AddAmazonPollyFromConfiguration(IConfiguration, string?)` |
| [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md) | `ITtsService<TypecastTtsRequest>` | (via `TypecastTtsAppService`) | `TypecastTtsAppService` | `AddTypecastTtsService(Action<TypecastOptions>?)`, `AddTypecastTtsServiceFromConfiguration(IConfiguration, string?)` (requires `TypecastClient` to be registered first) |
| [`Lyo.Tts.WindowsSpeech`](../Lyo.Tts.WindowsSpeech/README.md) | `ITtsService<WindowsTtsRequest>` | No `WindowsSpeechTtsAppService` ships yet | (none) | `AddWindowsSpeechTtsService(Action<TtsServiceOptions>?)`, `AddWindowsSpeechTtsService(TtsServiceOptions)` |

`AwsPolly` and `Typecast` register both `ITtsService<TRequest>` and the non-generic `ITtsService`. The
non-generic interface is backed by the `*TtsAppService` adapter so the same singleton handles both calls.
`WindowsSpeech` currently registers only the typed interface. Code that depends on the non-generic
`ITtsService` will not resolve when WindowsSpeech is the only TTS provider registered.

## Request types

- `AwsPollyTtsRequest`. Polly voice, engine, sample rate, output format, and related fields.
- `TypecastTtsRequest`. Model id, voice id, language, format, prosody.
- `WindowsTtsRequest`. Voice id, `Volume`, `SpeechRate`, optional `OutputFormat` (WAV output is enforced by SAPI regardless).

## Base class (`TtsServiceBase<TRequest>`)

- `Task<TtsResult<TRequest>> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default)`
- `Task<TtsResult<TRequest>> SynthesizeCoreAsync(TRequest request, CancellationToken ct = default)`
- `Task<bool> TestConnectionAsync(CancellationToken ct = default)`

## Error codes (`TtsErrorCodes`)

| Constant | Value |
| -------------------- | ------------------------- |
| `SynthesizeFailed` | `TTS_SYNTHESIZE_FAILED` |
| `OperationCancelled` | `TTS_OPERATION_CANCELLED` |
| `FileWriteFailed` | `TTS_FILE_WRITE_FAILED` |
| `StreamWriteFailed` | `TTS_STREAM_WRITE_FAILED` |

## Default metric keys (`Lyo.Tts.Constants.Metrics`)

| Constant key | Metric | Kind |
| ------------------------------ | -------------------------------------- | ------- |
| `SynthesizeDuration` | `tts.synthesize.duration` | Timer |
| `SynthesizeSuccess` | `tts.synthesize.success` | Counter |
| `SynthesizeFailure` | `tts.synthesize.failure` | Counter |
| `BulkSynthesizeDuration` | `tts.bulk.synthesize.duration` | Timer |
| `BulkSynthesizeTotal` | `tts.bulk.synthesize.total` | Counter |
| `BulkSynthesizeSuccess` | `tts.bulk.synthesize.success` | Counter |
| `BulkSynthesizeFailure` | `tts.bulk.synthesize.failure` | Counter |
| `BulkSynthesizeLastDurationMs` | `tts.bulk.synthesize.last_duration_ms` | Gauge |

Providers typically remap these to a namespaced prefix (`tts.awspolly.*`, `tts.typecast.*`,
`tts.windowsspeech.*`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Tts.Models` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)