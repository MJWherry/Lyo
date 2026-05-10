# Lyo.Translation.Aws

[Amazon Translate](https://docs.aws.amazon.com/translate/) implementation of [`ITranslationService`](../Lyo.Translation/README.md): translate text, bounded **bulk** translation, pragmatic **language detection**, and **`ListLanguages`** connection checks.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Registration (dependency injection)

```csharp
using Lyo.Translation.Aws;
using Microsoft.Extensions.DependencyInjection;

// From configuration section "AwsTranslationOptions"
services.AddAwsTranslationServiceFromConfiguration(configuration);

// Or configure in code
services.AddAwsTranslationService(o =>
{
    o.Region = "us-east-1";
    // Prefer IAM/instance profile when possible instead of keys
});
```

Ensure `AwsTranslationOptions` (and optionally `IAmazonTranslate`) agree on region and credentials.

## Behaviour notes

| Feature | Detail |
|---------|--------|
| Language codes | Target/source are mapped to ISO 639-1 (and BCP-47 prefixes) expected by Translate |
| `DetectLanguageAsync` | Uses `TranslateText` with `auto` source and English target to infer source language |
| Metrics | Provider keys in `Constants.Metrics` remap the base keys in [`Lyo.Translation`](../Lyo.Translation/README.md) |

## Related projects

- [`Lyo.Translation`](../Lyo.Translation/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- NuGet: `AWSSDK.Translate`
