# Lyo.Validation

`Lyo.Validation` contains reusable C# validators, fluent rule builders, validation attributes, and adapters that return structured `Lyo.Result.Result<T>` failures. Errors are
populated through `Lyo.Result.Error` so callers can render Problem Details, log structurally, or aggregate into `BulkResult`.

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

## What lives here

- Fluent validator composition via `ValidatorBuilder<T>` and `PropertyValidatorBuilder<T, TProperty>`.
- Attribute-based validation through built-in attributes such as `Required`, `NotEmpty`, `NotWhiteSpace`, `Length`, `Regex`, `Email`, `Phone`, `Uri`, and `Range` (in `Lyo.Validation.Attributes`).
- `AttributeValidator<T>` for reflection-driven validation across both Lyo's `ValidationAttributeBase` and `System.ComponentModel.DataAnnotations` (including `IValidatableObject`).
- Structured property metadata via `ValidationMetadataKeys` (`PropertyName`, `AttemptedValue`) attached to each failing `Error.Metadata`.
- Stable error codes via `Lyo.Result.ValidationErrorCodes` (e.g. `NullValue`, `EmptyValue`, `InvalidLength`, `InvalidEmail`, `OutOfRange`, `ValidationFailed`).

## Attribute-based validation

`AttributeValidator<T>` reads validation attributes from `T`'s public, instance-readable properties (compiled property getters are cached statically per `T`). It composes Lyo's own
`ValidationAttributeBase` attributes with `System.ComponentModel.DataAnnotations.ValidationAttribute`, and — when `T` implements `IValidatableObject` — also runs
`Validate(ValidationContext)`. `AttributeValidator<T>.Shared` exposes a cached singleton; convenience extension `value.ValidateWithAttributes()` calls it.

DataAnnotations error codes are mapped onto `Lyo.Result.ValidationErrorCodes` (e.g. `RequiredAttribute` → `RequiredValue`, `EmailAddressAttribute` → `InvalidEmail`,
`RangeAttribute` → `OutOfRange`); anything not mapped falls back to `ValidationFailed`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Result` — (direct, lyo)
- `System.ComponentModel.Annotations` `5.0.0` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)