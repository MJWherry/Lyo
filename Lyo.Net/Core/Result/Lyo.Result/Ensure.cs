using System.Collections;
using System.Runtime.CompilerServices;

namespace Lyo.Result;

/// <summary>Guard/Ensure helpers that return Result instead of throwing. Meant for validation flows.</summary>
public static class Ensure
{
    /// <summary>Ensures the value is not null. Returns Failure when null.</summary>
    public static Result<T> NotNull<T>(T? value, string errorCode = ValidationErrorCodes.NullValue, string? errorMessage = null)
        where T : class
        => ValidationHelpers.ValidateNotNull(value, errorCode, errorMessage);

    /// <summary>Ensures the nullable value is not null. Returns Failure when null.</summary>
    public static Result<T> NotNull<T>(T? value, string errorCode = ValidationErrorCodes.NullValue, string? errorMessage = null)
        where T : struct
        => ValidationHelpers.ValidateNotNull(value, errorCode, errorMessage);

    /// <summary>Ensures the string is not null or empty. Returns Failure when null or empty.</summary>
    public static Result<string> NotEmpty(string? value, string errorCode = ValidationErrorCodes.EmptyString, string? errorMessage = null)
        => ValidationHelpers.ValidateNotEmpty(value, errorCode, errorMessage);

    /// <summary>Ensures the string is not null, empty, or whitespace. Returns Failure when invalid.</summary>
    public static Result<string> NotWhiteSpace(string? value, string errorCode = ValidationErrorCodes.WhitespaceString, string? errorMessage = null)
        => ValidationHelpers.ValidateNotWhitespace(value, errorCode, errorMessage);

    /// <summary>Ensures the collection is not null or empty. Returns Failure when invalid.</summary>
    public static Result<TCollection> NotEmpty<TCollection>(TCollection? collection, string errorCode = ValidationErrorCodes.EmptyCollection, string? errorMessage = null)
        where TCollection : ICollection
        => ValidationHelpers.ValidateNotEmpty(collection, errorCode, errorMessage);

    /// <summary>Ensures the value is within range. Returns Failure when out of range.</summary>
    public static Result<T> InRange<T>(T value, T min, T max, string errorCode = ValidationErrorCodes.OutOfRange, string? errorMessage = null)
        where T : IComparable<T>
        => ValidationHelpers.ValidateRange(value, min, max, errorCode, errorMessage);

    /// <summary>Ensures the condition is true. Returns Failure when the condition is false.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> That<T>(T value, Func<T, bool> condition, string errorCode, string errorMessage)
        => ValidationHelpers.ValidateCondition(value, condition, errorCode, errorMessage);
}