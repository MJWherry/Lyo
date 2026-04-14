using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lyo.Exceptions.Models;

namespace Lyo.Exceptions;

/// <summary>Helper methods for argument validation that throw ArgumentException, ArgumentNullException, or ArgumentOutsideRangeException.</summary>
public static class ArgumentHelpers
{
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowArgumentNull(string? paramName) => throw new ArgumentNullException(paramName);

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowArgumentException(string message, string? paramName) => throw new ArgumentException(message, paramName);

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowArgumentOutsideRange(string? paramName, IConvertible? actualValue, IConvertible? minValue, IConvertible? maxValue, string? message)
        => throw new ArgumentOutsideRangeException(paramName, actualValue, minValue, maxValue, message);

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowFileNotFound(string message, string fileName) => throw new FileNotFoundException(message, fileName);

    /// <summary>Throws an ArgumentNullException if the argument is null.</summary>
    /// <param name="argument">The argument to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when argument is null.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? argument, string? paramName = null)
    {
#if NETSTANDARD2_0
        if (argument == null)
            ThrowArgumentNull(paramName ?? nameof(argument));
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
    }

    /// <summary>Throws an ArgumentNullException if the argument is null; otherwise returns the argument. Use for constructor base() chaining.</summary>
    /// <param name="argument">The argument to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The argument when not null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when argument is null.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static T ThrowIfNullReturn<T>([NotNull] T? argument, string? paramName = null)
        where T : class
    {
        ThrowIfNull(argument, paramName);
        return argument!;
    }

    /// <summary>Throws an ArgumentException if the string is null, empty, or consists only of white-space characters.</summary>
    /// <param name="argument">The string to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when argument is null, empty, or whitespace.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, string? paramName = null)
    {
#if NETSTANDARD2_0
        if (argument is null)
            ThrowArgumentNull(paramName ?? nameof(argument));

        if (string.IsNullOrWhiteSpace(argument))
            ThrowArgumentException("Value cannot be Empty or whitespace.", paramName ?? nameof(argument));
#else
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#endif
    }

    /// <summary>Throws an ArgumentException if the string is null or empty (but allows whitespace).</summary>
    /// <param name="argument">The string to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when argument is empty.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty([NotNull] string? argument, string? paramName = null)
    {
        ThrowIfNull(argument, paramName);
        if (argument!.Length == 0)
            ThrowArgumentException("Value cannot be empty.", paramName ?? nameof(argument));
    }

    /// <summary>Throws an ArgumentException if the condition is true.</summary>
    /// <param name="condition">The condition to check. If true, an ArgumentException is thrown.</param>
    /// <param name="message">The error message.</param>
    /// <param name="paramName">The parameter name.</param>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string message, string? paramName = null)
    {
        if (condition)
            ThrowArgumentException(message, paramName);
    }

    /// <summary>Throws an ArgumentException if the value is not within the specified range.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange<T>(T value, T? min = default, T? max = default, string? paramName = null, string? message = null)
        where T : IComparable<T>, IConvertible
    {
        if ((min is { } m && value.CompareTo(m) < 0) || (max is { } x && value.CompareTo(x) > 0))
            ThrowArgumentOutsideRange(paramName, value, min, max, message);
    }

    /// <summary>Throws an ArgumentException if the value is not within the specified range.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentException">Thrown when value is not in the range [min, max].</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange<T>(T? value, T? min = default, T? max = default, string? paramName = null, string? message = null)
        where T : IComparable<T>, IConvertible
    {
        ThrowIfNull(value, paramName);
        ThrowIfNotInRange(value, min, max, paramName, message);
    }

    /// <summary>Throws an ArgumentException if the DateTime value is not within the specified range.</summary>
    /// <param name="value">The DateTime value to check.</param>
    /// <param name="min">The minimum allowed DateTime value (inclusive).</param>
    /// <param name="max">The maximum allowed DateTime value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange(DateTime value, DateTime? min = null, DateTime? max = null, string? paramName = null, string? message = null)
    {
        if ((min.HasValue && value < min.Value) || (max.HasValue && value > max.Value))
            ThrowArgumentOutsideRange(paramName, value, min, max, message);
    }

    /// <summary>Throws an ArgumentException if the nullable DateTime value is null or not within the specified range.</summary>
    /// <param name="value">The nullable DateTime value to check.</param>
    /// <param name="min">The minimum allowed DateTime value (inclusive).</param>
    /// <param name="max">The maximum allowed DateTime value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange(DateTime? value, DateTime? min = null, DateTime? max = null, string? paramName = null, string? message = null)
    {
        ThrowIfNull(value, paramName);
        ThrowIfNotInRange(value.Value, min, max, paramName, message);
    }

    /// <summary>Throws an ArgumentException if the TimeSpan value is not within the specified range.</summary>
    /// <param name="value">The TimeSpan value to check.</param>
    /// <param name="min">The minimum allowed TimeSpan value (inclusive).</param>
    /// <param name="max">The maximum allowed TimeSpan value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange(TimeSpan value, TimeSpan? min = null, TimeSpan? max = null, string? paramName = null, string? message = null)
    {
        if ((min.HasValue && value < min.Value) || (max.HasValue && value > max.Value))
            ThrowArgumentOutsideRange(paramName, value.ToString(), min?.ToString(), max?.ToString(), message);
    }

    /// <summary>Throws an ArgumentException if the nullable TimeSpan value is null or not within the specified range.</summary>
    /// <param name="value">The nullable TimeSpan value to check.</param>
    /// <param name="min">The minimum allowed TimeSpan value (inclusive).</param>
    /// <param name="max">The maximum allowed TimeSpan value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange(TimeSpan? value, TimeSpan? min = default, TimeSpan? max = default, string? paramName = null, string? message = null)
    {
        ThrowIfNull(value, paramName);
        ThrowIfNotInRange(value.Value, min, max, paramName, message);
    }

    /// <summary>Throws NotInRangeException if the value is null or not within the specified range.</summary>
    /// <param name="value">The value to check. If null, throws ArgumentNullException.</param>
    /// <param name="min">The minimum allowed value (inclusive). Default is 0.</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange<T>([NotNull] T? value, T min, T max, string? paramName = null)
        where T : IConvertible, IComparable<T>
    {
        ThrowIfNull(value, paramName);
        ThrowIfNotInRange(value, min, max, paramName);
    }

    /// <summary>Throws NotInRangeException if the array length is null or not within the specified range.</summary>
    /// <param name="array">The array to check. If null, throws ArgumentNullException.</param>
    /// <param name="minLength">The minimum allowed length (inclusive). Default is 0.</param>
    /// <param name="maxLength">The maximum allowed length (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when array is null.</exception>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when array length is not in the range [minLength, maxLength].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange([NotNull] Array? array, long minLength, long maxLength, string? paramName = null)
    {
        ThrowIfNull(array, paramName);
        ThrowIfNotInRange(array.LongLength, minLength, maxLength, paramName, $"Array length ({array.LongLength}) is not in the allowed range [{minLength}, {maxLength}].");
    }

    /// <summary>Throws NotInRangeException if the array length is not within the specified range. Assumes array is not null.</summary>
    /// <param name="array">The array to check. Must not be null.</param>
    /// <param name="minLength">The minimum allowed length (inclusive). Default is 0.</param>
    /// <param name="maxLength">The maximum allowed length (inclusive).</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentOutsideRangeException">Thrown when array length is not in the range [minLength, maxLength].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange(Array array, long minLength, long maxLength, string? paramName = null)
        => ThrowIfNotInRange(array.LongLength, minLength, maxLength, paramName, $"Array length ({array.LongLength}) is not in the allowed range [{minLength}, {maxLength}].");

    /// <summary>Throws an ArgumentException if the collection is null or empty.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when collection is empty.</exception>
    /// <remarks>For single-use enumerables (e.g. LINQ without ToList), prefer passing ICollection or IReadOnlyCollection to avoid consuming the first element.</remarks>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty<T>([NotNull] IEnumerable<T>? collection, string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection is ICollection<T> c) {
            if (c.Count == 0)
                ThrowArgumentException("Collection cannot be empty.", paramName ?? nameof(collection));
        }
        else if (collection is IReadOnlyCollection<T> roc) {
            if (roc.Count == 0)
                ThrowArgumentException("Collection cannot be empty.", paramName ?? nameof(collection));
        }
        else if (!collection.Any())
            ThrowArgumentException("Collection cannot be empty.", paramName ?? nameof(collection));
    }

    /// <summary>Throws an ArgumentException if the collection is null or empty.</summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when collection is empty.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty<TKey, TValue>([NotNull] IDictionary<TKey, TValue>? collection, string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.Count == 0)
            ThrowArgumentException("Dictionary cannot be empty.", paramName ?? nameof(collection));
    }

    /// <summary>Throws an ArgumentException if the value is zero.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is zero.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZero<T>(T value, string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) == 0)
            ThrowArgumentException($"Value cannot be zero.  Actual value: {value}.", paramName);
    }

    /// <summary>Throws an ArgumentException if the value is negative.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is negative.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative<T>(T value, string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) < 0)
            ThrowArgumentException($"Value cannot be negative. Actual value: {value}.", paramName);
    }

    /// <summary>Throws an ArgumentException if the value is negative or zero.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when value is negative or zero.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegativeOrZero<T>(T value, string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) <= 0)
            ThrowArgumentException($"Value must be greater than zero. Actual value: {value}.", paramName);
    }

    /// <summary>Throws a FileNotFoundException if the file does not exist.</summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFileNotFound([NotNull] string? filePath, string? paramName = null)
    {
        UriHelpers.ThrowIfInvalidUri(filePath, paramName, UriKind.Relative);
        if (!File.Exists(filePath!))
            ThrowFileNotFound($"File not found: {filePath}", filePath!);
    }

    /// <summary>Throws a FileNotFoundException if the file does not exist.</summary>
    /// <param name="fileInfo">The FileInfo to check.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when fileInfo is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFileNotFound([NotNull] FileInfo? fileInfo, string? paramName = null)
    {
        ThrowIfNull(fileInfo, paramName ?? nameof(fileInfo));
        if (!fileInfo!.Exists)
            ThrowFileNotFound($"File not found: {fileInfo.FullName}", fileInfo.FullName);
    }
}