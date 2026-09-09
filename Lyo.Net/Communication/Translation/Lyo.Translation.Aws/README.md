# Lyo.Translation.Aws

[Amazon Translate](https://docs.aws.amazon.com/translate/) that implements [`ITranslationService`](../Lyo.Translation/README.md). Translates text, runs bounded bulk translation, infers language via a Translate call, and probes connectivity with `ListLanguages`.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Examples

### Wire into DI

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

## Wire into DI

`AddAwsTranslationService` and `AddAwsTranslationServiceFromConfiguration` wire:

- `AwsTranslationOptions` (singleton).
- `AwsTranslationService` (singleton; subclass of `TranslationServiceBase`).
- `ITranslationService` resolved from `AwsTranslationService`.

`AddAwsTranslationServiceFromConfiguration` also registers `IAmazonTranslate` from
`AwsTranslationOptions` if no `IAmazonTranslate` is already registered. The other two overloads do
not wire an `IAmazonTranslate`. Bring your own when you want explicit credentials or sharing.
The service constructor accepts an optional `IAmazonTranslate` and resolves one from DI when present.

`AwsTranslationOptions` and `IAmazonTranslate` should agree on region and credentials.

## `AwsTranslationOptions`

Inherits all of [`TranslationServiceOptions`](../Lyo.Translation/README.md). and adds:

| Property | Type | Default | Purpose |
| ----------------- | --------- | ----------- | --------------------------------------------- |
| `AccessKeyId` | `string?` | `null` | Static AWS access key id (prefer IAM roles). |
| `SecretAccessKey` | `string?` | `null` | Static AWS secret key (prefer IAM roles). |
| `Region` | `string` | `us-east-1` | AWS region the Translate client uses. |
| `ServiceUrl` | `string?` | `null` | Override endpoint (useful for local testing). |

Configuration section name: `AwsTranslationOptions.SectionName = "AwsTranslationOptions"`.

## Runtime notes

| Feature | Detail |
| --------------------- | --------------------------------------------------------------------------------------------------------------- |
| Language codes | Target/source are mapped to ISO 639-1 (and BCP-47 prefixes) that Translate expects |
| `DetectLanguageAsync` | Uses `TranslateText` with `auto` source and English target to infer the source language |
| Metrics | Provider keys in `Constants.Metrics` remap the base keys from [`Lyo.Translation`](../Lyo.Translation/README.md) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Translation` (direct, lyo)
- `AWSSDK.Translate` `4.0.100.3` (direct, third-party)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)