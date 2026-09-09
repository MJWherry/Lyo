# Lyo.Tts.WindowsSpeech

Text-to-speech through Windows SAPI. `WindowsSpeechTtsService` uses the built-in Speech API.

## Features

- **SAPI.** Calls the Windows Speech Synthesis API.
- **Voices.** Pick any installed Windows voice.
- **Rate.** Speech rate in the range -10 to 10.
- **Volume.** Volume in the range 0 to 100.
- **Bulk.** Batch synthesis through the shared `Lyo.Tts` pipeline.
- **Logging.** Writes through Microsoft.Extensions.Logging.
- **Metrics.** Optional instrumentation for TTS calls.
- **Concurrency.** Safe to use from multiple threads.
- **Async.** Every method accepts a `CancellationToken`.

## Examples

### Listen for events

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

### Ready-made options instance

```csharp
services.AddWindowsSpeechTtsService(new TtsServiceOptions
{
    DefaultVoiceId = "Microsoft Zira Desktop",
    DefaultOutputFormat = AudioFormat.Wav,
    MaxTextLength = 5000,
    EnableMetrics = true,
});
```

### Enumerate voices

```csharp
var ttsService = serviceProvider.GetRequiredService<ITtsService<WindowsTtsRequest>>();
var ok = await ttsService.TestConnectionAsync();
// TestConnectionAsync logs every installed SAPI voice via the registered ILogger.
```

## Supported platforms

Windows only. This package builds and runs on Windows. It needs the Windows-specific `System.Speech` package.

## What gets registered

`AddWindowsSpeechTtsService` (`Action<TtsServiceOptions>?` or `TtsServiceOptions`) wires:

- `TtsServiceOptions` (singleton).
- `WindowsSpeechTtsService` (singleton; subclass of `TtsServiceBase<WindowsTtsRequest>`).
- `ITtsService<WindowsTtsRequest>` resolved from the singleton above.

It does not wire the non-generic `ITtsService`. No `WindowsSpeechTtsAppService`
adapter in this package today, so callers that depend on `ITtsService` should depend on
`ITtsService<WindowsTtsRequest>` instead, or wire their own adapter. The other Lyo TTS providers
([`Lyo.Tts.AwsPolly`](../Lyo.Tts.AwsPolly/README.md), [`Lyo.Tts.Typecast`](../Lyo.Tts.Typecast/README.md))
register both interfaces because they ship an `*TtsAppService` adapter.

## Requirements

- Windows OS
- .NET Standard 2.0 or .NET 10.0
- System.Speech package (pulled in automatically)

## Notes

- This library will not compile off Windows
- The library calls Windows SAPI, which exists only on Windows
- Windows Speech Synthesis always emits WAV

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Tts` (direct, lyo)
- `Lyo.Tts.Models` (direct, lyo)
- `System.Speech` `10.0.5` (direct, microsoft, $([MSBuild]::IsOSPlatform('Windows')))
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)