# Lyo.Privacy.AspNetCore

ASP.NET Core DI integration for [`Lyo.Privacy`](../Lyo.Privacy/README.md): registers `ITextRedactor` / `IStructuredRedactor`, binds `PrivacyRedactorOptions` from configuration, and
supports keyed per-tenant or per-feature policies.

## Extensions — `PrivacyServiceCollectionExtensions`

| Extension                                                                                                                                                                              | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `services.AddLyoPrivacy(IConfiguration? configuration = null, Action<PrivacyRedactorOptions>? configureOptions = null, Action<RedactionPolicyBuilder>? configureDefaultPolicy = null)` | Binds `PrivacyRedactorOptions` from configuration (section `PrivacyRedactorOptions.SectionName`) when supplied, applies an optional inline overrides callback, and registers `ITextRedactor` (`TextRedactor`) + `IStructuredRedactor` (`JsonRedactor`) as singletons. The structured redactor reuses the text redactor when `PrivacyRedactorOptions.JsonApplyTextRulesToStrings` is `true`. `IMetrics` is consumed when present; falls back to `NullMetrics.Instance`. |
| `services.AddLyoPrivacyPolicy(object serviceKey, Action<RedactionPolicyBuilder> configure)`                                                                                            | Registers a **keyed** `ITextRedactor` built from a custom `RedactionPolicyBuilder`. Useful when one host needs different redaction posture per workload (e.g. `"Support"`, `"Marketing"`). The policy's `Name` defaults to `serviceKey.ToString()` when not set.                                                                                                                                                                                                       |

## Quick start

```csharp
using Lyo.Privacy.AspNetCore;

services.AddLyoPrivacy(builder.Configuration, configureDefaultPolicy: p => p
    .RedactEmail()
    .RedactCreditCards());

// Keyed: support-tier policy with looser PII rules
services.AddLyoPrivacyPolicy("Support", p => p
    .RedactCreditCards());
```

## Related projects

- [`Lyo.Metrics`](../../Metrics/Lyo.Metrics/README.md)
- [`Lyo.Privacy`](../Lyo.Privacy/README.md)
- [`Lyo.Privacy.Web.Components`](../Lyo.Privacy.Web.Components/README.md)
