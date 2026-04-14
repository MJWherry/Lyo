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
using Lyo.Tts.WindowsSpeech;
using Microsoft.Extensions.DependencyInjection;

// Register the service
var services = new ServiceCollection();
services.AddWindowsSpeechTtsService(options =>
{
    options.DefaultVoiceId = "Microsoft Zira Desktop";
    options.DefaultLanguageCode = "en-US";
    options.DefaultOutputFormat = "wav";
    options.EnableMetrics = true;
});

var serviceProvider = services.BuildServiceProvider();
var ttsService = serviceProvider.GetRequiredService<ITtsService>();

// Synthesize text to speech
var result = await ttsService.SynthesizeAsync("Hello, world!");

if (result.IsSuccess && result.AudioData != null)
{
    await File.WriteAllBytesAsync("output.wav", result.AudioData);
}
```

### Using with Configuration

```csharp
services.AddWindowsSpeechTtsService(options =>
{
    options.DefaultVoiceId = "Microsoft Zira Desktop";
    options.DefaultLanguageCode = "en-US";
    options.DefaultOutputFormat = "wav";
    options.MaxTextLength = 5000;
    options.EnableMetrics = true;
});
```

### List Available Voices

```csharp
var ttsService = new WindowsSpeechTtsService(options);
var testResult = await ttsService.TestConnectionAsync();
// This will log available voices
```

## Requirements

- Windows operating system
- .NET Standard 2.0 or .NET 10.0
- System.Speech package (automatically included)

## Notes

- This library will not build on non-Windows platforms
- The library uses Windows SAPI which is only available on Windows
- Audio output format is always WAV when using Windows Speech Synthesis

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Tts.WindowsSpeech.csproj`.)*

**Target framework:** `netstandard2.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `System.Speech` | `10.0.2` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`
- `Lyo.Tts`
- `Lyo.Tts.Models`

## Public API (generated)

Top-level `public` types in `*.cs` (*5*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `Metrics`
- `WindowsSpeechTtsService`
- `WindowsTtsRequest`

<!-- LYO_README_SYNC:END -->

