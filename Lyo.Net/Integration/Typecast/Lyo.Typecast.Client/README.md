# Lyo.Typecast.Client

Typecast API client for text-to-speech and voice management. `TypecastClient` extends `Lyo.Api.Client.ApiClient`, configures the `X-API-KEY` header from `TypecastClientOptions`,
and exposes two managers (`TextToSpeech`, `Voices`) for the underlying REST endpoints.

Multi-targets `netstandard2.0` and `net10.0`.

## Examples

### Register services

```csharp
// 1. Configuration-bound (defaults to the "TypecastClient" section)
services.AddTypecastClientFromConfiguration(builder.Configuration);

// 2. Inline configuration
services.AddTypecastClient(o => {
    o.ApiKey = "your-api-key";
    o.BaseUrl = "https://api.typecast.ai";
});

// 3. Pre-built options object
services.AddTypecastClient(new TypecastClientOptions { ApiKey = "your-api-key" });
```

### Register services (2)

```json
{
  "TypecastClient": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://api.typecast.ai"
  }
}
```

### `TypecastClient`

```csharp
public readonly TextToSpeechManager TextToSpeech;
public readonly VoiceManager Voices;
```

## Registration

Three DI extensions are available on `IServiceCollection`: All variants register `TypecastClient` as a singleton. `ILoggerFactory` and `HttpClient` are pulled from the container if available; otherwise sensible defaults are used. Example `appsettings.json`:

## `TypecastClientOptions`

Extends `ApiClientOptions`, so it inherits the standard HTTP transport knobs (`BaseUrl`, timeouts, retry, etc.).

| Property | Notes |
| ------------- | ---------------------------------------------------------------------------------------- |
| `ApiKey` | Required. Sent as `X-API-KEY`. |
| `BaseUrl` | Defaults to `https://api.typecast.ai`. |
| `SectionName` | `"TypecastClient"`. Used by `AddTypecastClientFromConfiguration` as the default section. |

JSON is serialized with snake_case naming, case-insensitive read, and `WhenWritingNull` ignore — matching Typecast's API.

## `TypecastClient` — `TextToSpeechManager`

| Method | Endpoint | Returns |
| ----------------------------------------------------------- | ------------------------- | ---------------------------- |
| `SynthesizeAsync(TypecastTtsRequest request, ct = default)` | `POST /v1/text-to-speech` | `byte[]` (WAV or MP3 audio). |

## `TypecastClient` — `VoiceManager`

| Method | Endpoint | Returns |
| ------------------------------------------------------------- | -------------------------- | ---------------------------------- |
| `ListVoicesAsync(VoiceListReq? request = null, ct = default)` | `GET /v2/voices` | `List<Voice>` (empty when absent). |
| `GetVoiceAsync(string voiceId, ct = default)` | `GET /v2/voices/{voiceId}` | `Voice?` |

## Request models

- `TypecastTtsRequest` (extends `Lyo.Tts.Models.TtsRequest`) — `VoiceId`, `Text`, `Model` (defaults to `TypecastModel.SsfmV30`), `Language` (`LanguageCodeInfo`, serialised as ISO 639-3), `Prompt`, `Output`, `Seed`, computed `AudioFormat`.
- `Prompt` — emotion / style settings (including the `"smart"` mode with optional `previous_text` / `next_text` context).
- `OutputSettings` — volume / pitch / tempo / audio format.
- `VoiceListReq` — optional `Model`, `Gender`, `Age`, `UseCases` filters.
- Enums under `Enums/` — `Gender`, `AgeGroup`, `TypecastModel`.
- Response models under `Models/Voices/Response/` — `Voice`, `VoiceModel`.

## `TypecastTtsRequestBuilder`

Fluent builder for assembling a valid TTS request. Validates `VoiceId` and `Text` on `Build()`.

```csharp
var request = TypecastTtsRequestBuilder
    .Create("tc_60e5426de8b95f1d3000d7b5", "Hello, world!")
    .WithModel("ssfm-v30")
    .WithLanguage("eng") // accepts ISO 639-3 or ISO 639-1
    .WithSmartPrompt(previousText: "Welcome.", nextText: "How are you?")
    .WithOutput(o => {
        o.AudioFormat = "mp3";
    })
    .WithSeed(42)
    .Build();

var audio = await client.TextToSpeech.SynthesizeAsync(request, ct);
```

| Method | Sets |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| `New()` / `Create(voiceId, text)` | Static factory methods. |
| `WithVoiceId(string)` / `WithText(string)` | Required fields. |
| `WithModel(string)` | e.g. `"ssfm-v30"`, `"ssfm-v21"`. |
| `WithLanguage(string)` | Parses ISO 639-3 first then ISO 639-1; falls back to no language when unknown. |
| `WithPrompt(Prompt)` / `WithPrompt(Action<Prompt>)` | Set or configure inline. |
| `WithSmartPrompt(previousText?, nextText?)` | Convenience for `"smart"` emotion mode. |
| `WithOutput(OutputSettings)` / `WithOutput(Action<OutputSettings>)` | Set or configure inline. |
| `WithSeed(int)` | Deterministic generation. |
| `Build()` | Validates required fields and returns the `TypecastTtsRequest`. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Tts.Models` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft)