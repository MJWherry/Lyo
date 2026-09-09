# Lyo.Configuration.Validation

Takes the same `ValidationSchema` documents the rest of the suite uses — a named `WhereClause` tree, typically published by an API — and applies them to a host's configuration.

Two entry points cover the two useful shapes. `ValidateSectionAsync<TOptions>` binds a section with `LyoOptions.Bind` and validates the bound instance, so the query engine evaluates rules against real CLR properties. `ValidateKeysAsync` skips binding and resolves each rule's field path as a configuration key, which is what you want for keys no options type owns, or for checking configuration before any options type exists.

Comparison semantics are never reimplemented here. `ConfigurationClauseEvaluator` walks the clause tree and reads keys, then hands each leaf to the where-clause engine wrapped in a `ConfigurationProbe<TValue>`, so operators behave exactly as they do for entities.

## Features

- **Raw-key rules.** Dotted rule paths become configuration keys in `ConfigurationClauseEvaluator` (`Database.Port` → `Database:Port`).
- **Bound-options rules.** `ValidateSectionAsync<TOptions>` binds the section first, then validates the POCO.
- **Operators unchanged.** Leaf comparisons go to `IWhereClauseService`, so `Regex`, `In`, `Contains`, and numeric compares match query behaviour.
- **Collection size.** A trailing `Count` segment reads the child count of the section it names, so `Hosts.Count` works over an array section.
- **Self-contained registration.** `AddConfigurationValidation` `TryAdd`s the lightweight evaluator, so no full query stack is required.

## Examples

### Check configuration keys against an API-published schema

```csharp
using Lyo.Configuration.Validation;

services.AddConfigurationValidationFromConfiguration(configuration);

// Seed rules however the host gets them (API client, Postgres store, literal).
await store.SaveAsync(new ValidationSchema {
    Key = "host.startup.v1",
    Constraints = new GroupClause(GroupOperatorEnum.And, [
        new ConditionClause("Database.Port", ComparisonOperatorEnum.GreaterThan, 0),
        new ConditionClause("Database.Host", ComparisonOperatorEnum.NotEquals, ""),
    ]),
}, ct);

var result = await validator.ValidateKeysAsync("host.startup.v1", ct: ct);
if (result.IsFailure)
    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Message)));
```

### Check a bound options type

```csharp
var result = await validator.ValidateSectionAsync<DatabaseOptions>("database.v1", DatabaseOptions.SectionName, ct);
DatabaseOptions options = result.ValueOrThrow();
```

### Options

```json
{
  "ConfigurationValidation": {
    "TreatDotAsKeyDelimiter": true,
    "FailWhenSchemaMissing": true,
    "DefaultSectionName": null
  }
}
```

## Package contents

- `ConfigurationValidator` / `IConfigurationValidator` — `ValidateKeysAsync` and `ValidateSectionAsync<TOptions>`.
- `ConfigurationClauseEvaluator` — an `IValidationClauseEvaluator` that reads configuration keys and passes non-configuration targets straight through to the inner evaluator.
- `ConfigurationProbe<TValue>` — the single-property carrier used to hand one parsed configuration value to the where-clause engine.
- `ConfigurationValidationOptions` — missing-schema behaviour, key delimiter handling, and the default section.
- `Extensions.AddConfigurationValidation` — the three house registration overloads.

## How a rule reaches a configuration value

There is nothing for the query engine to reflect over, because configuration is a flat string store. The evaluator therefore owns two things and delegates the rest.

**Key lookup** is one: a rule field of `Database.Port` becomes `Database:Port` (unless `TreatDotAsKeyDelimiter` is off), and a trailing `Count` segment falls back to the child count of the named section. The **tree walk** is the other, building the `WhereClauseExplainNode` tree that `WhereClauseExplainAnalysis` then turns into a blocking path, a failure summary, and per-branch outcomes for failed `Or` groups.

**Comparison** is not owned here. Each leaf is parsed into the shape implied by the rule's own filter literal — an integer literal yields `ConfigurationProbe<long?>`, a JSON string yields `ConfigurationProbe<string>`, and so on — and the condition is re-asked of the inner evaluator with its field rewritten to `Value`. A value that will not parse is left null, which fails positive operators and satisfies negative ones, the same shape as a missing key.

## Registration

The schema store and compiler (`AddValidation`), the lightweight `WhereClauseEvaluator` and its `ValueConversionService`, and a `ConfigurationClauseEvaluator` wrapping `WhereClauseServiceEvaluator` as the host's `IValidationClauseEvaluator` are what `AddConfigurationValidation` registers.

Every registration is a `TryAdd`, so a host that already called `AddLyoQueryServices()` keeps its cached, metered where-clause service and this package layers on top of it. Because the evaluator passes non-`IConfiguration` targets through, that single registration also serves ordinary object validation.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Query.Evaluation` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)