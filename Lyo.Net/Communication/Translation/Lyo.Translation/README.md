# Lyo.Translation

Contracts and shared behaviour for **machine translation** in Lyo: `ITranslationService`, `TranslationServiceBase` (bulk + metrics + events), error codes, and metric key names.

**Target frameworks:** `netstandard2.0`, `net10.0`

## Main types

| Type | Role |
|------|------|
| `ITranslationService` | Translate (string or request), bulk translate, detect language, connection test |
| `TranslationServiceBase` | Shared bulk pipeline; subclass implements `TranslateCoreAsync` |
| `TranslationErrorCodes` | Stable failure codes |
| `Constants.Metrics` | Default metric keys (AWS/Google implementations remap to provider-specific names) |
| `Extensions` | `AddTranslationService<TService,TOptions>(…)` registration helpers |

Domain models live under `Models/`: `TranslationRequest`, `TranslationResult`, `TranslationServiceOptions`, and event argument types.

## Related projects

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md) — [`LanguageCodeInfo`](../../../Core/Common/Lyo.Common/Records/LanguageCodeInfo.cs)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Result`](../../../Core/Result/Lyo.Result/README.md)
