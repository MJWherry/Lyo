# Lyo.Media.Models

Contracts for audio and video convert, probe, and play. There is no IMediaConverter: audio and video do not share a convert matrix, and images stay [`IImageService`](../../Images/Lyo.Images/README.md). Containers live on [`MediaContainer`](../../../Core/Common/Lyo.Common.Core/README.md) in Common.Core. Encoders (`AudioEncoder`, `VideoEncoder`, `PixelFormat`, `EncoderPreset`) live here. Take this package from libraries that only need the abstractions (mocking, custom backends, host bindings). Register the FFmpeg implementation in [`Lyo.FFmpeg`](../Lyo.FFmpeg/README.md).

## Features

- **Two families.** IAudioConverter / IAudioProber / IAudioPlayer and IVideoConverter / IVideoProber / IVideoPlayer. No shared converter base.
- **Shared plumbing that is not a converter.** IMediaProcessSession (live stdout), MediaIoMode, MediaProgress, MediaProbeResult (muxed files list every stream; attached_pic is not HasVideo).
- **Factories.** ForRawPcm / ForCompressAudio on AudioConversionOptions. ForCompressVideo / ForStreamingVideo on VideoConversionOptions. ForStreamingVideo defaults to fragmented Mp4; WebM is copy plus pipe with no movflags.
- **PcmFrameReader.** Slices s16le PCM into 20 ms frames without referencing Discord. Single-consumer.
- **Thread-safe catalogs.** TryFromName / Custom use ConcurrentDictionary. Bitrate stays a string (128k, 2M).

## Examples

### Audio PCM session (host wiring, not a package ref)

```csharp
var pcm = AudioConversionOptions.ForRawPcm();
await using var session = (await audio.StartConvertAsync(fileOrUrl, pcm)).Data!;
var frames = new PcmFrameReader(session.StandardOutput);
await foreach (var frame in frames.ReadFramesAsync(ct))
    await sendVoicePacket(frame);
```

## Public types

| Type | Description |
| --------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| IAudioConverter | Audio convert matrix, StartConvertAsync, StartRawPcmAsync. Always drops video. |
| IVideoConverter | Video convert matrix, StartConvertAsync, ExtractFrameAsync. |
| IAudioProber / IVideoProber | ProbeAsync / ProbeStreamAsync / ProbeBytesAsync return MediaProbeResult. Each flattens the first real audio vs first real video stream. |
| IAudioPlayer / IVideoPlayer | Local play. NoDisplay defaults true; video window is opt-in. Network streaming is StartConvertAsync. |
| IMediaProcessSession | Running convert: StandardOutput readable before exit. Single-consumer. Dispose cancels. No process timeout. |
| AudioConversionOptions / VideoConversionOptions | Records with Validate(). Factories on the matching type. Progress callbacks fire on the process I/O thread. |
| AudioEncoder / VideoEncoder / PixelFormat / EncoderPreset | Static instances, TryFromName / TryFromId, Custom. Ids are codec tokens (h264, mp3), not backend encoder names. |
| PcmFrameReader | IAsyncEnumerable of s16le frames. Default 20 ms, 48 kHz, stereo. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)