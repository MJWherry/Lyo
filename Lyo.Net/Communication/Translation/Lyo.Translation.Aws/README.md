# Lyo.Translation.Aws

[Amazon Translate](https://docs.aws.amazon.com/translate/) implementation of [`ITranslationService`](../Lyo.Translation/README.md). Translates text, runs bounded bulk translation, infers language via a Translate call, and probes connectivity with `ListLanguages`.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Examples

### Register with DI

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

## Register with DI

`AddAwsTranslationService` and `AddAwsTranslationServiceFromConfiguration` register:

- `AwsTranslationOptions` (singleton).
- `AwsTranslationService` (singleton; subclass of `TranslationServiceBase`).
- `ITranslationService` resolved from `AwsTranslationService`.

`AddAwsTranslationServiceFromConfiguration` also registers `IAmazonTranslate` from
`AwsTranslationOptions` if no `IAmazonTranslate` is already registered. The other two overloads do
not register an `IAmazonTranslate`. Bring your own if you want explicit credentials or sharing.
The service constructor accepts an optional `IAmazonTranslate` and resolves one from DI when present.

`AwsTranslationOptions` and `IAmazonTranslate` should agree on region and credentials.

## `AwsTranslationOptions`

Inherits everything on [`TranslationServiceOptions`](../Lyo.Translation/README.md). Adds:

| Property | Type | Default | Purpose |
| ----------------- | --------- | ----------- | ---------------------------------------------------- |
| `AccessKeyId` | `string?` | `null` | Static AWS access key id (prefer IAM roles instead). |
| `SecretAccessKey` | `string?` | `null` | Static AWS secret key (prefer IAM roles instead). |
| `Region` | `string` | `us-east-1` | AWS region for the Translate client. |
| `ServiceUrl` | `string?` | `null` | Override endpoint (for local testing). |

Configuration section name: `AwsTranslationOptions.SectionName = "AwsTranslationOptions"`.

## Behavior notes

| Feature | Detail |
| --------------------- | ------------------------------------------------------------------------------------------------------------- |
| Language codes | Target/source are mapped to ISO 639-1 (and BCP-47 prefixes) expected by Translate |
| `DetectLanguageAsync` | Uses `TranslateText` with `auto` source and English target to infer source language |
| Metrics | Provider keys in `Constants.Metrics` remap the base keys in [`Lyo.Translation`](../Lyo.Translation/README.md) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Translation` (direct, lyo)
- `AWSSDK.Translate` `4.0.100.3` (direct, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)