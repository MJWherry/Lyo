# Lyo.Tts.AwsPolly

[Amazon Polly](https://docs.aws.amazon.com/polly/) integration: `AwsPollyTtsService` extends `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk
synthesis, metrics, and DI helpers.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Quick start (code)

```csharp
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Tts.AwsPolly;

var options = new AwsPollyOptions
{
    Region = "us-east-1",
    DefaultVoiceId = nameof(AwsPollyVoiceId.Joanna),
    DefaultLanguageCode = LanguageCodeInfo.EnUs,
    DefaultOutputFormat = AudioFormat.Mp3
};

await using var service = new AwsPollyTtsService(options);
var result = await service.SynthesizeAsync("Hello, world!");

if (result.IsSuccess && result.AudioData is { Length: > 0 })
{
    await File.WriteAllBytesAsync("out.mp3", result.AudioData);
}
```

Prefer **IAM roles**, environment credentials, or the shared credentials file instead of embedding `AccessKeyId` / `SecretAccessKey`.

## Dependency injection

```csharp
using Lyo.Tts.AwsPolly;

// Configuration-bound registration: registers IAmazonPolly + AwsPollyOptions if missing,
// then registers AwsPollyTtsService, ITtsService<AwsPollyTtsRequest>, AwsPollyTtsAppService, ITtsService.
services.AddAwsPollyTtsServiceFromConfiguration(configuration);

// Or with an inline configurator (also registers AwsPollyTtsAppService + ITtsService):
services.AddAwsPollyTtsService(options =>
{
    options.Region = "us-east-1";
    options.DefaultVoiceId = nameof(AwsPollyVoiceId.Joanna);
});

// AddAmazonPollyFromConfiguration is also exposed on its own for hosts that want to
// register IAmazonPolly + AwsPollyOptions without the Lyo TTS surface.
services.AddAmazonPollyFromConfiguration(configuration);
```

`AddAwsPollyTtsService` and `AddAwsPollyTtsServiceFromConfiguration` both register:

- `AwsPollyTtsService` (the singleton implementation, subclass of `TtsServiceBase<AwsPollyTtsRequest>`).
- `ITtsService<AwsPollyTtsRequest>` resolved from the singleton above.
- `AwsPollyTtsAppService` — a thin adapter that converts `TtsResult<AwsPollyTtsRequest>` into
  `TtsSynthesisResult`.
- `ITtsService` (non-generic) backed by `AwsPollyTtsAppService`, so callers that only need the simple
  contract can resolve it directly without referencing the Polly-specific types.

Example `appsettings.json` snippets appear in XML documentation on `AddAwsPollyTtsServiceFromConfiguration`.

## Behaviour notes

| Area     | Detail                                                                                                                                                                       |
|----------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Voices   | `AwsPollyVoiceId` maps to Polly [`VoiceId`](https://docs.aws.amazon.com/polly/latest/dg/voicelist.html) values                                                               |
| Language | `LanguageCode` on `AwsPollyTtsRequest` is primarily for selection; a fixed `VoiceId` determines spoken language                                                              |
| Metrics  | `Constants.Metrics` uses `tts.awspolly.*` keys (distinct from [`Lyo.Tts`](../Lyo.Tts/README.md))                                                                             |
| Adapter  | `AwsPollyTtsAppService` adapts `AwsPollyTtsService` to `ITtsService.SynthesizeAsync(text, voiceId)`, surfacing the first error message on failure and audio bytes on success |

## Dependencies

*(Aligned with [`Lyo.Tts.AwsPolly.csproj`](Lyo.Tts.AwsPolly.csproj).)*

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `AWSSDK.Polly`                              | `[4.0,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options`              | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Tts`](../Lyo.Tts/README.md)
