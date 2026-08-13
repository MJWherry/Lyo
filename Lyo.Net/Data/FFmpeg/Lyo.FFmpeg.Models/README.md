# Lyo.FFmpeg.Models

Engine-neutral **contracts and models** for [`Lyo.FFmpeg`](../Lyo.FFmpeg/README.md): the three service interfaces (`IAudioPlayer`, `IAudioProber`, `IAudioConverter`), their request/options shapes (`AudioConversionRequest`, `AudioConversionOptions`), the probe result type (`AudioProbeResult`), and global host configuration (`FFmpegOptions` + `FFmpegProcessOutputMode`).

This package has **no DI surface** — registration lives in `Lyo.FFmpeg`. Reference it from libraries that only need the abstractions (mocking, custom backends, host bindings).

## Public API

| Type | Description |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`IAudioPlayer`** | `PlayAsync(filePath)`, `PlayStreamAsync(stream)`, `PlayBytesAsync(bytes)` — each returns `Task<Result<bool>>`. |
| **`IAudioProber`** | `ProbeAsync(filePath)`, `ProbeStreamAsync(stream)`, `ProbeBytesAsync(bytes)` → `Task<Result<AudioProbeResult>>`. |
| **`IAudioConverter`** | Full file/stream/byte conversion matrix plus a request-shaped `ConvertAsync(AudioConversionRequest)`. |
| **`AudioConversionRequest`** | `InputPath`, `OutputPath`, `Codec` (default `pcm_s16le`), `SampleRate` (44100), `Channels` (2), `Format` (`wav`), `Overwrite` (true), `NoVideo` (true). |
| **`AudioConversionOptions`** | Stream/byte-mode equivalent (no paths) — same fields, nullable defaults. |
| **`AudioProbeResult`** | `FilePath`, `DurationSeconds`, `Format`, `SampleRate`, `Channels`, `Codec`, `BitRate`, `FileSizeBytes`, `HasVideo`, `HasAudio`, `RawMetadata` (raw ffprobe key/value). |
| **`FFmpegOptions`** | Executable paths (`FFmpegPath`, `FfprobePath`, `FfplayPath`), defaults (`DefaultCodec`, `DefaultSampleRate`, `DefaultChannels`, `DefaultFormat`, `DefaultOverwrite`, `DefaultNoVideo`), `GlobalArguments`, `EnableMetrics`, `SuppressFfplayOutput`, `ProcessOutputMode`. `SectionName = "FFmpegOptions"`. |
| **`FFmpegProcessOutputMode`** | `Suppress` (default — capture stdout/stderr internally) or `Passthrough` (echo to console for debugging). |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)