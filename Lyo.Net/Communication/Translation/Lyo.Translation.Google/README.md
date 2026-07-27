# Lyo.Translation.Google

Google Translate implementation of `ITranslationService` for the Lyo stack. `GoogleTranslationService`
extends `TranslationServiceBase` and talks to the Google Cloud Translation v2 REST API over HTTP.

**Target frameworks:** `netstandard2.0;net10.0`

## Dependency injection

```csharp
using Lyo.Translation.Google;

services.AddGoogleTranslationServiceFromConfiguration(configuration);
// Override the configuration section name (default: "GoogleTranslationOptions"):
// services.AddGoogleTranslationServiceFromConfiguration(configuration, "MyGoogle");
```

Other entry points on `IServiceCollection`:

- `AddGoogleTranslationService(Action<GoogleTranslationOptions> configure)` — inline configuration.
- `AddGoogleTranslationService(GoogleTranslationOptions options)` — pre-built options instance.

All three overloads register:

- `GoogleTranslationOptions` (singleton).
- `GoogleTranslationService` (singleton; subclass of `TranslationServiceBase`).
- `ITranslationService` resolved from `GoogleTranslationService`.

The service constructor accepts an optional `HttpClient` — if not registered in DI, a private
`HttpClient` is created and disposed with the service.

## `GoogleTranslationOptions`

Inherits everything on [`TranslationServiceOptions`](../Lyo.Translation/README.md). Adds:

| Property                 | Type      | Default                                                    | Purpose                                                                                   |
|--------------------------|-----------|------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| `ApiKey`                 | `string?` | `null`                                                     | Google Cloud API key. Required by `TranslateCoreAsync` — the service throws when missing. |
| `ProjectId`              | `string?` | `null`                                                     | GCP project id (used when integrating with service-account-style auth).                   |
| `ServiceAccountJsonPath` | `string?` | `null`                                                     | Optional path to a service-account credential JSON file.                                  |
| `ApiEndpoint`            | `string`  | `https://translation.googleapis.com/language/translate/v2` | REST endpoint base; trailing slashes are stripped at startup.                             |

Configuration section name: `GoogleTranslationOptions.SectionName = "GoogleTranslationOptions"`.

## Behaviour notes

| Area           | Detail                                                                                                                                                                                                                             |
|----------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| API            | POSTs to `{ApiEndpoint}?key={ApiKey}` with `{ q, target, source }` (source omitted when not provided for auto-detect).                                                                                                             |
| Language codes | Google Translate uses ISO 639-1; the service derives that from `LanguageCodeInfo.Iso6391` (falls back to the first segment of `Bcp47`).                                                                                            |
| Detection      | `TranslateCoreAsync` surfaces the API's `detectedSourceLanguage` via `LanguageCodeInfo.FromISO639_1` when present.                                                                                                                 |
| Metrics        | Overrides the base `MetricNames` with `translation.google.*` (see `Lyo.Translation.Google.Constants.Metrics`).                                                                                                                     |
| Disposal       | The HTTP client created by the service is disposed by `Dispose`; an externally-supplied `HttpClient` is also disposed when passed in, so prefer injecting a long-lived client via DI and letting the consumer manage its lifetime. |

## Related projects

- [`Lyo.Translation`](../Lyo.Translation/README.md)
- [`Lyo.Exceptions`](../../../Core/Exceptions/Lyo.Exceptions/README.md)
- [`Lyo.Translation.Aws`](../Lyo.Translation.Aws/README.md)
