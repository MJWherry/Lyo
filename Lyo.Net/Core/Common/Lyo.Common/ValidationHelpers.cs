using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Lyo.Common.Enums;

namespace Lyo.Common;

/// <summary>Helper methods for validation scenarios.</summary>
public static class ValidationHelpers
{
    /// <summary>Validates a value using multiple validation functions.</summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="validators">Validation functions that return (isValid, errorCode, errorMessage) tuples.</param>
    /// <returns>A Result containing the value if all validations pass, or errors if any fail.</returns>
    public static Result<T> Validate<T>(T value, params Func<T, (bool isValid, string errorCode, string errorMessage)>[] validators)
    {
        if (validators == null || validators.Length == 0)
            return Result<T>.Success(value);

        var errors = new List<Error>();
        foreach (var validator in validators) {
            var (isValid, code, message) = validator(value);
            if (!isValid)
                errors.Add(new(message, code));
        }

        return errors.Count > 0 ? Result<T>.Failure(errors) : Result<T>.Success(value);
    }

    /// <summary>Validates a value using multiple validation functions with custom severity.</summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="validators">Validation functions that return (isValid, errorCode, errorMessage, severity) tuples.</param>
    /// <returns>A Result containing the value if all validations pass, or errors if any fail.</returns>
    public static Result<T> Validate<T>(T value, params Func<T, (bool isValid, string errorCode, string errorMessage, ErrorSeverity severity)>[] validators)
    {
        if (validators == null || validators.Length == 0)
            return Result<T>.Success(value);

        var errors = new List<Error>();
        foreach (var validator in validators) {
            var (isValid, code, message, severity) = validator(value);
            if (!isValid)
                errors.Add(new(message, code, null, null, null, null, severity));
        }

        return errors.Count > 0 ? Result<T>.Failure(errors) : Result<T>.Success(value);
    }

    /// <summary>Validates that a value is not null.</summary>
    /// <typeparam name="T">The type of value to validate (must be a reference type).</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="errorCode">The error code to use if validation fails. Defaults to <see cref="ValidationErrorCodes.NullValue" />.</param>
    /// <param name="errorMessage">The error message to use if validation fails. If null, uses a default message.</param>
    /// <returns>A Result containing the value if not null, or an error if null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> ValidateNotNull<T>(T? value, string errorCode = ValidationErrorCodes.NullValue, string? errorMessage = null)
        where T : class
        => value == null ? Result<T>.Failure(errorMessage ?? "Value cannot be null", errorCode) : Result<T>.Success(value);

    /// <summary>Validates that a nullable value is not null.</summary>
    /// <typeparam name="T">The type of value to validate (must be a value type).</typeparam>
    /// <param name="value">The nullable value to validate.</param>
    /// <param name="errorCode">The error code to use if validation fails. Defaults to <see cref="ValidationErrorCodes.NullValue" />.</param>
    /// <param name="errorMessage">The error message to use if validation fails. If null, uses a default message.</param>
    /// <returns>A Result containing the value if not null, or an error if null.</returns>
    public static Result<T> ValidateNotNull<T>(T? value, string errorCode = ValidationErrorCodes.NullValue, string? errorMessage = null)
        where T : struct
        => !value.HasValue ? Result<T>.Failure(errorMessage ?? "Value cannot be null", errorCode) : Result<T>.Success(value.Value);

    /// <summary>Validates that a string is not null or empty.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="errorCode">The error code to use if validation fails. Defaults to <see cref="ValidationErrorCodes.EmptyString" />.</param>
    /// <param name="errorMessage">The error message to use if validation fails. If null, uses a default message.</param>
    /// <returns>A Result containing the string if not null or empty, or an error otherwise.</returns>
    public static Result<string> ValidateNotEmpty(string? value, string errorCode = ValidationErrorCodes.EmptyString, string? errorMessage = null)
        => string.IsNullOrEmpty(value) ? Result<string>.Failure(errorMessage ?? "String cannot be null or empty", errorCode) : Result<string>.Success(value!);

    /// <summary>Validates that a string is not null, empty, or whitespace.</summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="errorCode">The error code to use if validation fails. Defaults to <see cref="ValidationErrorCodes.WhitespaceString" />.</param>
    /// <param name="errorMessage">The error message to use if validation fails. If null, uses a default message.</param>
    /// <returns>A Result containing the string if not null, empty, or whitespace, or an error otherwise.</returns>
    public static Result<string> ValidateNotWhitespace(string? value, string errorCode = ValidationErrorCodes.WhitespaceString, string? errorMessage = null)
        => string.IsNullOrWhiteSpace(value) ? Result<string>.Failure(errorMessage ?? "String cannot be null, empty, or whitespace", errorCode) : Result<string>.Success(value!);

    /// <summary>Validates that a collection is not null or empty.</summary>
    /// <typeparam name="TCollection">The type of collection to validate (must implement ICollection).</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="errorCode">The error code to use if validation fails. Defaults to <see cref="ValidationErrorCodes.EmptyCollection" />.</param>
    /// <param name="errorMessage">The error message to use if validation fails. If null, uses a default message.</param>
    /// <returns>A Result containing the collection if not null or empty, or an error otherwise.</returns>
    public static Result<TCollection> ValidateNotEmpty<TCollection>(TCollection? collection, string errorCode = ValidationErrorCodes.EmptyCollection, string? errorMessage = null)
        where TCollection : ICollection
        => collection == null || collection.Count == 0
            ? Result<TCollection>.Failure(errorMessage ?? "Collection cannot be null or empty", errorCode)
            : Result<TCollection>.Success(collection);

    /// <summary>Validates that a value is within a range.</summary>
    /// <typeparam name="T">The type of value to validate (must implement IComparable&lt;T&gt;).</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="errorCode">The error code to use if validation fails. Defaults to <see cref="ValidationErrorCodes.OutOfRange" />.</param>
    /// <param name="errorMessage">The error message to use if validation fails. If null, uses a default message.</param>
    /// <returns>A Result containing the value if within range, or an error otherwise.</returns>
    public static Result<T> ValidateRange<T>(T value, T min, T max, string errorCode = ValidationErrorCodes.OutOfRange, string? errorMessage = null)
        where T : IComparable<T>
        => value.CompareTo(min) < 0 || value.CompareTo(max) > 0 ? Result<T>.Failure(errorMessage ?? $"Value must be between {min} and {max}", errorCode) : Result<T>.Success(value);

    /// <summary>Validates that a value meets a custom condition.</summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="condition">The condition function that returns true if the value is valid.</param>
    /// <param name="errorCode">The error code to use if validation fails.</param>
    /// <param name="errorMessage">The error message to use if validation fails.</param>
    /// <returns>A Result containing the value if the condition is met, or an error otherwise.</returns>
    public static Result<T> ValidateCondition<T>(T value, [NotNull] Func<T, bool> condition, string errorCode, string errorMessage)
        => !condition(value) ? Result<T>.Failure(errorMessage, errorCode) : Result<T>.Success(value);

    /// <summary>Validates that a string matches the email format.</summary>
    public static Result<string> ValidateEmail(string? value, string errorCode = ValidationErrorCodes.InvalidEmail, string? errorMessage = null)
        => string.IsNullOrWhiteSpace(value) ? Result<string>.Failure(errorMessage ?? "Email cannot be null or empty", ValidationErrorCodes.EmptyString) :
            RegexPatterns.EmailRegex.IsMatch(value) ? Result<string>.Success(value!) : Result<string>.Failure(errorMessage ?? "Invalid email format", errorCode);

    /// <summary>Validates that a string matches a phone number format.</summary>
    public static Result<string> ValidatePhone(string? value, string errorCode = ValidationErrorCodes.InvalidPhone, string? errorMessage = null)
        => string.IsNullOrWhiteSpace(value) ? Result<string>.Failure(errorMessage ?? "Phone cannot be null or empty", ValidationErrorCodes.EmptyString) :
            RegexPatterns.PhoneNumberRegex.IsMatch(value) ? Result<string>.Success(value!) : Result<string>.Failure(errorMessage ?? "Invalid phone number format", errorCode);

    /// <summary>Validates that a string is a valid URI.</summary>
    public static Result<string> ValidateUri(string? value, UriKind kind = UriKind.Absolute, string errorCode = ValidationErrorCodes.InvalidUri, string? errorMessage = null)
        => string.IsNullOrWhiteSpace(value) ? Result<string>.Failure(errorMessage ?? "URI cannot be null or empty", ValidationErrorCodes.EmptyString) :
            Uri.TryCreate(value, kind, out var _) ? Result<string>.Success(value!) : Result<string>.Failure(errorMessage ?? "Invalid URI format", errorCode);

    /// <summary>Validates that a string length is within the specified range.</summary>
    public static Result<string> ValidateLength(string? value, int minLength, int maxLength, string errorCode = ValidationErrorCodes.InvalidLength, string? errorMessage = null)
    {
        if (value == null)
            return Result<string>.Failure(errorMessage ?? "Value cannot be null", ValidationErrorCodes.NullValue);

        if (value.Length < minLength)
            return Result<string>.Failure(errorMessage ?? $"Value must be at least {minLength} characters", errorCode);

        if (value.Length > maxLength)
            return Result<string>.Failure(errorMessage ?? $"Value must be at most {maxLength} characters", errorCode);

        return Result<string>.Success(value);
    }

    /// <summary>Validates that a string matches the specified regex pattern.</summary>
    public static Result<string> ValidateRegex(string? value, [NotNull] Regex pattern, string errorCode = ValidationErrorCodes.InvalidFormat, string? errorMessage = null)
        => value == null ? Result<string>.Failure(errorMessage ?? "Value cannot be null", ValidationErrorCodes.NullValue) :
            pattern.IsMatch(value) ? Result<string>.Success(value) : Result<string>.Failure(errorMessage ?? "Value does not match required format", errorCode);

    /// <summary>Validates that a string matches the US ZIP code format (12345 or 12345-6789).</summary>
    public static Result<string> ValidateUsZipCode(string? value, string errorCode = ValidationErrorCodes.InvalidZip, string? errorMessage = null)
        => ValidateRegex(value, RegexPatterns.UsZipCodeRegex, errorCode, errorMessage ?? "Invalid US ZIP code format");
}