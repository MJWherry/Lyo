# Lyo.Translation.Aws

[Amazon Translate](https://docs.aws.amazon.com/translate/) implementation of [`ITranslationService`](../Lyo.Translation/README.md): translate text, bounded **bulk** translation,
pragmatic **language detection**, and **`ListLanguages`** connection checks.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Registration (dependency injection)

```csharp
using Lyo.Translation.Aws;
using Microsoft.Extensions.DependencyInjection;

// From configuration section "AwsTranslationOptions" — also registers IAmazonTranslate from the
// same section if no IAmazonTranslate is already in the container.
services.AddAwsTranslationServiceFromConfiguration(configuration);

// Inline configuration
services.AddAwsTranslationService(o =>
{
    o.Region = "us-east-1";
    // Prefer IAM/instance profile when possible instead of keys
});

// Pre-built options instance
services.AddAwsTranslationService(new AwsTranslationOptions { Region = "us-east-1" });
```

`AddAwsTranslationService` and `AddAwsTranslationServiceFromConfiguration` register:

- `AwsTranslationOptions` (singleton).
- `AwsTranslationService` (singleton; subclass of `TranslationServiceBase`).
- `ITranslationService` resolved from `AwsTranslationService`.

`AddAwsTranslationServiceFromConfiguration` also bootstraps `IAmazonTranslate` from
`AwsTranslationOptions` if no `IAmazonTranslate` is already registered. The other two overloads do
**not** register an `IAmazonTranslate`; bring your own if you want explicit credentials or sharing.
The service constructor accepts an optional `IAmazonTranslate` and resolves one from DI when present.

Ensure `AwsTranslationOptions` (and optionally `IAmazonTranslate`) agree on region and credentials.

## `AwsTranslationOptions`

Inherits everything on [`TranslationServiceOptions`](../Lyo.Translation/README.md). Adds:

| Property          | Type      | Default     | Purpose                                              |
|-------------------|-----------|-------------|------------------------------------------------------|
| `AccessKeyId`     | `string?` | `null`      | Static AWS access key id (prefer IAM roles instead). |
| `SecretAccessKey` | `string?` | `null`      | Static AWS secret key (prefer IAM roles instead).    |
| `Region`          | `string`  | `us-east-1` | AWS region for the Translate client.                 |
| `ServiceUrl`      | `string?` | `null`      | Override endpoint (for local testing).               |

Configuration section name: `AwsTranslationOptions.SectionName = "AwsTranslationOptions"`.

## Behaviour notes

| Feature               | Detail                                                                                                        |
|-----------------------|---------------------------------------------------------------------------------------------------------------|
| Language codes        | Target/source are mapped to ISO 639-1 (and BCP-47 prefixes) expected by Translate                             |
| `DetectLanguageAsync` | Uses `TranslateText` with `auto` source and English target to infer source language                           |
| Metrics               | Provider keys in `Constants.Metrics` remap the base keys in [`Lyo.Translation`](../Lyo.Translation/README.md) |

## Related projects

- [`Lyo.Translation`](../Lyo.Translation/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- NuGet: `AWSSDK.Translate`
