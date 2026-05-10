# Lyo.Tts.AwsPolly

[Amazon Polly](https://docs.aws.amazon.com/polly/) integration: `AwsPollyTtsService` extends `TtsServiceBase<AwsPollyTtsRequest>` with voice selection, output formats, bulk synthesis, metrics, and DI helpers.

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

services.AddAmazonPollyFromConfiguration(configuration); // registers IAmazonPolly + AwsPollyOptions
services.AddAwsPollyTtsServiceFromConfiguration(configuration);
// ITtsService<AwsPollyTtsRequest> and AwsPollyTtsService registered
```

For a minimal [`ITtsService`](../Lyo.Tts/README.md), register `AwsPollyTtsAppService` beside `AwsPollyTtsService` if your host only exposes the non-generic contract.

Example `appsettings.json` snippets appear in XML documentation on `AddAwsPollyTtsServiceFromConfiguration`.

## Behaviour notes

| Area | Detail |
|------|--------|
| Voices | `AwsPollyVoiceId` maps to Polly [`VoiceId`](https://docs.aws.amazon.com/polly/latest/dg/voicelist.html) values |
| Language | `LanguageCode` on `AwsPollyTtsRequest` is primarily for selection; a fixed `VoiceId` determines spoken language |
| Metrics | `Constants.Metrics` uses `tts.awspolly.*` keys (distinct from [`Lyo.Tts`](../Lyo.Tts/README.md)) |

## Dependencies

*(Aligned with [`Lyo.Tts.AwsPolly.csproj`](Lyo.Tts.AwsPolly.csproj).)*

### NuGet packages

| Package | Version |
|---------|---------|
| `AWSSDK.Polly` | `[4.0,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Tts`](../Lyo.Tts/README.md)
