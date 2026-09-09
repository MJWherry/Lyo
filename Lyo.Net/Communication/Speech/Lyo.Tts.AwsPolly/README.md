# Lyo.Tts.AwsPolly

[Amazon Polly](https://docs.aws.amazon.com/polly/) TTS. `AwsPollyTtsService` subclasses `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk synthesis, metrics, and DI helpers.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Examples

### Start here

```csharp
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
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

### Wire into DI

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

## Start here

Use **IAM roles**, environment credentials, or the shared credentials file rather than embedding `AccessKeyId` / `SecretAccessKey`.

## Service registration

- `AwsPollyTtsService` (the singleton implementation, a subclass of `TtsServiceBase<AwsPollyTtsRequest>`).
- `ITtsService<AwsPollyTtsRequest>` resolved from that singleton.
- `AwsPollyTtsAppService` converts `TtsResult<AwsPollyTtsRequest>` into `TtsSynthesisResult`.
- `ITtsService` (non-generic) backed by `AwsPollyTtsAppService`, so callers that only need the simple contract can resolve it without taking a dependency on Polly-specific types.

## Runtime notes

| Area | Detail |
| -------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Voices | `AwsPollyVoiceId` maps onto Polly [`VoiceId`](https://docs.aws.amazon.com/polly/latest/dg/voicelist.html) values |
| Language | `LanguageCode` on `AwsPollyTtsRequest` is mainly for selection; a fixed `VoiceId` determines spoken language |
| Metrics | `Constants.Metrics` uses `tts.awspolly.*` keys (separate from [`Lyo.Tts`](../Lyo.Tts/README.md)) |
| Adapter | `AwsPollyTtsAppService` adapts `AwsPollyTtsService` to `ITtsService.SynthesizeAsync(text, voiceId)`. Failure returns the first error message; success returns audio bytes. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Tts` (direct, lyo)
- `AWSSDK.Polly` `4.0.100.3` (direct, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Tts.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)