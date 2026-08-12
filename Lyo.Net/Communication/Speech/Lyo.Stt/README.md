# Lyo.Stt

Provider-agnostic Speech-to-Text **contract** for the Lyo stack. This package ships the interface (`ISttService`), an abstract base class (`SttServiceBase`), the
request/result/options/event records, and metric name constants. **No provider implementations ship in this repository today** — applications that need transcription provide their
own `SttServiceBase` subclass (or any `ISttService`) and register it through DI themselves.

## What ships — `ISttService`

- `RecognizeAsync(byte[] audioData, LanguageCodeInfo?, AudioFormat?, CancellationToken)`
- `RecognizeFromFileAsync(string audioFilePath, LanguageCodeInfo?, CancellationToken)` — audio format is detected from the file extension.
- `RecognizeFromStreamAsync(Stream audioStream, LanguageCodeInfo?, AudioFormat?, CancellationToken)`
- `RecognizeAsync(SttRequest request, CancellationToken)`
- `RecognizeBulkAsync(IEnumerable<SttRequest>, CancellationToken)`
- `TestConnectionAsync(CancellationToken)`

## What ships — `SttServiceBase`

Abstract `ISttService` implementation (also `IDisposable`) that supplies the bulk pipeline, concurrency throttling, and metric/event plumbing. Subclasses implement: -
`Task<SttResult> RecognizeCoreAsync(SttRequest request, CancellationToken ct)` — the actual provider call. - `Task<bool> TestConnectionAsync(CancellationToken ct)`. The base class
exposes events `Recognizing`, `Recognized`, `BulkRecognizing`, and `BulkRecognized`, and applies a `SemaphoreSlim` sized by `SttServiceOptions.BulkSttConcurrencyLimit` to throttle
bulk work.

## What ships — `SttServiceOptions`

Base options (in `Lyo.Stt.Models`):

| Property                  | Type                | Default                     | Purpose                                                                                          |
|---------------------------|---------------------|-----------------------------|--------------------------------------------------------------------------------------------------|
| `DefaultLanguageCode`     | `LanguageCodeInfo?` | `null`                      | Default language for `RecognizeAsync` convenience overloads.                                     |
| `DefaultAudioFormat`      | `AudioFormat?`      | `null`                      | Default audio format used when callers omit it.                                                  |
| `MaxAudioFileSize`        | `long`              | `10 * 1024 * 1024` (10 MiB) | Advisory upper bound for `RecognizeFromFileAsync` payloads (enforced by providers).              |
| `EnableMetrics`           | `bool`              | `true`                      | When `false`, the base swaps in `NullMetrics.Instance`.                                          |
| `BulkSttConcurrencyLimit` | `int`               | `10`                        | Concurrency cap for `RecognizeBulkAsync`.                                                        |
| `MaxBulkSttLimit`         | `int`               | `100`                       | Max requests per `RecognizeBulkAsync` call (throws `ArgumentOutsideRangeException` if exceeded). |

## What ships — Metric names (`Lyo.Stt.Constants.Metrics`)

`SttServiceBase` records counters/timers under these keys (providers may override the dictionary):

| Key                           | Metric                                | Kind    |
|-------------------------------|---------------------------------------|---------|
| `RecognizeDuration`           | `stt.recognize.duration`              | Timer   |
| `RecognizeSuccess`            | `stt.recognize.success`               | Counter |
| `RecognizeFailure`            | `stt.recognize.failure`               | Counter |
| `BulkRecognizeDuration`       | `stt.bulk.recognize.duration`         | Timer   |
| `BulkRecognizeTotal`          | `stt.bulk.recognize.total`            | Counter |
| `BulkRecognizeSuccess`        | `stt.bulk.recognize.success`          | Counter |
| `BulkRecognizeFailure`        | `stt.bulk.recognize.failure`          | Counter |
| `BulkRecognizeLastDurationMs` | `stt.bulk.recognize.last_duration_ms` | Gauge   |

## No bundled providers

There are no `Lyo.Stt.*` provider packages in this solution at the moment. To use the contract you must write your own implementation (typically by subclassing `SttServiceBase` and
wiring it into DI directly). If/when a provider ships, this README will list it here.

## Target frameworks

`netstandard2.0;net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)