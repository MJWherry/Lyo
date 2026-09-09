# Lyo.Validation

Fluent rule builders, C# validators, validation attributes, and adapters that return structured `Lyo.Result.Result<T>` failures. Errors are populated through `Lyo.Result.Error` so callers can render Problem Details, log structurally, or aggregate into `BulkResult`.

Schemas are named `WhereClause` documents (`ValidationSchema`) that hosts load from an API or [`Lyo.Validation.Postgres`](../Lyo.Validation.Postgres/README.md). Evaluation reuses `IWhereClauseService.ExplainMatch`. Failed nodes become errors via `WhereClauseExplainResult.ToErrors`.

## Features

- **Fluent + attributes.** `PropertyValidatorBuilder<T, TProperty>` / `ValidatorBuilder<T>` and `AttributeValidator<T>`.
- **Schema documents.** `ValidationSchema` keyed by name, optional target type, `WhereClause` constraints (`NotIn`, `In`, `Regex`, and similar).
- **Store.** `IValidationSchemaStore` (in-memory default. Postgres in a sibling package).
- **Error mapping.** `WhereClauseExplainResult.ToErrors` in Query.Models (not a Validation-local builder).

## Examples

### Usual usage

```csharp
using Lyo.Validation;

var validator = ValidatorBuilder<CreateUserRequest>.Create()
    .RuleFor(x => x.Name)
    .NotWhiteSpace()
    .Length(2, 50)
    .RuleFor(x => x.Email)
    .Email()
    .RuleFor(x => x.Age)
    .InclusiveBetween(18, 120)
    .Build();

var result = validator.Validate(new CreateUserRequest { Name = "Matt", Email = "matt@example.com", Age = 33 });
```

### Validate via attributes

```csharp
public sealed class CreateUserRequest {
    [NotWhiteSpace]
    [Length(2, 50)]
    public string Name { get; init; } = string.Empty;

    [Email]
    public string Email { get; init; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; init; }
}

Result<CreateUserRequest> result = new CreateUserRequest { /* ... */ }.ValidateWithAttributes();
```

### Load a schema built from WhereClause

```csharp
services.AddLyoQueryServices();
services.AddValidation();
services.AddQueryValidationEvaluator();
// or services.AddPostgresValidationStoreFromConfiguration(configuration);

var validator = await compiler.GetAsync<CreateUserRequest>("signup.v2", ct);
var result = validator.Validate(request);
```

## Package contents

- Fluent validator composition via `PropertyValidatorBuilder<T, TProperty>` and `ValidatorBuilder<T>` (including `NotIn` / `In` for scalar allow-lists).
- Attribute-based validation through built-in attributes such as `NotEmpty`, `Required`, `NotWhiteSpace`, `Length`, `Regex`, `Phone`, `Email`, `Uri`, and `Range` (in `Lyo.Validation.Attributes`).
- Data-driven `ValidationSchema` documents: a named `WhereClause` tree (same operators as query filters) compiled to `IValidator<T>` via `IValidationSchemaCompiler`.
- `AttributeValidator<T>` for reflection-driven validation across both Lyo's `ValidationAttributeBase` and `System.ComponentModel.DataAnnotations` (including `IValidatableObject`).
- Structured property metadata via `ValidationMetadataKeys` (`AttemptedValue`, `PropertyName`) attached to each failing `Error.Metadata`.
- Stable error codes via `Lyo.Result.ValidationErrorCodes` (e.g. `EmptyValue`, `NullValue`, `InvalidLength`, `InvalidEmail`, `OutOfRange`, `ValidationFailed`).

## Validate via attributes

Public instance-readable properties on `T` are what `AttributeValidator<T>` validates (compiled getters are cached statically per `T`). Lyo `ValidationAttributeBase` attributes run together with
`System.ComponentModel.DataAnnotations.ValidationAttribute`. When `T` is an `IValidatableObject`,
`Validate(ValidationContext)` runs as well. The cached singleton is `AttributeValidator<T>.Shared`; `value.ValidateWithAttributes()` is the call that uses it.

`Lyo.Result.ValidationErrorCodes` is where DataAnnotations codes land (`RequiredAttribute` → `RequiredValue`, `EmailAddressAttribute` → `InvalidEmail`,
`RangeAttribute` → `OutOfRange`). Unmapped attributes become `ValidationFailed`.

## Schemas backed by a database

`ValidationSchema` is the wire DTO hosts PUT/GET on their own API. Constraints are a polymorphic `WhereClause` (`$type`: `group` / `condition`) using `ComparisonOperatorEnum` (`In`, `Equals`, `NotIn`, `Regex`, …). An instance is valid iff the query engine reports a match.

Register `AddValidation()` plus `AddQueryValidationEvaluator()` (after `AddLyoQueryServices()`). Persist with [`Lyo.Validation.Postgres`](../Lyo.Validation.Postgres/README.md) (`AddPostgresValidationStoreFromConfiguration`). `WhereClauseValidator<T>` calls `ExplainMatch` then `ToErrors`. Do not add a second error mapper in API code.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Query.Evaluation` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `Lyo.Validation.Models` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (direct, microsoft)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)