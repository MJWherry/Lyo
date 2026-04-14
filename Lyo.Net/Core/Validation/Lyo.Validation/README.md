# Lyo.Validation

`Lyo.Validation` contains reusable C# validators, fluent rule builders, validation attributes, and adapters that return structured `Result` failures.

## What lives here

- Fluent validator composition via `ValidatorBuilder<T>` and `PropertyValidatorBuilder<T, TProperty>`.
- Attribute-based validation through built-in attributes such as `NotWhiteSpace`, `Length`, `Email`, `Uri`, and `Range`.
- DataAnnotations and `IValidatableObject` support through `AttributeValidator<T>`.
- Structured property metadata via `ValidationMetadataKeys` for downstream error handling.

## Typical usage

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

## Related projects

- `Lyo.Common`: shared `Result`, `Error`, and metadata contracts.
- `Lyo.Exceptions`: guard helpers and error utilities used by validation rules.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Validation.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `System.ComponentModel.Annotations` | `5.0.0` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*19*). Nested types and file-scoped namespaces may omit some entries.

- `AttributeValidationExtensions`
- `AttributeValidator`
- `EmailAttribute`
- `IValidationRule`
- `IValidator`
- `LengthAttribute`
- `NotEmptyAttribute`
- `NotWhiteSpaceAttribute`
- `PhoneAttribute`
- `PropertyValidatorBuilder`
- `RangeAttribute`
- `RegexAttribute`
- `RequiredAttribute`
- `UriAttribute`
- `ValidationAttributeBase`
- `ValidationMetadataKeys`
- `ValidationRule`
- `Validator`
- `ValidatorBuilder`

<!-- LYO_README_SYNC:END -->

