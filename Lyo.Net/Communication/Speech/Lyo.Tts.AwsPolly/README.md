# Lyo.Tts.AwsPolly

[Amazon Polly](https://docs.aws.amazon.com/polly/) TTS. `AwsPollyTtsService` extends `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk synthesis, metrics, and DI helpers.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Examples

### Quick start

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

### Register with DI

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

## Quick start

Prefer **IAM roles**, environment credentials, or the shared credentials file instead of embedding `AccessKeyId` / `SecretAccessKey`.

## Dependency injection

- `AwsPollyTtsService` (the singleton implementation, subclass of `TtsServiceBase<AwsPollyTtsRequest>`).
- `ITtsService<AwsPollyTtsRequest>` resolved from the singleton above.
- `AwsPollyTtsAppService` converts `TtsResult<AwsPollyTtsRequest>` into `TtsSynthesisResult`.
- `ITtsService` (non-generic) backed by `AwsPollyTtsAppService`, so callers that only need the simple contract can resolve it without referencing Polly-specific types.

## Behavior notes

| Area | Detail |
| -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Voices | `AwsPollyVoiceId` maps to Polly [`VoiceId`](https://docs.aws.amazon.com/polly/latest/dg/voicelist.html) values |
| Language | `LanguageCode` on `AwsPollyTtsRequest` is primarily for selection; a fixed `VoiceId` determines spoken language |
| Metrics | `Constants.Metrics` uses `tts.awspolly.*` keys (distinct from [`Lyo.Tts`](../Lyo.Tts/README.md)) |
| Adapter | `AwsPollyTtsAppService` adapts `AwsPollyTtsService` to `ITtsService.SynthesizeAsync(text, voiceId)`. On failure it returns the first error message. On success it returns audio bytes. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Tts` (direct, lyo)
- `AWSSDK.Polly` `4.0.100.3` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Tts.Models` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)