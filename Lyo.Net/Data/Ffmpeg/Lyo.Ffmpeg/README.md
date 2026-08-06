# Lyo.Ffmpeg

FFmpeg integration for .NET. Wraps the `ffmpeg` / `ffprobe` / `ffplay` CLIs (via **CliWrap**) behind three contracts from [`Lyo.Ffmpeg.Models`](../Lyo.Ffmpeg.Models/README.md): **`IAudioPlayer`**, **`IAudioProber`**, **`IAudioConverter`**. Includes a fluent **`FFmpegCommandBuilder`** for hand-rolled command lines and a temp-file helper for stream inputs (`FfmpegTempHelper`).

## Examples

### Usage

```csharp
using Lyo.Ffmpeg;
using Lyo.Ffmpeg.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFfmpegServices(o => {
    o.FfmpegPath = "/usr/bin/ffmpeg";
    o.FfprobePath = "/usr/bin/ffprobe";
    o.DefaultFormat = "wav";
    o.DefaultSampleRate = 44100;
    o.DefaultChannels = 2;
});

await using var sp = services.BuildServiceProvider();
using var scope = sp.CreateScope();

var prober = scope.ServiceProvider.GetRequiredService<IAudioProber>();
var probe = await prober.ProbeAsync("clip.mp3");
if (probe.IsSuccess) Console.WriteLine($"{probe.Value.DurationSeconds}s {probe.Value.Codec}");

var converter = scope.ServiceProvider.GetRequiredService<IAudioConverter>();
await converter.ConvertFileToFileAsync(
    inputPath: "clip.mp3",
    outputPath: "clip.wav",
    options: new AudioConversionOptions { Codec = "pcm_s16le", SampleRate = 44100, Channels = 2 });
```

### Configuration binding

```json
{
  "FfmpegOptions": {
    "FfmpegPath": "/usr/bin/ffmpeg",
    "FfprobePath": "/usr/bin/ffprobe",
    "FfplayPath": null,
    "DefaultCodec": "pcm_s16le",
    "DefaultSampleRate": 44100,
    "DefaultChannels": 2,
    "DefaultFormat": "wav",
    "DefaultOverwrite": true,
    "DefaultNoVideo": true,
    "EnableMetrics": false,
    "SuppressFfplayOutput": true,
    "ProcessOutputMode": "Suppress",
    "GlobalArguments": ["-hide_banner", "-loglevel", "warning"]
  }
}
```

### Configuration binding (2)

```csharp
services.AddFfmpegServicesFromConfiguration(builder.Configuration);
```

### Building a command manually

```csharp
var args = new FFmpegCommandBuilder()
    .WithDefaults(options) // FfmpegOptions
    .WithInput("clip.mp3")
    .WithOutput("clip.wav")
    .WithCodec("pcm_s16le")
    .WithSampleRate(44100)
    .WithChannels(2)
    .WithOverwrite(true)
    .Build();
```

## Public API

| Type | Description |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`FfmpegAudioPlayer`** | `IAudioPlayer` over `ffplay`; `PlayAsync(filePath)`, `PlayStreamAsync(stream)`, `PlayBytesAsync(bytes)`. |
| **`FfmpegAudioProber`** | `IAudioProber` over `ffprobe`; `ProbeAsync`, `ProbeStreamAsync`, `ProbeBytesAsync` → `AudioProbeResult` (duration, sample rate, channels, codec, bit rate, has-video/audio, raw `ffprobe` metadata). |
| **`FfmpegAudioConverter`** | `IAudioConverter` over `ffmpeg`; full matrix of file/stream/byte conversion overloads (`ConvertFileToFileAsync`, `ConvertFileToStreamAsync`, `ConvertStreamToBytesAsync`, etc.) and a request-shaped `ConvertAsync(AudioConversionRequest)`. |
| **`FFmpegCommandBuilder`** | Fluent builder for ad-hoc ffmpeg command lines: `WithInput/WithOutput`, `WithCodec`, `WithSampleRate`, `WithChannels`, `WithFormat`, `WithOverwrite`, `WithNoVideo`, `WithDefaults(FfmpegOptions)`, custom args. |
| **`FfmpegProcessRunner`** | Internal runner that executes the built command line through CliWrap, applying `FfmpegOptions.GlobalArguments` and `ProcessOutputMode` (`Suppress`/`Passthrough`). |
| **`FfmpegTempHelper`** | Materializes input streams/bytes into a scoped temp file so ffmpeg/ffprobe can read them by path (cleaned up on disposal). |
| **`Extensions`** | DI: **`AddFfmpegServices()`**, **`AddFfmpegServices(Action<FfmpegOptions>)`**, **`AddFfmpegServicesFromConfiguration(IConfiguration, sectionName?)`**. |

Each `AddFfmpegServices*` overload registers `FfmpegAudioPlayer`/`FfmpegAudioProber`/`FfmpegAudioConverter` as **scoped** services and exposes them through both the concrete
type and their respective interfaces.

## Notes

- Requires `ffmpeg`, `ffprobe`, and (for playback) `ffplay` either on `PATH` or pointed to by `FfmpegOptions.FfmpegPath` / `FfprobePath` / `FfplayPath`.
- All operations are async and return `Result<…>`; failures carry stable error codes from `Constants`.
- Stream/bytes inputs are written to a temporary file managed by `FfmpegTempHelper` so the CLI can read them by path.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Ffmpeg.Models` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `CliWrap` `3.10.2` — (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)