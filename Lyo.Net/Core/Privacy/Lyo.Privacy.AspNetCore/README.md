# Lyo.Privacy.AspNetCore

DI wiring for [`Lyo.Privacy`](../Lyo.Privacy/README.md) on ASP.NET Core: registers `ITextRedactor` / `IStructuredRedactor`, binds `PrivacyRedactorOptions` from configuration, and allows keyed per-tenant or per-feature policies.

## Examples

### First steps

```csharp
using Lyo.Privacy.AspNetCore;

services.AddLyoPrivacy(builder.Configuration, configureDefaultPolicy: p => p
    .RedactEmail()
    .RedactCreditCards());

// Keyed: support-tier policy with looser PII rules
services.AddLyoPrivacyPolicy("Support", p => p
    .RedactCreditCards());
```

## `PrivacyServiceCollectionExtensions`

| Extension | What it does |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `services.AddLyoPrivacy(IConfiguration? configuration = null, Action<PrivacyRedactorOptions>? configureOptions = null, Action<RedactionPolicyBuilder>? configureDefaultPolicy = null)` | When an `IConfiguration` is supplied, binds `PrivacyRedactorOptions` from section `PrivacyRedactorOptions.SectionName`, applies an optional inline overrides callback, and registers singleton `ITextRedactor` (`TextRedactor`) plus `IStructuredRedactor` (`JsonRedactor`). If `PrivacyRedactorOptions.JsonApplyTextRulesToStrings` is `true`, the structured redactor reuses the text redactor. `IMetrics` is used when present; otherwise `NullMetrics.Instance`. |
| `services.AddLyoPrivacyPolicy(object serviceKey, Action<RedactionPolicyBuilder> configure)` | Registers a keyed `ITextRedactor` built from a custom `RedactionPolicyBuilder`. Reach for this when one host needs different redaction rules per workload (e.g. `"Support"`, `"Marketing"`). When `Name` is unset, the policy defaults it to `serviceKey.ToString()`. |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Metrics` (direct, lyo)
- `Lyo.Privacy` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Collections.Immutable` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)