# Lyo.Ffmpeg

FFmpeg integration for .NET. Wraps the `ffmpeg` / `ffprobe` / `ffplay` CLIs (via **CliWrap**) behind three contracts from [`Lyo.Ffmpeg.Models`](../Lyo.Ffmpeg.Models/README.md):
**`IAudioPlayer`**, **`IAudioProber`**, **`IAudioConverter`**. Includes a fluent **`FFmpegCommandBuilder`** for hand-rolled command lines and a temp-file helper for stream
inputs (`FfmpegTempHelper`).

## Public API

| Type                       | Description                                                                                                                                                                                                                                  |
|----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`FfmpegAudioPlayer`**    | `IAudioPlayer` over `ffplay`; `PlayAsync(filePath)`, `PlayStreamAsync(stream)`, `PlayBytesAsync(bytes)`.                                                                                                                                     |
| **`FfmpegAudioProber`**    | `IAudioProber` over `ffprobe`; `ProbeAsync`, `ProbeStreamAsync`, `ProbeBytesAsync` → `AudioProbeResult` (duration, sample rate, channels, codec, bit rate, has-video/audio, raw `ffprobe` metadata).                                         |
| **`FfmpegAudioConverter`** | `IAudioConverter` over `ffmpeg`; full matrix of file/stream/byte conversion overloads (`ConvertFileToFileAsync`, `ConvertFileToStreamAsync`, `ConvertStreamToBytesAsync`, etc.) and a request-shaped `ConvertAsync(AudioConversionRequest)`. |
| **`FFmpegCommandBuilder`** | Fluent builder for ad-hoc ffmpeg command lines: `WithInput/WithOutput`, `WithCodec`, `WithSampleRate`, `WithChannels`, `WithFormat`, `WithOverwrite`, `WithNoVideo`, `WithDefaults(FfmpegOptions)`, custom args.                             |
| **`FfmpegProcessRunner`**  | Internal runner that executes the built command line through CliWrap, applying `FfmpegOptions.GlobalArguments` and `ProcessOutputMode` (`Suppress`/`Passthrough`).                                                                           |
| **`FfmpegTempHelper`**     | Materializes input streams/bytes into a scoped temp file so ffmpeg/ffprobe can read them by path (cleaned up on disposal).                                                                                                                   |
| **`Extensions`**           | DI: **`AddFfmpegServices()`**, **`AddFfmpegServices(Action<FfmpegOptions>)`**, **`AddFfmpegServicesFromConfiguration(IConfiguration, sectionName?)`**.                                                                                       |

Each `AddFfmpegServices*` overload registers `FfmpegAudioPlayer`/`FfmpegAudioProber`/`FfmpegAudioConverter` as **scoped** services and exposes them through both the concrete
type and their respective interfaces.

## Usage

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

## Notes

- Requires `ffmpeg`, `ffprobe`, and (for playback) `ffplay` either on `PATH` or pointed to by `FfmpegOptions.FfmpegPath` / `FfprobePath` / `FfplayPath`.
- All operations are async and return `Result<…>`; failures carry stable error codes from `Constants`.
- Stream/bytes inputs are written to a temporary file managed by `FfmpegTempHelper` so the CLI can read them by path.

## Dependencies

*(Synchronized from `Lyo.Ffmpeg.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version   |
|---------------------------------------------------------|-----------|
| `CliWrap`                                               | `[3.10,)` |
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)`   |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)`   |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)`   |
| `Microsoft.Extensions.Logging.Abstractions`             | `[10,)`   |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Ffmpeg.Models`](../Lyo.Ffmpeg.Models/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
