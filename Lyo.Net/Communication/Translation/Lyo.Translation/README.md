# Lyo.Translation

**Archetype B (capability).** Providers (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) stay under `Communication/Translation/`, not `Integration/`.
See [package layout](../../../docs/package-layout.md).

Contracts and shared behaviour for machine translation in Lyo: the `ITranslationService` interface, `TranslationServiceBase` (bulk pipeline + metrics + lifecycle events), error
codes, metric key names, and a small DI helper.

**Target frameworks:** `netstandard2.0;net10.0`

## Examples

### DI helpers

```csharp
services.AddTranslationService<MyTranslationService, MyTranslationOptions>(opts =>
{
    opts.DefaultSourceLanguage = LanguageCodeInfo.EnUs;
});

// or with a pre-built options instance:
services.AddTranslationService<MyTranslationService, MyTranslationOptions>(myOptions);
```

## `ITranslationService`

| Member                                                                                                                                      | Description                                                                                                      |
|---------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| `TranslateAsync(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null, CancellationToken ct = default)` | Convenience overload — builds a `TranslationRequest`, applies `Options.DefaultSourceLanguage` when not provided. |
| `TranslateAsync(TranslationRequest request, CancellationToken ct = default)`                                                                | Full request flow.                                                                                               |
| `TranslateBulkAsync(IEnumerable<TranslationRequest> requests, CancellationToken ct = default)`                                              | Concurrency-capped bulk send (returns `IReadOnlyList<TranslationResult>`).                                       |
| `DetectLanguageAsync(string text, CancellationToken ct = default)`                                                                          | Detects the language of `text`, returns `LanguageCodeInfo` (providers may return an unknown info on failure).    |
| `TestConnectionAsync(CancellationToken ct = default)`                                                                                       | Provider-defined connectivity probe.                                                                             |

## `TranslationServiceBase`

- A shared bulk pipeline with a `SemaphoreSlim` sized by `TranslationServiceOptions.BulkTranslationConcurrencyLimit` and a hard per-call limit of
  `TranslationServiceOptions.MaxBulkTranslationLimit`.
- Result ordering note: bulk results are collected through a `ConcurrentBag` so output order is **not** guaranteed to match the input order.
- Lifecycle events: `Translating`, `Translated`, `BulkTranslating`, `BulkTranslated`.
- A `MetricNames` dictionary providers can override per-provider in their constructor.

## `TranslationServiceOptions`

| Property                          | Type                | Default | Purpose                                                          |
|-----------------------------------|---------------------|---------|------------------------------------------------------------------|
| `DefaultTargetLanguage`           | `LanguageCodeInfo?` | `null`  | Provider-side fallback when the caller doesn't specify a target. |
| `DefaultSourceLanguage`           | `LanguageCodeInfo?` | `null`  | Source language assumed when callers omit one.                   |
| `MaxTextLength`                   | `int`               | `50000` | Maximum text length providers should accept.                     |
| `EnableMetrics`                   | `bool`              | `true`  | When `false`, the base swaps in `NullMetrics.Instance`.          |
| `BulkTranslationConcurrencyLimit` | `int`               | `10`    | Bulk dispatch concurrency cap.                                   |
| `MaxBulkTranslationLimit`         | `int`               | `100`   | Maximum requests per `TranslateBulkAsync` call.                  |

## Error codes (`TranslationErrorCodes`)

| Constant               | Value                                |
|------------------------|--------------------------------------|
| `TranslateFailed`      | `TRANSLATION_FAILED`                 |
| `OperationCancelled`   | `TRANSLATION_OPERATION_CANCELLED`    |
| `DetectLanguageFailed` | `TRANSLATION_DETECT_LANGUAGE_FAILED` |

## Default metric keys (`Lyo.Translation.Constants.Metrics`)

| Constant key                  | Metric                                                | Kind    |
|-------------------------------|-------------------------------------------------------|---------|
| `TranslateDuration`           | `translation.Service.translate.duration`              | Timer   |
| `TranslateSuccess`            | `translation.Service.translate.success`               | Counter |
| `TranslateFailure`            | `translation.Service.translate.failure`               | Counter |
| `BulkTranslateDuration`       | `translation.Service.bulk.translate.duration`         | Timer   |
| `BulkTranslateTotal`          | `translation.Service.bulk.translate.total`            | Counter |
| `BulkTranslateSuccess`        | `translation.Service.bulk.translate.success`          | Counter |
| `BulkTranslateFailure`        | `translation.Service.bulk.translate.failure`          | Counter |
| `BulkTranslateLastDurationMs` | `translation.Service.bulk.translate.last_duration_ms` | Gauge   |
| `DetectLanguageDuration`      | `translation.Service.detectLanguage.duration`         | Timer   |
| `DetectLanguageSuccess`       | `translation.Service.detectLanguage.success`          | Counter |
| `DetectLanguageFailure`       | `translation.Service.detectLanguage.failure`          | Counter |

Provider packages (Google, AWS) override `CreateMetricNamesDictionary` to namespace these.

## DI helpers

`Lyo.Translation.Extensions` exposes a generic helper used by provider packages: The helper registers `TOptions`, `TService`, and `ITranslationService` (resolved from `TService`)
as singletons. Concrete provider packages (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) wrap this in their own `Add*FromConfiguration` extensions that also wire up native SDK
clients.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)