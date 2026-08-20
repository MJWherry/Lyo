# Lyo.FFmpeg.Models

Contracts and models for [`Lyo.FFmpeg`](../Lyo.FFmpeg/README.md): IAudioPlayer, IAudioProber, IAudioConverter, AudioConversionRequest, AudioConversionOptions, AudioProbeResult, and host configuration (FFmpegOptions and FFmpegProcessOutputMode).

This package has no DI registration. Registration lives in Lyo.FFmpeg. Reference it from libraries that only need the abstractions (mocking, custom backends, host bindings).

## Public API

| Type | Description |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| IAudioPlayer | PlayAsync(filePath), PlayStreamAsync(stream), PlayBytesAsync(bytes). Each returns Task<Result<bool>>. |
| IAudioProber | ProbeAsync(filePath), ProbeStreamAsync(stream), ProbeBytesAsync(bytes) return Task<Result<AudioProbeResult>>. |
| IAudioConverter | File, stream, and byte conversion overloads plus ConvertAsync(AudioConversionRequest). |
| AudioConversionRequest | InputPath, OutputPath, Codec (default pcm_s16le), SampleRate (44100), Channels (2), Format (wav), Overwrite (true), NoVideo (true). |
| AudioConversionOptions | Stream/byte-mode equivalent (no paths). Same fields, nullable defaults. |
| AudioProbeResult | FilePath, DurationSeconds, Format, SampleRate, Channels, Codec, BitRate, FileSizeBytes, HasVideo, HasAudio, RawMetadata (raw ffprobe key/value). |
| FFmpegOptions | Executable paths (FFmpegPath, FfprobePath, FfplayPath), defaults (DefaultCodec, DefaultSampleRate, DefaultChannels, DefaultFormat, DefaultOverwrite, DefaultNoVideo), GlobalArguments, EnableMetrics, SuppressFfplayOutput, ProcessOutputMode. SectionName = "FFmpegOptions". |
| FFmpegProcessOutputMode | Suppress (default, capture stdout/stderr internally) or Passthrough (echo to console for debugging). |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)