# Lyo.FFmpeg

CliWrap wrapper around `ffmpeg` / `ffprobe` / `ffplay` for [`Lyo.Media.Models`](../Lyo.Media.Models/README.md): sibling IAudio* and IVideo* contracts. Convert includes transcode and compress. Stream is StartConvertAsync (stdout readable while ffmpeg runs). Play is local ffplay. Images stay [`IImageService`](../../Images/Lyo.Images/README.md). This package does not reference FileStorage, Blazor, or Discord. One instance is safe for concurrent convert/probe/play (one process per call; no process-wide lock).

## Features

- **Convert.** File-to-file transcode and compress. `AudioConversion` and `VideoConversion` build the request. Metadata dictionaries use [`FileTagKey`](../../FileTags/Lyo.FileTags/README.md) and still emit `-metadata` during encode. Tag read and stream-copy write without a re-encode live in [`Lyo.FileTags.FFmpeg`](../../FileTags/Lyo.FileTags.FFmpeg/README.md). `FFmpegCommandBuilder` stays the raw argv escape hatch. Pipe mode does not spool to a temp file.
- **Stream.** StartConvertAsync returns a session whose stdout is live. ForRawPcm for Discord-shaped PCM. ForStreamingVideo remuxes fragmented mp4; WebM is copy plus pipe with no movflags.
- **Play.** ffplay for audio and video. NoDisplay defaults true so headless hosts do not open a window; video window is opt-in.
- **Probe.** ffprobe JSON via TypeConversion. Video width/height/fps. Album art is not HasVideo.
- **Jobs.** Extract audio, concat demuxer, GIF palette, loudnorm, libx264/libx265 bitrate two-pass, HLS/DASH playlists, and sized thumbnails. One ffmpeg process per pass.
- **GPU.** `FFmpegOptions.Hardware` is the worker switch (`nvenc`, `qsv`, `vaapi`, `videotoolbox`). Null stays on the CPU. A request `HardwareAccel.None` forces the CPU. There is no probe that falls back when the GPU encoder is missing.

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
    "Hardware": "nvenc",
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
var request = VideoConversion.Transcode("in.mov", "out.webm", MediaContainer.WebM, AudioEncoder.Opus, VideoEncoder.Vp9)
    .WithAudioBitRate(MediaBitRate.K128)
    .WithSize(1280, 720, ScaleMode.Fit)
    .Build();
await video.ConvertAsync(request);

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
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FFmpegAudioConverter / FFmpegVideoConverter | IAudioConverter and IVideoConverter on ffmpeg. Shared runner. Concurrent converts on one instance are two processes. |
| FFmpegAudioProber / FFmpegVideoProber | IAudioProber and IVideoProber on ffprobe. |
| FFmpegAudioPlayer / FFmpegVideoPlayer | IAudioPlayer and IVideoPlayer on ffplay. NoDisplay defaults true. |
| FFmpegCommandBuilder | Raw argv builder and escape hatch. Do not reuse across threads. Maps codec tokens onto encoder names (h264 becomes libx264, hevc becomes libx265). New flags must be listed or Emit drops them. AudioConversion and VideoConversion do not grow a raw-argument list. |
| Extensions | AddFFmpegServices(), AddFFmpegServices(Action), AddFFmpegServices(FFmpegOptions), AddFFmpegServicesFromConfiguration. |

Every AddFFmpegServices* overload registers audio and video converter, prober, and player as scoped services (TryAddScoped) and exposes them as both the concrete type and their interfaces. A Discord host can take IAudioConverter only.

## Notes

- `ffmpeg`, `ffprobe`, and (for playback) `ffplay` must be on PATH or set via FFmpegOptions. Binaries are not checked at DI time.
- Failures use Constants.Errors (FFmpegError, FFprobeError, FfplayError, ProbeParseError).
- ForAudio applies pcm/wav defaults. A bare AudioConversionOptions does not force pcm. Audio converts always emit -vn.
- Set Crf or VideoBitRate, not both. Bitrate, sample rate, channels, CRF, and frame rate are catalog records (`MediaBitRate.K128` id `128k`). CopyCodecs remuxes without re-encode and does not pick up a host GPU.
- `FFmpegOptions.Hardware` is inherited when the request leaves Hardware null. NVENC quality is `-rc:v vbr -cq`. QSV uses `-global_quality`. VAAPI uses `-qp` plus `format=nv12,hwupload` when VideoFilter is empty; a custom VideoFilter replaces that upload graph. VideoToolbox has no rate factor. An x264 preset on NVENC becomes `EncoderPreset.P4`. AV1 rejects x264 presets; `EncoderPreset.Svt8` is the libsvtav1 default.
- M4A uses the ipod muxer, so `TryFromFormat("mp4")` stays Mp4. GIF runs palettegen then paletteuse and rejects pipes and StartConvert. Bitrate two-pass is libx264 or libx265 only, deletes both pass log files, and reports progress on pass 2. Concat uses the demuxer (`-f concat -safe 0` before `-i`) and still requires a matching stream layout. Loudnorm forces info log level so a warning GlobalArguments entry does not hide the measure JSON.
- StartConvertAsync does not apply ProcessTimeout. Cancel with the token or by disposing the session.
- IoMode.Pipe uses pipe:0/pipe:1. Muxed mp4 on a pipe needs FragmentedOutput (ForStreamingVideo default). WebM streaming is copy + pipe, no movflags. Convert*ToBytesAsync is for tiny clips.
- KnownDuration is only for MediaProgress.Percentage. IProgress callbacks fire on the CliWrap thread; hosts that mutate UI must synchronize.
- Encrypted FileStorage objects: stream via GetFileStreamAsync. Presigned GET is ciphertext when the file is encrypted.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FFmpeg.Models` (direct, lyo)
- `Lyo.FileTags` (direct, lyo)
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