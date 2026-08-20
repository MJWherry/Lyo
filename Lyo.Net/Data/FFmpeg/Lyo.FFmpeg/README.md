# Lyo.FFmpeg

Wraps the `ffmpeg` / `ffprobe` / `ffplay` CLIs (via CliWrap) behind three contracts from [`Lyo.FFmpeg.Models`](../Lyo.FFmpeg.Models/README.md): IAudioPlayer, IAudioProber, IAudioConverter. Includes FFmpegCommandBuilder for hand-rolled command lines and FFmpegTempHelper for stream inputs.

## Examples

### Usage

```csharp
using Lyo.FFmpeg;
using Lyo.FFmpeg.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFFmpegServices(o => {
    o.FFmpegPath = "/usr/bin/ffmpeg";
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
  "FFmpegOptions": {
    "FFmpegPath": "/usr/bin/ffmpeg",
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

### AddFFmpegServicesFromConfiguration

```csharp
services.AddFFmpegServicesFromConfiguration(builder.Configuration);
```

### Building a command manually

```csharp
var args = new FFmpegCommandBuilder()
    .WithDefaults(options) // FFmpegOptions
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
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FFmpegAudioPlayer | IAudioPlayer over ffplay. PlayAsync(filePath), PlayStreamAsync(stream), PlayBytesAsync(bytes). |
| FFmpegAudioProber | IAudioProber over ffprobe. ProbeAsync, ProbeStreamAsync, ProbeBytesAsync return AudioProbeResult (duration, sample rate, channels, codec, bit rate, has-video/audio, raw ffprobe metadata). |
| FFmpegAudioConverter | IAudioConverter over ffmpeg. File, stream, and byte conversion overloads (ConvertFileToFileAsync, ConvertFileToStreamAsync, ConvertStreamToBytesAsync, and the rest) plus ConvertAsync(AudioConversionRequest). |
| FFmpegCommandBuilder | Fluent builder for ad-hoc ffmpeg command lines: WithInput/WithOutput, WithCodec, WithSampleRate, WithChannels, WithFormat, WithOverwrite, WithNoVideo, WithDefaults(FFmpegOptions), custom args. |
| FFmpegProcessRunner | Internal runner that executes the built command line through CliWrap, applying FFmpegOptions.GlobalArguments and ProcessOutputMode (Suppress/Passthrough). |
| FFmpegTempHelper | Writes input streams/bytes into a scoped temp file so ffmpeg/ffprobe can read them by path. Cleans up on disposal. |
| Extensions | DI: AddFFmpegServices(), AddFFmpegServices(Action<FFmpegOptions>), AddFFmpegServicesFromConfiguration(IConfiguration, sectionName?). |

Each AddFFmpegServices* overload registers FFmpegAudioPlayer, FFmpegAudioProber, and FFmpegAudioConverter as scoped services and exposes them through both the concrete type and their interfaces.

## Notes

- Requires `ffmpeg`, `ffprobe`, and (for playback) `ffplay` either on `PATH` or pointed to by `FFmpegOptions.FFmpegPath` / `FfprobePath` / `FfplayPath`.
- All operations are async and return `Result<…>`; failures carry stable error codes from `Constants`.
- Stream/bytes inputs are written to a temporary file managed by `FFmpegTempHelper` so the CLI can read them by path.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FFmpeg.Models` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `CliWrap` `3.10.2` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)