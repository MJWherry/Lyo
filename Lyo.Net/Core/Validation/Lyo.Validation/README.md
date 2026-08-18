# Lyo.Validation

`Lyo.Validation` contains reusable C# validators, fluent rule builders, validation attributes, and adapters that return structured `Lyo.Result.Result<T>` failures. Errors are populated through `Lyo.Result.Error` so callers can render Problem Details, log structurally, or aggregate into `BulkResult`.

Schemas are named `WhereClause` documents (`ValidationSchema`) that hosts load from an API or [`Lyo.Validation.Postgres`](../Lyo.Validation.Postgres/README.md). Evaluation reuses `IWhereClauseService.ExplainMatch`; failed nodes become errors via `WhereClauseExplainResult.ToErrors`.

## Features

- **Fluent + attributes** — `ValidatorBuilder<T>` / `PropertyValidatorBuilder<T, TProperty>` and `AttributeValidator<T>`
- **Schema documents** — `ValidationSchema` keyed by name, optional target type, `WhereClause` constraints (`In`, `NotIn`, `Regex`, …)
- **Pluggable store** — `IValidationSchemaStore` (in-memory default; Postgres in a sibling package)
- **Shared error mapping** — `WhereClauseExplainResult.ToErrors` in Query.Models (not a Validation-local builder)

## Examples

### Typical usage

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

### Attribute-based validation

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

### Load a WhereClause schema

```csharp
services.AddLyoQueryServices();
services.AddValidation();
services.AddQueryValidationEvaluator();
// or services.AddPostgresValidationStoreFromConfiguration(configuration);

var validator = await compiler.GetAsync<CreateUserRequest>("signup.v2", ct);
var result = validator.Validate(request);
```

## What lives here

- Fluent validator composition via `ValidatorBuilder<T>` and `PropertyValidatorBuilder<T, TProperty>` (including `In` / `NotIn` for scalar allow-lists).
- Attribute-based validation through built-in attributes such as `Required`, `NotEmpty`, `NotWhiteSpace`, `Length`, `Regex`, `Email`, `Phone`, `Uri`, and `Range` (in `Lyo.Validation.Attributes`).
- Data-driven `ValidationSchema` documents: a named `WhereClause` tree (same operators as query filters) compiled to `IValidator<T>` via `IValidationSchemaCompiler`.
- `AttributeValidator<T>` for reflection-driven validation across both Lyo's `ValidationAttributeBase` and `System.ComponentModel.DataAnnotations` (including `IValidatableObject`).
- Structured property metadata via `ValidationMetadataKeys` (`PropertyName`, `AttemptedValue`) attached to each failing `Error.Metadata`.
- Stable error codes via `Lyo.Result.ValidationErrorCodes` (e.g. `NullValue`, `EmptyValue`, `InvalidLength`, `InvalidEmail`, `OutOfRange`, `ValidationFailed`).

## Attribute-based validation

`AttributeValidator<T>` reads validation attributes from `T`'s public, instance-readable properties (compiled property getters are cached statically per `T`). It composes Lyo's own
`ValidationAttributeBase` attributes with `System.ComponentModel.DataAnnotations.ValidationAttribute`, and — when `T` implements `IValidatableObject` — also runs
`Validate(ValidationContext)`. `AttributeValidator<T>.Shared` exposes a cached singleton; convenience extension `value.ValidateWithAttributes()` calls it.

DataAnnotations error codes are mapped onto `Lyo.Result.ValidationErrorCodes` (e.g. `RequiredAttribute` → `RequiredValue`, `EmailAddressAttribute` → `InvalidEmail`,
`RangeAttribute` → `OutOfRange`); anything not mapped falls back to `ValidationFailed`.

## Database-backed schemas

`ValidationSchema` is the wire DTO hosts PUT/GET on their own API. Constraints are a polymorphic `WhereClause` (`$type`: `condition` / `group`) using `ComparisonOperatorEnum` (`Equals`, `In`, `NotIn`, `Regex`, …). An instance is valid iff the query engine reports a match.

Register `AddValidation()` plus `AddQueryValidationEvaluator()` (after `AddLyoQueryServices()`). Persist with [`Lyo.Validation.Postgres`](../Lyo.Validation.Postgres/README.md) (`AddPostgresValidationStoreFromConfiguration`). `WhereClauseValidator<T>` calls `ExplainMatch` then `ToErrors` — do not add a second error mapper in API code.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Query` — (direct, lyo)
- `Lyo.Query.Models` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` — (direct, microsoft)
- `Lyo.Cache` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Health` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)