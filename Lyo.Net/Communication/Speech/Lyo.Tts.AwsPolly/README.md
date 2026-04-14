# Lyo.Tts.AwsPolly

AWS Polly Text-to-Speech service implementation for the Lyo framework.

## Features

- Convert text to speech using Amazon Polly
- Support for multiple voices and languages
- Bulk synthesis operations
- Metrics tracking
- Event notifications

## Usage

### Basic Usage

```csharp
var options = new AwsPollyOptions
{
    Region = "us-east-1",
    DefaultVoiceId = "Joanna",
    DefaultLanguageCode = "en-US"
};

var service = new AwsPollyTtsService(options);
var result = await service.SynthesizeAsync("Hello, world!");

if (result.IsSuccess)
{
    // Use result.AudioData
}
```

## Configuration

The service can be configured via options or dependency injection. AWS credentials can be provided via:

- AccessKeyId and SecretAccessKey in options
- IAM roles (when running on AWS)
- Environment variables
- AWS credentials file

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Tts.AwsPolly.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `AWSSDK.Polly` | `[4.0,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Tts`

## Public API (generated)

Top-level `public` types in `*.cs` (*9*). Nested types and file-scoped namespaces may omit some entries.

- `AwsPollyOptions`
- `AwsPollyTtsAppService`
- `AwsPollyTtsRequest`
- `AwsPollyTtsService`
- `AwsPollyVoiceId`
- `Constants`
- `Extensions`
- `IsExternalInit`
- `Metrics`

<!-- LYO_README_SYNC:END -->

