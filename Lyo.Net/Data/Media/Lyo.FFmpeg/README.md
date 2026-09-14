# Lyo.FFmpeg

CliWrap wrapper around `ffmpeg` / `ffprobe` / `ffplay` for [`Lyo.Media.Models`](../Lyo.Media.Models/README.md): sibling IAudio* and IVideo* contracts. Convert includes transcode and compress. Stream is StartConvertAsync (stdout readable while ffmpeg runs). Play is local ffplay. Images stay [`IImageService`](../../Images/Lyo.Images/README.md). This package does not reference FileStorage, Blazor, or Discord. One instance is safe for concurrent convert/probe/play (one process per call; no process-wide lock).

## Features

- **Convert.** File-to-file transcode and compress (CRF, bitrate, preset). Pipe mode does not spool to a temp file. Audio and video facades share one runner.
- **Stream.** StartConvertAsync returns a session whose stdout is live. ForRawPcm for Discord-shaped PCM. ForStreamingVideo remuxes fragmented mp4; WebM is copy plus pipe with no movflags.
- **Play.** ffplay for audio and video. NoDisplay defaults true so headless hosts do not open a window; video window is opt-in.
- **Probe.** ffprobe JSON via TypeConversion. Video width/height/fps. Album art is not HasVideo.

## Examples

### Probe and convert audio

```csharp
using Lyo.FFmpeg;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFFmpegServices(o => {
    o.FFmpegPath = "/usr/bin/ffmpeg";
    o.FfprobePath = "/usr/bin/ffprobe";
});

await using var sp = services.BuildServiceProvider();
using var scope = sp.CreateScope();

var prober = scope.ServiceProvider.GetRequiredService<IAudioProber>();
var probe = await prober.ProbeAsync("clip.mp3");
if (probe.IsSuccess) Console.WriteLine($"{probe.Data!.DurationSeconds}s {probe.Data.Codec}");

var converter = scope.ServiceProvider.GetRequiredService<IAudioConverter>();
await converter.ConvertFileToFileAsync("clip.mp3", "clip.wav", AudioConversionOptions.ForAudio());
```

### Compress video then remux for a browser pipe

```csharp
var compress = VideoConversionOptions.ForCompressVideo();
await video.ConvertFileToFileAsync("in.mov", "out.mp4", compress);

var stream = VideoConversionOptions.ForStreamingVideo();
await using var session = (await video.StartConvertAsync("out.mp4", stream)).Data!;
await session.StandardOutput.CopyToAsync(httpResponseBody);
```

### Discord: PCM frames (host wiring, not a package ref)

```csharp
var pcm = AudioConversionOptions.ForRawPcm();
await using var session = (await audio.StartConvertAsync(fileOrUrl, pcm)).Data!;
var frames = new PcmFrameReader(session.StandardOutput);
await foreach (var frame in frames.ReadFramesAsync(ct))
    await sendVoicePacket(frame);
```

### FileStorage input (host wiring, not a package ref)

```text
Plaintext object: pass GetPreSignedReadUrlAsync to ProbeAsync / ConvertFileToFileAsync / PlayAsync as -i (ffmpeg can HTTP range-seek).
Encrypted or compressed object: GetFileStreamAsync into StartConvertAsync(Stream) or ConvertStreamTo* with IoMode.Pipe. Never GetFileAsync (full byte[]). Never point ffmpeg at a presigned URL of ciphertext.
```

### Blazor: skip FFmpeg when the browser can play the file

```text
If the stored mp3/mp4 is already playable, give the browser a URL. Use ForStreamingVideo (copy + fragmented mp4) or ForCompressVideo plus FragmentedOutput when you must remux or shrink. Ordinary non-fragmented mp4 is file-to-file only. WebM streaming is copy + pipe with no movflags.
```

### appsettings sample

```json
{
  "FFmpegOptions": {
    "FFmpegPath": "/usr/bin/ffmpeg",
    "FfprobePath": "/usr/bin/ffprobe",
    "FfplayPath": null,
    "DefaultCodec": "pcm_s16le",
    "DefaultSampleRate": 44100,
    "DefaultChannels": 2,
    "DefaultFormat": "wav",
    "DefaultOverwrite": true,
    "EnableMetrics": false,
    "SuppressFfplayOutput": true,
    "ProcessOutputMode": "Suppress",
    "ProcessTimeout": "00:05:00",
    "GlobalArguments": ["-hide_banner", "-loglevel", "warning"]
  }
}
```

### AddFFmpegServicesFromConfiguration

```csharp
services.AddFFmpegServicesFromConfiguration(builder.Configuration);
```

### Build a command by hand

```csharp
var cmd = FFmpegCommandBuilder.New()
    .WithDefaults(options)
    .WithInput("clip.mp3")
    .WithOutput("clip.wav")
    .WithAudioCodec(AudioEncoder.PcmS16le)
    .WithSampleRate(44100)
    .WithChannels(2)
    .WithFormat(MediaContainer.Wav)
    .DropVideo()
    .Overwrite()
    .Build();
```

## Public types

| Type | Description |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FFmpegAudioConverter / FFmpegVideoConverter | IAudioConverter and IVideoConverter on ffmpeg. Shared runner. Concurrent converts on one instance are two processes. |
| FFmpegAudioProber / FFmpegVideoProber | IAudioProber and IVideoProber on ffprobe. |
| FFmpegAudioPlayer / FFmpegVideoPlayer | IAudioPlayer and IVideoPlayer on ffplay. NoDisplay defaults true. |
| FFmpegCommandBuilder | Fluent argv builder. Do not reuse across threads. Maps MediaContainer.Format and encoder Id onto ffmpeg flags (h264 becomes libx264). WithFormat emits -f. NoOverwrite emits -n. |
| Extensions | AddFFmpegServices(), AddFFmpegServices(Action), AddFFmpegServices(FFmpegOptions), AddFFmpegServicesFromConfiguration. |

Every AddFFmpegServices* overload registers audio and video converter, prober, and player as scoped services (TryAddScoped) and exposes them as both the concrete type and their interfaces. A Discord host can take IAudioConverter only.

## Notes

- `ffmpeg`, `ffprobe`, and (for playback) `ffplay` must be on PATH or set via FFmpegOptions. Binaries are not checked at DI time.
- Failures use Constants.Errors (FFmpegError, FFprobeError, FfplayError, ProbeParseError).
- ForAudio applies pcm/wav defaults. A bare AudioConversionOptions does not force pcm. Audio converts always emit -vn.
- Set Crf or VideoBitRate, not both. CopyCodecs remuxes without re-encode. Compress is an explicit factory. When H.264 is missing, hosts can pass VideoEncoder.Mpeg4.
- StartConvertAsync does not apply ProcessTimeout. Cancel with the token or by disposing the session.
- IoMode.Pipe uses pipe:0/pipe:1. Muxed mp4 on a pipe needs FragmentedOutput (ForStreamingVideo default). WebM streaming is copy + pipe, no movflags. Convert*ToBytesAsync is for tiny clips.
- KnownDuration is only for MediaProgress.Percentage. IProgress callbacks fire on the CliWrap thread; hosts that mutate UI must synchronize.
- Encrypted FileStorage objects: stream via GetFileStreamAsync. Presigned GET is ciphertext when the file is encrypted.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FFmpeg.Models` (direct, lyo)
- `Lyo.Media.Models` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `CliWrap` `3.10.2` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)