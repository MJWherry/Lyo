# Lyo.Validation.Models

`Lyo.Validation`'s contract half. Hosts that only exchange or persist validation rules — an API that returns `ValidationSchema` documents, a Postgres store that saves them — take these types and skip the validators, builders, and attributes that implement them.

The projection from a match explanation to errors lives here as well. `WhereClauseExplainResult.ToErrors` used to sit in `Lyo.Query.Models`, which made the query model reference `Lyo.Result` for one validation-only method. That edge went away when the method moved.

The `Lyo.Validation` namespace stayed on the types, so importers did not have to change when they moved.

## Features

- **Schema documents.** `ValidationSchema` — optional target type, key, `WhereClause` constraints, and per-field message overrides.
- **Message overrides.** Dotted field paths key `ValidationMessage` and `WhereClauseErrorOverride`.
- **Contracts.** `IValidationRule<T>`, `IValidator<T>`, `IValidationSchemaCompiler`, `IValidationSchemaStore`, `IValidationClauseEvaluator`.
- **Error projection.** A failed explain tree becomes `Lyo.Result.Error`s through `WhereClauseExplainResultExtensions.ToErrors`.
- **Metadata keys.** Each error attaches `PropertyName` / `AttemptedValue` via `ValidationMetadataKeys`.

## Examples

### Wire shape of a schema

```csharp
using Lyo.Validation;

var schema = new ValidationSchema {
    Key = "signup.v2",
    TargetTypeName = nameof(CreateUserRequest),
    Constraints = new GroupClause(GroupOperatorEnum.And, [
        new ConditionClause("Email", ComparisonOperatorEnum.Regex, @"^[^@]+@[^@]+$"),
        new ConditionClause("Age", ComparisonOperatorEnum.GreaterThanOrEqual, 18),
    ]),
    Messages = new Dictionary<string, ValidationMessage> {
        ["Age"] = new() { ErrorCode = "AGE_MIN", ErrorMessage = "Must be 18 or older." },
    },
};
```

## Package contents

- `ValidationMessage`, `ValidationSchema` — the serializable rule document.
- `IValidationRule<T>`, `IValidator<T>`, `ValidationRule<T>` — validation abstractions and the delegate-backed rule.
- `IValidationSchemaCompiler`, `IValidationSchemaStore` — compile and load schemas.
- `IValidationClauseEvaluator` — where a schema meets whatever evaluates its clause.
- `WhereClauseExplainResultExtensions`, `WhereClauseErrorOverride` — explain tree to `Error` projection.
- `ValidationMetadataKeys` — metadata keys emitted by validation rules.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)