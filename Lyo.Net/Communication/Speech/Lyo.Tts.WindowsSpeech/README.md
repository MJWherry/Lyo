# Lyo.Tts.WindowsSpeech

Windows SAPI text-to-speech. `WindowsSpeechTtsService` uses the built-in Speech API.

## Features

- **SAPI.** Uses the Windows Speech Synthesis API.
- **Voices.** Select any installed Windows voice.
- **Rate.** Speech rate from -10 to 10.
- **Volume.** Volume from 0 to 100.
- **Bulk.** Bulk synthesis through the shared `Lyo.Tts` pipeline.
- **Logging.** Logs through Microsoft.Extensions.Logging.
- **Metrics.** Optional metrics on TTS calls.
- **Concurrency.** Safe for concurrent use.
- **Async.** Methods take `CancellationToken`.

## Examples

### Subscribe to events

```csharp
using Lyo.Tts;
using Lyo.Tts.Models;
using Lyo.Tts.WindowsSpeech;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddWindowsSpeechTtsService(options =>
{
    options.DefaultVoiceId = "Microsoft Zira Desktop";
    options.DefaultOutputFormat = AudioFormat.Wav;
    options.MaxTextLength = 5000;
    options.EnableMetrics = true;
});

await using var serviceProvider = services.BuildServiceProvider();
var ttsService = serviceProvider.GetRequiredService<ITtsService<WindowsTtsRequest>>();

var result = await ttsService.SynthesizeAsync("Hello, world!");
if (result.IsSuccess && result.AudioData is { Length: > 0 })
    await File.WriteAllBytesAsync("output.wav", result.AudioData);
```

### Explicit options instance

```csharp
services.AddWindowsSpeechTtsService(new TtsServiceOptions
{
    DefaultVoiceId = "Microsoft Zira Desktop",
    DefaultOutputFormat = AudioFormat.Wav,
    MaxTextLength = 5000,
    EnableMetrics = true,
});
```

### List available voices

```csharp
var ttsService = serviceProvider.GetRequiredService<ITtsService<WindowsTtsRequest>>();
var ok = await ttsService.TestConnectionAsync();
// TestConnectionAsync logs every installed SAPI voice via the registered ILogger.
```

## Platform support

Windows only. This package builds and runs on Windows. It needs the `System.Speech` package, which is Windows-specific.

## Registered services

`AddWindowsSpeechTtsService` (`Action<TtsServiceOptions>?` or `TtsServiceOptions`) registers:

- `TtsServiceOptions` (singleton).
- `WindowsSpeechTtsService` (singleton; subclass of `TtsServiceBase<WindowsTtsRequest>`).
- `ITtsService<WindowsTtsRequest>` resolved from the singleton above.

It does not register the non-generic `ITtsService`. There is no `WindowsSpeechTtsAppService`
adapter in this package today, so callers that depend on `ITtsService` should depend on
`ITtsService<WindowsTtsRequest>` instead, or wire their own adapter. The other Lyo TTS providers
([`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md), [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md))
register both interfaces because they ship an `*TtsAppService` adapter.

## Requirements

- Windows operating system
- .NET Standard 2.0 or .NET 10.0
- System.Speech package (automatically included)

## Notes

- This library will not build on non-Windows platforms
- The library uses Windows SAPI which is only available on Windows
- Audio output format is always WAV when using Windows Speech Synthesis

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Tts` (direct, lyo)
- `Lyo.Tts.Models` (direct, lyo)
- `System.Speech` `10.0.5` (direct, microsoft, $([MSBuild]::IsOSPlatform('Windows')))
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)