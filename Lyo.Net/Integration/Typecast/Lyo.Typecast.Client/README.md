# Lyo.Typecast.Client

HTTP client for Typecast text-to-speech and voice catalog work. `TypecastClient` subclasses `Lyo.Api.Client.ApiClient`, sets the `X-API-KEY` header from `TypecastClientOptions`, and surfaces two managers (`TextToSpeech`, `Voices`) over the REST endpoints.

Multi-targets `netstandard2.0` and `net10.0`.

## Examples

### Add the client

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

### Add the client (2)

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

## Service registration

`IServiceCollection` has three DI extensions: every overload registers `TypecastClient` as a singleton. When the container has `ILoggerFactory` or `HttpClient`, those are used; otherwise the client falls back to sensible defaults. Example `appsettings.json`:

## `TypecastClientOptions`

Subclass of `ApiClientOptions`, so the usual HTTP transport knobs come along (`BaseUrl`, timeouts, retry, etc.).

| Property | Notes |
| ------------- | ----------------------------------------------------------------------------- |
| `ApiKey` | Required. Transmitted as `X-API-KEY`. |
| `BaseUrl` | Falls back to `https://api.typecast.ai`. |
| `SectionName` | `"TypecastClient"`. Default section for `AddTypecastClientFromConfiguration`. |

Serialization uses snake_case names, case-insensitive reads, and `WhenWritingNull` ignore so it matches Typecast's API.

## `TypecastClient` `TextToSpeechManager`

| Method | Endpoint | Returns |
| ----------------------------------------------------------- | ------------------------- | ------------------------------- |
| `SynthesizeAsync(TypecastTtsRequest request, ct = default)` | `POST /v1/text-to-speech` | `byte[]` (audio as WAV or MP3). |

## `TypecastClient` `VoiceManager`

| Method | Endpoint | Returns |
| ------------------------------------------------------------- | -------------------------- | ----------------------------------- |
| `ListVoicesAsync(VoiceListReq? request = null, ct = default)` | `GET /v2/voices` | `List<Voice>` (empty list if none). |
| `GetVoiceAsync(string voiceId, ct = default)` | `GET /v2/voices/{voiceId}` | `Voice?` |

## Request types

- **`TypecastTtsRequest`.** Subclasses `Lyo.Tts.Models.TtsRequest`. Fields: `VoiceId`, `Text`, `Model` (defaults to `TypecastModel.SsfmV30`), `Language` (`LanguageCodeInfo`, serialized as ISO 639-3), `Prompt`, `Output`, `Seed`, computed `AudioFormat`.
- **`Prompt`.** emotion / style knobs (including the `"smart"` mode with optional `previous_text` / `next_text` context).
- **`OutputSettings`.** volume / pitch / tempo / audio format.
- **`VoiceListReq`.** optional `Model`, `Gender`, `Age`, `UseCases` filters.
- **Enums under `Enums/`.** `Gender`, `AgeGroup`, `TypecastModel`.
- **Response models under `Models/Voices/Response/`.** `Voice`, `VoiceModel`.

## `TypecastTtsRequestBuilder`

Fluent helper that assembles a valid TTS request. `Build()` checks `VoiceId` and `Text`.

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
| ------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `New()` / `Create(voiceId, text)` | Static factories. |
| `WithVoiceId(string)` / `WithText(string)` | Mandatory fields. |
| `WithModel(string)` | e.g. `"ssfm-v30"`, `"ssfm-v21"`. |
| `WithLanguage(string)` | Tries ISO 639-3 before ISO 639-1; leaves language unset when the code is unknown. |
| `WithPrompt(Prompt)` / `WithPrompt(Action<Prompt>)` | Assign or configure in place. |
| `WithSmartPrompt(previousText?, nextText?)` | Shortcut for `"smart"` emotion mode. |
| `WithOutput(OutputSettings)` / `WithOutput(Action<OutputSettings>)` | Assign or configure in place. |
| `WithSeed(int)` | Fixed-seed generation. |
| `Build()` | Checks required fields, then returns the `TypecastTtsRequest`. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Tts.Models` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)