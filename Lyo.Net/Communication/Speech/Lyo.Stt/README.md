# Lyo.Stt

Lyo speech-to-text contract. The package ships `ISttService`, an abstract `SttServiceBase`, the request/result/options/event records, and metric name constants. No provider implementations ship in this repository. Applications that need transcription implement `SttServiceBase` (or any `ISttService`) and register it themselves.

## `ISttService`

- `RecognizeAsync(byte[] audioData, LanguageCodeInfo?, AudioFormat?, CancellationToken)`
- `RecognizeFromFileAsync(string audioFilePath, LanguageCodeInfo?, CancellationToken)`. Audio format is inferred from the file extension.
- `RecognizeFromStreamAsync(Stream audioStream, LanguageCodeInfo?, AudioFormat?, CancellationToken)`
- `RecognizeAsync(SttRequest request, CancellationToken)`
- `RecognizeBulkAsync(IEnumerable<SttRequest>, CancellationToken)`
- `TestConnectionAsync(CancellationToken)`

## `SttServiceBase`

Abstract `ISttService` implementation (also `IDisposable`) that provides the bulk pipeline, concurrency throttling, and metric/event plumbing. Subclasses implement `Task<SttResult> RecognizeCoreAsync(SttRequest request, CancellationToken ct)` (the provider call) and `Task<bool> TestConnectionAsync(CancellationToken ct)`. The base class raises events `Recognizing`, `Recognized`, `BulkRecognizing`, and `BulkRecognized`, and applies a `SemaphoreSlim` sized by `SttServiceOptions.BulkSttConcurrencyLimit` to throttle bulk work.

## `SttServiceOptions`

Base options (from `Lyo.Stt.Models`):

| Property | Type | Default | Purpose |
| ------------------------- | ------------------- | --------------------------- | -------------------------------------------------------------------------------------------------- |
| `DefaultLanguageCode` | `LanguageCodeInfo?` | `null` | Default language for the `RecognizeAsync` convenience overloads. |
| `DefaultAudioFormat` | `AudioFormat?` | `null` | Audio format used when callers leave it unset. |
| `MaxAudioFileSize` | `long` | `10 * 1024 * 1024` (10 MiB) | Advisory upper bound on `RecognizeFromFileAsync` payloads (enforced by providers). |
| `EnableMetrics` | `bool` | `true` | When `false`, the base substitutes `NullMetrics.Instance`. |
| `BulkSttConcurrencyLimit` | `int` | `10` | Concurrency cap applied to `RecognizeBulkAsync`. |
| `MaxBulkSttLimit` | `int` | `100` | Max requests per `RecognizeBulkAsync` call (throws `ArgumentOutsideRangeException` when exceeded). |

## Metric names (`Lyo.Stt.Constants.Metrics`)

`SttServiceBase` records counters/timers under these keys (providers may replace the dictionary):

| Key | Metric | Kind |
| ----------------------------- | ------------------------------------- | ------- |
| `RecognizeDuration` | `stt.recognize.duration` | Timer |
| `RecognizeSuccess` | `stt.recognize.success` | Counter |
| `RecognizeFailure` | `stt.recognize.failure` | Counter |
| `BulkRecognizeDuration` | `stt.bulk.recognize.duration` | Timer |
| `BulkRecognizeTotal` | `stt.bulk.recognize.total` | Counter |
| `BulkRecognizeSuccess` | `stt.bulk.recognize.success` | Counter |
| `BulkRecognizeFailure` | `stt.bulk.recognize.failure` | Counter |
| `BulkRecognizeLastDurationMs` | `stt.bulk.recognize.last_duration_ms` | Gauge |

## No in-repo providers

No `Lyo.Stt.*` provider packages in this solution. Using the contract means writing your own implementation, usually by subclassing `SttServiceBase` and registering it in DI. A later provider will be listed here.

## Supported frameworks

`netstandard2.0;net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)