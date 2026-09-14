# Lyo.Translation.Google

Google Cloud Translation v2 that implements `ITranslationService`. `GoogleTranslationService` extends `TranslationServiceBase` and calls the REST API over HTTP.

**Target frameworks:** `netstandard2.0;net10.0`

## Examples

### Wire into DI

```csharp
using Lyo.Translation.Google;

services.AddGoogleTranslationServiceFromConfiguration(configuration);
// Override the configuration section name (default: "GoogleTranslationOptions"):
// services.AddGoogleTranslationServiceFromConfiguration(configuration, "MyGoogle");
```

## Service registration

- `AddGoogleTranslationService(Action<GoogleTranslationOptions> configure)` takes configuration inline.
- `AddGoogleTranslationService(GoogleTranslationOptions options)` takes a ready-made options instance.

## `GoogleTranslationOptions`

Inherits all of [`TranslationServiceOptions`](../Lyo.Translation/README.md). and adds:

| Property | Type | Default | Purpose |
| ------------------------ | --------- | ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `ApiKey` | `string?` | `null` | Google Cloud API key. Required by `TranslateCoreAsync`. The service throws when that key is missing. |
| `ProjectId` | `string?` | `null` | GCP project id, used with service-account credentials. |
| `ServiceAccountJsonPath` | `string?` | `null` | Optional path to a service-account credentials JSON file. |
| `ApiEndpoint` | `string` | `https://translation.googleapis.com/language/translate/v2` | REST endpoint base; trailing slashes are dropped at startup. |

Configuration section name: `GoogleTranslationOptions.SectionName = "GoogleTranslationOptions"`.

## Runtime notes

| Area | Detail |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| API | POSTs to `{ApiEndpoint}?key={ApiKey}` with `{ q, target, source }` (source left out when unset so the API can auto-detect). |
| Language codes | Google Translate uses ISO 639-1; the service takes that from `LanguageCodeInfo.Iso6391` (falls back to the first segment of `Bcp47`). |
| Detection | `TranslateCoreAsync` maps the API's `detectedSourceLanguage` through `LanguageCodeInfo.FromISO639_1` when that field is present. |
| Metrics | Replaces the base `MetricNames` with `translation.google.*` (see `Lyo.Translation.Google.Constants.Metrics`). |
| Disposal | `Dispose` disposes the HTTP client the service created. An externally supplied `HttpClient` is also disposed when passed in, so inject a long-lived client via DI and letting the consumer manage its lifetime. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Translation` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)