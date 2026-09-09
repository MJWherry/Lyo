# Lyo.Translation

**Archetype B (capability).** Providers (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) remain under `Communication/Translation/`, not `Integration/`. See [package layout](../../../docs/package-layout.md).

Contracts and shared translation behavior: `ITranslationService`, `TranslationServiceBase` (bulk pipeline, metrics, lifecycle events), error codes, metric key names, and a small DI helper.

**Target frameworks:** `netstandard2.0;net10.0`

## Examples

### Injection helpers

```csharp
services.AddTranslationService<MyTranslationService, MyTranslationOptions>(opts =>
{
    opts.DefaultSourceLanguage = LanguageCodeInfo.EnUs;
});

// or with a pre-built options instance:
services.AddTranslationService<MyTranslationService, MyTranslationOptions>(myOptions);
```

## `ITranslationService`

| Member | Description |
| ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `TranslateAsync(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null, CancellationToken ct = default)` | Convenience overload. Builds a `TranslationRequest` and applies `Options.DefaultSourceLanguage` when the caller leaves source unset. |
| `TranslateAsync(TranslationRequest request, CancellationToken ct = default)` | Translates one `TranslationRequest`. |
| `TranslateBulkAsync(IEnumerable<TranslationRequest> requests, CancellationToken ct = default)` | Bulk send capped by concurrency. Returns `IReadOnlyList<TranslationResult>`. |
| `DetectLanguageAsync(string text, CancellationToken ct = default)` | Detects the language of `text` and returns `LanguageCodeInfo`. Providers may return unknown info on failure. |
| `TestConnectionAsync(CancellationToken ct = default)` | Provider-defined connectivity check. |

## `TranslationServiceBase`

- Shared bulk pipeline using a `SemaphoreSlim` sized by `TranslationServiceOptions.BulkTranslationConcurrencyLimit` and a hard per-call cap of `TranslationServiceOptions.MaxBulkTranslationLimit`.
- Bulk results land in a `ConcurrentBag`, so output order is not guaranteed to match input order.
- Lifecycle events: `Translating`, `Translated`, `BulkTranslating`, `BulkTranslated`.
- A `MetricNames` dictionary that providers may override in the constructor.

## `TranslationServiceOptions`

| Property | Type | Default | Purpose |
| --------------------------------- | ------------------- | ------- | ---------------------------------------------------------- |
| `DefaultTargetLanguage` | `LanguageCodeInfo?` | `null` | Provider fallback used when the caller omits a target. |
| `DefaultSourceLanguage` | `LanguageCodeInfo?` | `null` | Source language used when callers leave it unset. |
| `MaxTextLength` | `int` | `50000` | Upper bound on text length providers should accept. |
| `EnableMetrics` | `bool` | `true` | When `false`, the base substitutes `NullMetrics.Instance`. |
| `BulkTranslationConcurrencyLimit` | `int` | `10` | Cap on bulk dispatch concurrency. |
| `MaxBulkTranslationLimit` | `int` | `100` | Upper bound on requests in one `TranslateBulkAsync` call. |

## Failure codes (`TranslationErrorCodes`)

| Constant | Value |
| ---------------------- | ------------------------------------ |
| `TranslateFailed` | `TRANSLATION_FAILED` |
| `OperationCancelled` | `TRANSLATION_OPERATION_CANCELLED` |
| `DetectLanguageFailed` | `TRANSLATION_DETECT_LANGUAGE_FAILED` |

## Default metric keys (`Lyo.Translation.Constants.Metrics`)

| Constant key | Metric | Kind |
| ----------------------------- | ----------------------------------------------------- | ------- |
| `TranslateDuration` | `translation.Service.translate.duration` | Timer |
| `TranslateSuccess` | `translation.Service.translate.success` | Counter |
| `TranslateFailure` | `translation.Service.translate.failure` | Counter |
| `BulkTranslateDuration` | `translation.Service.bulk.translate.duration` | Timer |
| `BulkTranslateTotal` | `translation.Service.bulk.translate.total` | Counter |
| `BulkTranslateSuccess` | `translation.Service.bulk.translate.success` | Counter |
| `BulkTranslateFailure` | `translation.Service.bulk.translate.failure` | Counter |
| `BulkTranslateLastDurationMs` | `translation.Service.bulk.translate.last_duration_ms` | Gauge |
| `DetectLanguageDuration` | `translation.Service.detectLanguage.duration` | Timer |
| `DetectLanguageSuccess` | `translation.Service.detectLanguage.success` | Counter |
| `DetectLanguageFailure` | `translation.Service.detectLanguage.failure` | Counter |

Provider packages (Google, AWS) override `CreateMetricNamesDictionary` to put a namespace on these.

## Injection helpers

`Lyo.Translation.Extensions` exposes a generic helper used by provider packages. It wires `TOptions`, `TService`, and `ITranslationService` (resolved from `TService`) as singletons. Concrete provider packages (`Lyo.Translation.Google`, `Lyo.Translation.Aws`) wrap this in their own `Add*FromConfiguration` extensions that also wire native SDK clients.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)