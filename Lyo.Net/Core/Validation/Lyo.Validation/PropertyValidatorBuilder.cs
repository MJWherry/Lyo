using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Lyo.Common;
using Lyo.Exceptions;

namespace Lyo.Validation;

/// <summary>Fluent property rule builder for <typeparamref name="T" /> validators.</summary>
public sealed class PropertyValidatorBuilder<T, TProperty>
{
    private readonly ValidatorBuilder<T> _builder;
    private readonly string _propertyName;
    private readonly Func<T, TProperty> _selector;

    internal PropertyValidatorBuilder(ValidatorBuilder<T> builder, Func<T, TProperty> selector, string propertyName)
    {
        ArgumentHelpers.ThrowIfNull(builder, nameof(builder));
        ArgumentHelpers.ThrowIfNull(selector, nameof(selector));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
        _builder = builder;
        _selector = selector;
        _propertyName = propertyName;
    }

    /// <summary>Requires the property value to be non-null.</summary>
    public PropertyValidatorBuilder<T, TProperty> NotNull(string errorCode = ValidationErrorCodes.NullValue, string? errorMessage = null)
    {
        _builder.AddPropertyRule(_selector, _propertyName, (_, value) => value is null ? [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} cannot be null")] : []);
        return this;
    }

    /// <summary>Requires the property value to not equal default(TProperty).</summary>
    public PropertyValidatorBuilder<T, TProperty> NotDefault(string errorCode = ValidationErrorCodes.DefaultValue, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName, (_, value) => Equals(value, default!) ? [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} cannot be the default value")] : []);

        return this;
    }

    /// <summary>Requires the property value to not be empty. Supports strings and collections.</summary>
    public PropertyValidatorBuilder<T, TProperty> NotEmpty(string errorCode = ValidationErrorCodes.EmptyValue, string? errorMessage = null)
    {
        _builder.AddPropertyRule(_selector, _propertyName, (_, value) => IsEmpty(value) ? [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} cannot be empty")] : []);
        return this;
    }

    /// <summary>Requires the string property value to not be null, empty, or whitespace.</summary>
    public PropertyValidatorBuilder<T, TProperty> NotWhiteSpace(string errorCode = ValidationErrorCodes.WhitespaceString, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName, (_, value) => {
                var text = value as string;
                return string.IsNullOrWhiteSpace(text) ? [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} cannot be null, empty, or whitespace")] : [];
            });

        return this;
    }

    /// <summary>Requires the string property value length to be within the supplied inclusive range.</summary>
    public PropertyValidatorBuilder<T, TProperty> Length(int minLength, int maxLength, string errorCode = ValidationErrorCodes.InvalidLength, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName, (_, value) => {
                var text = value as string;
                if (text == null || text.Length < minLength || text.Length > maxLength)
                    return [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} length must be between {minLength} and {maxLength}")];

                return [];
            });

        return this;
    }

    /// <summary>Requires the string property value to match the supplied regex.</summary>
    public PropertyValidatorBuilder<T, TProperty> Matches(Regex pattern, string errorCode = ValidationErrorCodes.InvalidFormat, string? errorMessage = null)
    {
        ArgumentHelpers.ThrowIfNull(pattern, nameof(pattern));
        _builder.AddPropertyRule(
            _selector, _propertyName, (_, value) => {
                var text = value as string;
                if (text == null || !pattern.IsMatch(text))
                    return [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} is not in the expected format")];

                return [];
            });

        return this;
    }

    /// <summary>Requires the string property value to be a valid email.</summary>
    public PropertyValidatorBuilder<T, TProperty> Email(string errorCode = ValidationErrorCodes.InvalidEmail, string? errorMessage = null)
        => Matches(RegexPatterns.EmailRegex, errorCode, errorMessage ?? $"{_propertyName} must be a valid email address");

    /// <summary>Requires the string property value to be a valid URI.</summary>
    public PropertyValidatorBuilder<T, TProperty> Uri(UriKind kind = UriKind.Absolute, string errorCode = ValidationErrorCodes.InvalidUri, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName, (_, value) => {
                var text = value as string;
                Uri? parsed;
                if (string.IsNullOrWhiteSpace(text) || !System.Uri.TryCreate(text, kind, out parsed))
                    return [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} must be a valid URI")];

                return [];
            });

        return this;
    }

    /// <summary>Requires the property value to be greater than the supplied minimum.</summary>
    public PropertyValidatorBuilder<T, TProperty> GreaterThan(TProperty minimum, string errorCode = ValidationErrorCodes.OutOfRange, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName,
            (_, value) => Compare(value, minimum) > 0 ? [] : [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} must be greater than {minimum}")]);

        return this;
    }

    /// <summary>Requires the property value to be greater than or equal to the supplied minimum.</summary>
    public PropertyValidatorBuilder<T, TProperty> GreaterThanOrEqualTo(TProperty minimum, string errorCode = ValidationErrorCodes.OutOfRange, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName,
            (_, value) => Compare(value, minimum) >= 0 ? [] : [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} must be greater than or equal to {minimum}")]);

        return this;
    }

    /// <summary>Requires the property value to be less than the supplied maximum.</summary>
    public PropertyValidatorBuilder<T, TProperty> LessThan(TProperty maximum, string errorCode = ValidationErrorCodes.OutOfRange, string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName,
            (_, value) => Compare(value, maximum) < 0 ? [] : [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} must be less than {maximum}")]);

        return this;
    }

    /// <summary>Requires the property value to be within the supplied inclusive range.</summary>
    public PropertyValidatorBuilder<T, TProperty> InclusiveBetween(
        TProperty minimum,
        TProperty maximum,
        string errorCode = ValidationErrorCodes.OutOfRange,
        string? errorMessage = null)
    {
        _builder.AddPropertyRule(
            _selector, _propertyName,
            (_, value) => Compare(value, minimum) >= 0 && Compare(value, maximum) <= 0
                ? []
                : [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} must be between {minimum} and {maximum}")]);

        return this;
    }

    /// <summary>Requires the enumerable property value to contain the supplied item.</summary>
    public PropertyValidatorBuilder<T, TProperty> Contains<TItem>(
        TItem expected,
        IEqualityComparer<TItem>? comparer = null,
        string errorCode = ValidationErrorCodes.MissingItem,
        string? errorMessage = null)
    {
        comparer ??= EqualityComparer<TItem>.Default;
        _builder.AddPropertyRule(
            _selector, _propertyName,
            (_, value) => ContainsItem(value, expected, comparer) ? [] : [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} must contain {expected}")]);

        return this;
    }

    /// <summary>Requires the enumerable property value to not contain the supplied item.</summary>
    public PropertyValidatorBuilder<T, TProperty> NotContains<TItem>(
        TItem disallowed,
        IEqualityComparer<TItem>? comparer = null,
        string errorCode = ValidationErrorCodes.DisallowedItem,
        string? errorMessage = null)
    {
        comparer ??= EqualityComparer<TItem>.Default;
        _builder.AddPropertyRule(
            _selector, _propertyName,
            (_, value) => ContainsItem(value, disallowed, comparer) ? [CreateError(value, errorCode, errorMessage ?? $"{_propertyName} cannot contain {disallowed}")] : []);

        return this;
    }

    /// <summary>Adds a custom property-level predicate.</summary>
    public PropertyValidatorBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string errorCode, string errorMessage, IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentHelpers.ThrowIfNull(predicate, nameof(predicate));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        _builder.AddPropertyRule(_selector, _propertyName, (_, value) => predicate(value) ? [] : [CreateError(value, errorCode, errorMessage, metadata)]);
        return this;
    }

    /// <summary>Adds a custom property-level predicate that can inspect the parent instance.</summary>
    public PropertyValidatorBuilder<T, TProperty> Must(
        Func<T, TProperty, bool> predicate,
        string errorCode,
        string errorMessage,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentHelpers.ThrowIfNull(predicate, nameof(predicate));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorCode, nameof(errorCode));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));
        _builder.AddPropertyRule(_selector, _propertyName, (instance, value) => predicate(instance, value) ? [] : [CreateError(value, errorCode, errorMessage, metadata)]);
        return this;
    }

    /// <summary>Applies another validator to the property value and remaps the errors to this property path.</summary>
    public PropertyValidatorBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator)
    {
        ArgumentHelpers.ThrowIfNull(validator, nameof(validator));
        _builder.AddPropertyRule(
            _selector, _propertyName, (_, value) => {
                var result = validator.Validate(value);
                if (result.IsSuccess)
                    return [];

                var errors = new List<Error>();
                foreach (var error in result.Errors ?? []) {
                    var metadata = new Dictionary<string, object>();
                    if (error.Metadata != null) {
                        foreach (var kvp in error.Metadata)
                            metadata[kvp.Key] = kvp.Value;
                    }

                    var nestedPropertyName = metadata.TryGetValue(ValidationMetadataKeys.PropertyName, out var propertyNameValue) ? propertyNameValue as string : null;
                    metadata[ValidationMetadataKeys.PropertyName] = string.IsNullOrWhiteSpace(nestedPropertyName) ? _propertyName : $"{_propertyName}.{nestedPropertyName}";
                    if (!metadata.ContainsKey(ValidationMetadataKeys.AttemptedValue))
                        metadata[ValidationMetadataKeys.AttemptedValue] = value!;

                    errors.Add(new(error.Message, error.Code, error.StackTrace, error.InnerError, metadata, error.Exception, error.Severity) { Timestamp = error.Timestamp });
                }

                return errors;
            });

        return this;
    }

    /// <summary>Adds another model-level validator.</summary>
    public ValidatorBuilder<T> Include(IValidator<T> validator) => _builder.Include(validator);

    /// <summary>Starts another property rule chain.</summary>
    public PropertyValidatorBuilder<T, TNextProperty> RuleFor<TNextProperty>(Expression<Func<T, TNextProperty>> selector, string? propertyName = null)
        => _builder.RuleFor(selector, propertyName);

    /// <summary>Adds a model-level rule.</summary>
    public ValidatorBuilder<T> Must(Func<T, bool> predicate, string errorCode, string errorMessage, IReadOnlyDictionary<string, object>? metadata = null)
        => _builder.Must(predicate, errorCode, errorMessage, metadata);

    /// <summary>Builds the validator instance.</summary>
    public Validator<T> Build() => _builder.Build();

    private Error CreateError(object? attemptedValue, string errorCode, string errorMessage, IReadOnlyDictionary<string, object>? metadata = null)
        => ValidatorBuilder<T>.CreatePropertyError(_propertyName, attemptedValue, errorCode, errorMessage, metadata);

    private static bool IsEmpty(object? value)
    {
        if (value == null)
            return true;

        if (value is string text)
            return string.IsNullOrEmpty(text);

        if (value is ICollection collection)
            return collection.Count == 0;

        if (value is IEnumerable enumerable) {
            var enumerator = enumerable.GetEnumerator();
            try {
                return !enumerator.MoveNext();
            }
            finally {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        return false;
    }

    private static int Compare(TProperty left, TProperty right) => Comparer<TProperty>.Default.Compare(left, right);

    private static bool ContainsItem<TItem>(object? value, TItem expected, IEqualityComparer<TItem> comparer)
    {
        if (value == null)
            return false;

        if (value is IEnumerable<TItem> typedEnumerable) {
            foreach (var item in typedEnumerable) {
                if (comparer.Equals(item, expected))
                    return true;
            }

            return false;
        }

        if (value is not IEnumerable enumerable)
            return false;

        foreach (var item in enumerable) {
            if (item is TItem typedItem && comparer.Equals(typedItem, expected))
                return true;

            if (item == null && expected == null)
                return true;
        }

        return false;
    }
}