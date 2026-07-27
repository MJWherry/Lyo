# Lyo.Tts.WindowsSpeech

Windows Speech Synthesis Text-to-Speech service implementation for the Lyo framework using Windows built-in SAPI (Speech API).

## Features

- **Windows Native**: Uses Windows built-in Speech Synthesis API (SAPI)
- **Voice Selection**: Support for selecting installed Windows voices
- **Speech Rate Control**: Adjustable speech rate (-10 to 10)
- **Volume Control**: Adjustable volume (0 to 100)
- **Bulk Operations**: Support for bulk text-to-speech synthesis
- **Logging**: Comprehensive logging support via Microsoft.Extensions.Logging
- **Metrics**: Optional metrics collection for monitoring TTS operations
- **Thread-Safe**: Safe for concurrent use
- **Async Support**: Full async/await support with cancellation token support

## Platform Support

**Windows Only**: This library only builds and runs on Windows platforms. It requires the `System.Speech` package which is Windows-specific.

## Quick Start

### Basic Usage

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

### Using with an explicit options instance

```csharp
services.AddWindowsSpeechTtsService(new TtsServiceOptions
{
    DefaultVoiceId = "Microsoft Zira Desktop",
    DefaultOutputFormat = AudioFormat.Wav,
    MaxTextLength = 5000,
    EnableMetrics = true,
});
```

### List Available Voices

```csharp
var ttsService = serviceProvider.GetRequiredService<ITtsService<WindowsTtsRequest>>();
var ok = await ttsService.TestConnectionAsync();
// TestConnectionAsync logs every installed SAPI voice via the registered ILogger.
```

## Registered services

`AddWindowsSpeechTtsService` (`Action<TtsServiceOptions>?` or `TtsServiceOptions`) registers:

- `TtsServiceOptions` (singleton).
- `WindowsSpeechTtsService` (singleton; subclass of `TtsServiceBase<WindowsTtsRequest>`).
- `ITtsService<WindowsTtsRequest>` resolved from the singleton above.

It does **not** register the non-generic `ITtsService` — there is no `WindowsSpeechTtsAppService`
adapter in this package today, so callers that depend on `ITtsService` should depend on
`ITtsService<WindowsTtsRequest>` instead (or wire up their own adapter). The other Lyo TTS providers
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

*(Synchronized from `Lyo.Tts.WindowsSpeech.csproj`.)*

**Target framework:** `netstandard2.0;net10.0` *(Windows only)*

### NuGet packages

| Package         | Version  |
|-----------------|----------|
| `System.Speech` | `10.0.2` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Tts`](../Lyo.Tts/README.md)
- [`Lyo.Tts.Models`](../Lyo.Tts.Models/README.md)