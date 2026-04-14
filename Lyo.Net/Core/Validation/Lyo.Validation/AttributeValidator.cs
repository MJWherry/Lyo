using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Lyo.Common;
using Lyo.Validation.Attributes;
using DataAnnotationsValidationAttribute = System.ComponentModel.DataAnnotations.ValidationAttribute;
using PhoneAttribute = System.ComponentModel.DataAnnotations.PhoneAttribute;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using RequiredAttribute = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace Lyo.Validation;

/// <summary>Validates a model by reading custom and DataAnnotations attributes from its public properties.</summary>
public sealed class AttributeValidator<T> : IValidator<T>
{
    private static readonly IReadOnlyList<PropertyValidationInfo> _propertyInfos = BuildPropertyInfos();
    private static readonly bool _supportsObjectValidation = typeof(IValidatableObject).IsAssignableFrom(typeof(T));

    /// <summary>Gets a reusable validator instance for the current type.</summary>
    public static AttributeValidator<T> Shared { get; } = new();

    /// <inheritdoc />
    public Result<T> Validate(T value)
    {
        if (value is null)
            return Result<T>.Failure("Validation target cannot be null", ValidationErrorCodes.NullValue);

        List<Error>? errors = null;
        foreach (var propertyInfo in _propertyInfos) {
            var propertyValue = propertyInfo.Getter(value);
            foreach (var validator in propertyInfo.Validators) {
                var validationErrors = validator(value, propertyValue);
                if (validationErrors.Count == 0)
                    continue;

                errors ??= new(validationErrors.Count);
                errors.AddRange(validationErrors);
            }
        }

        if (_supportsObjectValidation && value is IValidatableObject validatableObject) {
            var context = new ValidationContext(value);
            foreach (var validationResult in validatableObject.Validate(context)) {
                if (validationResult == ValidationResult.Success)
                    continue;

                errors ??= [];
                AddValidationResultErrors(errors, validationResult, null, null);
            }
        }

        return errors == null || errors.Count == 0 ? Result<T>.Success(value) : Result<T>.Failure(errors);
    }

    private static IReadOnlyList<PropertyValidationInfo> BuildPropertyInfos()
    {
        var propertyInfos = new List<PropertyValidationInfo>();
        foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;

            var validators = new List<Func<T, object?, IReadOnlyList<Error>>>();
            foreach (var attribute in property.GetCustomAttributes(typeof(ValidationAttributeBase), true).Cast<ValidationAttributeBase>())
                validators.Add((instance, propertyValue) => attribute.Validate(property.Name, propertyValue, instance!));

            foreach (var attribute in property.GetCustomAttributes(typeof(DataAnnotationsValidationAttribute), true).Cast<DataAnnotationsValidationAttribute>())
                validators.Add(CreateDataAnnotationsValidator(property.Name, attribute));

            if (validators.Count == 0)
                continue;

            propertyInfos.Add(new(CreateGetter(property), validators.ToArray()));
        }

        return propertyInfos.ToArray();
    }

    private static Func<T, object?> CreateGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        var propertyAccess = Expression.Property(instance, property);
        var convert = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<T, object?>>(convert, instance).Compile();
    }

    private static Func<T, object?, IReadOnlyList<Error>> CreateDataAnnotationsValidator(string propertyName, DataAnnotationsValidationAttribute attribute)
        => (instance, propertyValue) => {
            var context = new ValidationContext(instance!) { MemberName = propertyName, DisplayName = propertyName };
            var validationResult = attribute.GetValidationResult(propertyValue, context);
            if (validationResult == ValidationResult.Success)
                return [];

            var errors = new List<Error>(1);
            AddValidationResultErrors(errors, validationResult!, propertyName, propertyValue, attribute);
            return errors;
        };

    private static void AddValidationResultErrors(
        ICollection<Error> errors,
        ValidationResult validationResult,
        string? fallbackPropertyName,
        object? attemptedValue,
        DataAnnotationsValidationAttribute? sourceAttribute = null)
    {
        var errorCode = sourceAttribute != null ? ResolveErrorCode(sourceAttribute) : ValidationErrorCodes.ValidationFailed;
        var memberNames = validationResult.MemberNames?.Where(static name => !string.IsNullOrWhiteSpace(name)).Distinct().ToArray() ?? [];
        if (memberNames.Length == 0) {
            var resolvedFallbackPropertyName = fallbackPropertyName;
            if (!string.IsNullOrWhiteSpace(resolvedFallbackPropertyName)) {
                var propertyName = resolvedFallbackPropertyName!;
                errors.Add(ValidatorBuilder<T>.CreatePropertyError(propertyName, attemptedValue, errorCode, validationResult.ErrorMessage ?? $"{propertyName} is invalid"));
                return;
            }

            errors.Add(new(validationResult.ErrorMessage ?? "Validation failed", errorCode));
            return;
        }

        foreach (var memberName in memberNames)
            errors.Add(ValidatorBuilder<T>.CreatePropertyError(memberName, attemptedValue, errorCode, validationResult.ErrorMessage ?? $"{memberName} is invalid"));

        if (string.IsNullOrWhiteSpace(fallbackPropertyName) || memberNames.Contains(fallbackPropertyName, StringComparer.Ordinal))
            return;

        errors.Add(
            ValidatorBuilder<T>.CreatePropertyError(
                fallbackPropertyName!, attemptedValue, sourceAttribute != null ? ResolveErrorCode(sourceAttribute) : ValidationErrorCodes.ValidationFailed,
                validationResult.ErrorMessage ?? $"{fallbackPropertyName} is invalid"));
    }

    private static string ResolveErrorCode(DataAnnotationsValidationAttribute attribute)
        => attribute switch {
            RequiredAttribute => ValidationErrorCodes.RequiredValue,
            MaxLengthAttribute => ValidationErrorCodes.InvalidLength,
            MinLengthAttribute => ValidationErrorCodes.InvalidLength,
            StringLengthAttribute => ValidationErrorCodes.InvalidLength,
            RangeAttribute => ValidationErrorCodes.OutOfRange,
            EmailAddressAttribute => ValidationErrorCodes.InvalidEmail,
            PhoneAttribute => ValidationErrorCodes.InvalidPhone,
            UrlAttribute => ValidationErrorCodes.InvalidUri,
            RegularExpressionAttribute => ValidationErrorCodes.InvalidFormat,
            var _ => ValidationErrorCodes.ValidationFailed
        };

    private sealed class PropertyValidationInfo
    {
        public Func<T, object?> Getter { get; }

        public IReadOnlyList<Func<T, object?, IReadOnlyList<Error>>> Validators { get; }

        public PropertyValidationInfo(Func<T, object?> getter, IReadOnlyList<Func<T, object?, IReadOnlyList<Error>>> validators)
        {
            Getter = getter;
            Validators = validators;
        }
    }
}

/// <summary>Convenience methods for running attribute-based validation.</summary>
public static class AttributeValidationExtensions
{
    /// <summary>Validates a value using validation attributes declared on its public properties.</summary>
    public static Result<T> ValidateWithAttributes<T>(this T value) => AttributeValidator<T>.Shared.Validate(value);
}