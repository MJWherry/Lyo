using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using Lyo.Result;

namespace Lyo.Validation.Attributes;

/// <summary>Base attribute for property-level validation rules.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public abstract class ValidationAttributeBase(string errorCode) : Attribute
{
    /// <summary>Gets the error code returned when validation fails.</summary>
    public string ErrorCode { get; } = errorCode;

    /// <summary>Gets or sets the optional custom error message.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Validates the supplied property value.</summary>
    public abstract IReadOnlyList<Error> Validate(string propertyName, object? value, object instance);

    protected Error CreateError(string propertyName, object? attemptedValue, string defaultMessage)
    {
        var metadata = new Dictionary<string, object> { [ValidationMetadataKeys.PropertyName] = propertyName, [ValidationMetadataKeys.AttemptedValue] = attemptedValue! };
        return new(ErrorMessage ?? defaultMessage, ErrorCode, metadata: metadata);
    }

    protected static bool IsEmpty(object? value)
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
}

/// <summary>Requires the property value to be non-null.</summary>
public sealed class RequiredAttribute()
    : ValidationAttributeBase(ValidationErrorCodes.NullValue)
{
    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
        => value == null ? [CreateError(propertyName, value, $"{propertyName} cannot be null")] : [];
}

/// <summary>Requires the property value to not be empty.</summary>
public sealed class NotEmptyAttribute()
    : ValidationAttributeBase(ValidationErrorCodes.EmptyValue)
{
    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
        => IsEmpty(value) ? [CreateError(propertyName, value, $"{propertyName} cannot be empty")] : [];
}

/// <summary>Requires the string property value to not be null, empty, or whitespace.</summary>
public sealed class NotWhiteSpaceAttribute()
    : ValidationAttributeBase(ValidationErrorCodes.WhitespaceString)
{
    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
        => string.IsNullOrWhiteSpace(value as string) ? [CreateError(propertyName, value, $"{propertyName} cannot be null, empty, or whitespace")] : [];
}

/// <summary>Requires the string property value length to be within the supplied inclusive range.</summary>
public sealed class LengthAttribute(int minLength, int maxLength)
    : ValidationAttributeBase(ValidationErrorCodes.InvalidLength)
{
    public int MinLength { get; } = minLength;

    public int MaxLength { get; } = maxLength;

    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
        => value is not string text || text.Length < MinLength || text.Length > MaxLength
            ? [CreateError(propertyName, value, $"{propertyName} length must be between {MinLength} and {MaxLength}")]
            : [];
}

/// <summary>Requires the string property value to match the supplied regex.</summary>
public sealed class RegexAttribute(string pattern)
    : ValidationAttributeBase(ValidationErrorCodes.InvalidFormat)
{
    private readonly Regex _regex = new(pattern, RegexOptions.Compiled);

    public string Pattern { get; } = pattern;

    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
        => value is not string text || !_regex.IsMatch(text) ? [CreateError(propertyName, value, $"{propertyName} is not in the expected format")] : [];
}

/// <summary>Requires the string property value to be a valid email address.</summary>
public sealed class EmailAttribute()
    : ValidationAttributeBase(ValidationErrorCodes.InvalidEmail)
{
    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
    {
        var text = value as string;
        return string.IsNullOrWhiteSpace(text) || !RegexPatterns.EmailRegex.IsMatch(text)
            ? [CreateError(propertyName, value, $"{propertyName} must be a valid email address")]
            : [];
    }
}

/// <summary>Requires the string property value to be a valid phone number.</summary>
public sealed class PhoneAttribute()
    : ValidationAttributeBase(ValidationErrorCodes.InvalidPhone)
{
    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
    {
        var text = value as string;
        return string.IsNullOrWhiteSpace(text) || !RegexPatterns.PhoneNumberRegex.IsMatch(text)
            ? [CreateError(propertyName, value, $"{propertyName} must be a valid phone number")]
            : [];
    }
}

/// <summary>Requires the string property value to be a valid URI.</summary>
public sealed class UriAttribute(UriKind kind = UriKind.Absolute)
    : ValidationAttributeBase(ValidationErrorCodes.InvalidUri)
{
    public UriKind Kind { get; } = kind;

    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
    {
        var text = value as string;
        return string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text, Kind, out var _) ? [CreateError(propertyName, value, $"{propertyName} must be a valid URI")] : [];
    }
}

/// <summary>Requires the property value to be within the supplied inclusive numeric range.</summary>
public sealed class RangeAttribute(double minimum, double maximum)
    : ValidationAttributeBase(ValidationErrorCodes.OutOfRange)
{
    public double Minimum { get; } = minimum;

    public double Maximum { get; } = maximum;

    public override IReadOnlyList<Error> Validate(string propertyName, object? value, object instance)
    {
        if (value == null)
            return [CreateError(propertyName, value, $"{propertyName} must be between {Minimum} and {Maximum}")];

        try {
            var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return number >= Minimum && number <= Maximum ? [] : [CreateError(propertyName, value, $"{propertyName} must be between {Minimum} and {Maximum}")];
        }
        catch {
            return [CreateError(propertyName, value, $"{propertyName} must be a numeric value between {Minimum} and {Maximum}")];
        }
    }
}