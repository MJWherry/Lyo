# Lyo.Tts.Typecast

Typecast Text-to-Speech service implementation for the Lyo framework.

## Features

- Convert text to speech using the Typecast API
- Support for multiple voices and languages
- Bulk synthesis operations
- Metrics tracking
- Event notifications

## Usage

### Basic Usage

```csharp
var options = new TypecastOptions
{
    ApiKey = "your-api-key",
    DefaultVoiceId = "voice-id",
    DefaultLanguageCode = "en-US"
};

var service = new TypecastTtsService(options);
var result = await service.SynthesizeAsync("Hello, world!");

if (result.IsSuccess)
{
    // Use result.AudioData
}
```

## Configuration

The service can be configured via options or dependency injection.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Tts.Typecast.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `System.Text.Json` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Tts`
- `Lyo.Typecast.Client`

## Public API (generated)

Top-level `public` types in `*.cs` (*7*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `IsExternalInit`
- `Metrics`
- `TypecastOptions`
- `TypecastTtsAppService`
- `TypecastTtsService`

<!-- LYO_README_SYNC:END -->

