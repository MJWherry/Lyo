using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.

namespace Lyo.Exceptions;

/// <summary>
/// Helper methods for operation validation that throw <see cref="InvalidOperationException" />, <see cref="ObjectDisposedException" />,
/// <see cref="OperationCanceledException" />, or <see cref="NotSupportedException" />.
/// </summary>
/// <remarks>
/// Unlike <see cref="ArgumentHelpers" />, this type signals invalid runtime state with <see cref="InvalidOperationException" /> (including null references for most checks
/// here). Numeric, range, and equality helpers mirror <see cref="ArgumentHelpers" /> in naming and comparisons; overloads optionally capture the caller&apos;s value expression (via
/// <see cref="CallerArgumentExpressionAttribute" />), surfaced as a suffix on thrown messages rather than exception <c>ParamName</c>. Methods document every exception they throw,
/// including those produced by other overloads in this class that they call.
/// </remarks>
public static class OperationHelpers
{
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowInvalidOperation(string message) => throw new InvalidOperationException(message);

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowObjectDisposed(string? objectName, string? message) => throw new ObjectDisposedException(objectName, message);

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowOperationCancelled(CancellationToken token) => throw new OperationCanceledException(token);

    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowNotSupported(string message) => throw new NotSupportedException(message);

#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [DoesNotReturn]
    private static void ThrowInvalidOperationOutsideRange(string? paramName, IConvertible? actualValue, IConvertible? minValue, IConvertible? maxValue, string? message)
    {
        var body = message ?? $"Value ({actualValue ?? "NULL"}) is not in the allowed range [{minValue ?? "Unspecified"}, {maxValue ?? "Unspecified"}].";
        ThrowInvalidOperation(WithParamHint(paramName, body));
    }

    private static string WithParamHint(string? paramName, string text) => string.IsNullOrEmpty(paramName) ? text : $"{text} ({paramName})";

    /// <summary>Throws an InvalidOperationException if the condition is true.</summary>
    /// <param name="condition">The condition to check. If true, an InvalidOperationException is thrown.</param>
    /// <param name="message">The error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when condition is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf(bool condition, string message)
    {
        if (condition)
            ThrowInvalidOperation(message);
    }

    /// <summary>Throws an InvalidOperationException if the value is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is null.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? value, string? message = null)
    {
        if (value == null)
            ThrowInvalidOperation(message ?? "Operation cannot be performed because a required value is null.");
    }

    /// <summary>Throws an InvalidOperationException if the string is null or whitespace.</summary>
    /// <param name="value">The string to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is null or whitespace.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? value, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            ThrowInvalidOperation(message ?? "Operation cannot be performed because a required string value is null or whitespace.");
    }

    /// <summary>Throws an InvalidOperationException if the string is null or empty.</summary>
    /// <param name="value">The string to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is null or empty.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty([NotNull] string? value, string? message = null)
    {
        if (string.IsNullOrEmpty(value))
            ThrowInvalidOperation(message ?? "Operation cannot be performed because a required string value is null or empty.");
    }

    /// <summary>Throws an InvalidOperationException if the collection is null or empty.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="paramName">The parameter name to include in the error message, when provided.</param>
    /// <remarks>Calls <see cref="ThrowIfNull" /> when the collection reference is null, then checks for an empty sequence.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when collection is null (via <see cref="ThrowIfNull" />) or empty.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty<T>([NotNull] IEnumerable<T>? collection, string? paramName = null)
    {
        ThrowIfNull(collection, paramName != null ? $"Required collection '{paramName}' is null." : null);
        var emptyMessage = paramName != null ? $"Collection '{paramName}' cannot be empty." : "Collection cannot be empty.";
        if (collection is ICollection<T> c) {
            if (c.Count == 0)
                ThrowInvalidOperation(emptyMessage);
        }
        else if (collection is IReadOnlyCollection<T> roc) {
            if (roc.Count == 0)
                ThrowInvalidOperation(emptyMessage);
        }
        else if (!collection.Any())
            ThrowInvalidOperation(emptyMessage);
    }

    /// <summary>Throws an InvalidOperationException if the stream is null or not readable.</summary>
    /// <param name="stream">The stream to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when stream is null or not readable.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotReadable([NotNull] Stream? stream, string? message = null)
    {
        if (stream == null)
            ThrowInvalidOperation(message ?? "Operation cannot be performed because the stream is null.");

        if (!stream.CanRead)
            ThrowInvalidOperation(message ?? "Operation cannot be performed because the stream is not readable.");
    }

    /// <summary>Throws an InvalidOperationException if the stream is null or not writable.</summary>
    /// <param name="stream">The stream to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when stream is null or not writable.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotWritable([NotNull] Stream? stream, string? message = null)
    {
        if (stream == null)
            ThrowInvalidOperation(message ?? "Operation cannot be performed because the stream is null.");

        if (!stream.CanWrite)
            ThrowInvalidOperation(message ?? "Operation cannot be performed because the stream is not writable.");
    }

    /// <summary>Throws an ObjectDisposedException if the disposed flag is true.</summary>
    /// <param name="disposed">The flag indicating whether the object has been disposed.</param>
    /// <param name="objectName">The name of the disposed object. If null, a generic message is used.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="ObjectDisposedException">Thrown when disposed is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDisposed(bool disposed, string? objectName = null, string? message = null)
    {
        if (disposed)
            ThrowObjectDisposed(objectName, message);
    }

    /// <summary>Throws an OperationCanceledException if the cancellation token has been cancelled.</summary>
    /// <param name="ct">The cancellation token to check.</param>
    /// <exception cref="OperationCanceledException">Thrown when the token has been cancelled.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCancelled(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            ThrowOperationCancelled(ct);
    }

    /// <summary>Throws a NotSupportedException if the condition is true.</summary>
    /// <param name="condition">The condition to check. If true, a NotSupportedException is thrown.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="NotSupportedException">Thrown when condition is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotSupported(bool condition, string? message = null)
    {
        if (condition)
            ThrowNotSupported(message ?? "The operation is not supported.");
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is not within the specified range.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum allowed value (inclusive). Omit or pass null to skip the minimum check.</param>
    /// <param name="max">The maximum allowed value (inclusive). Omit or pass null to skip the maximum check.</param>
    /// <param name="paramName">Omitted: caller expression for <paramref name="value" />. Included in the exception message when provided.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange<T>(T value, T? min = null, T? max = null, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : struct, IComparable<T>, IConvertible
    {
        if ((min.HasValue && value.CompareTo(min.Value) < 0) || (max.HasValue && value.CompareTo(max.Value) > 0))
            ThrowInvalidOperationOutsideRange(paramName, value, min, max, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the reference-type value is not within the specified range.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum allowed value (inclusive). Omit or pass null to skip the minimum check.</param>
    /// <param name="max">The maximum allowed value (inclusive). Omit or pass null to skip the maximum check.</param>
    /// <param name="paramName">Omitted: caller expression for <paramref name="value" />. Included in the exception message when provided.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is not in the range [min, max].</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange<T>(T value, T? min = null, T? max = null, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : class, IComparable<T>, IConvertible
    {
        if ((min is { } m && value.CompareTo(m) < 0) || (max is { } x && value.CompareTo(x) > 0))
            ThrowInvalidOperationOutsideRange(paramName, value, min, max, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the nullable value is null or not within the specified range.</summary>
    /// <remarks>Delegates to <see cref="ThrowIfNull(System.Object,string?)" />; if not null, applies <see cref="ThrowIfNotInRange{T}(T,T?,T?,string?,string?)" />.</remarks>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange<T>(
        [NotNull] T? value,
        T? min = null,
        T? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
        where T : struct, IComparable<T>, IConvertible
    {
        ThrowIfNull(value);
        ThrowIfNotInRange(value.Value, min, max, paramName, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is not within the specified range. Overload for non-nullable values, where the null check is vacuous.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange<T>(
        T value,
        T? min = null,
        T? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
        where T : struct, IComparable<T>, IConvertible
        => ThrowIfNotInRange(value, min, max, paramName, message);

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is null or not within the specified range.</summary>
    /// <remarks>Delegates to <see cref="ThrowIfNull(System.Object,string?)" />; if not null, applies <see cref="ThrowIfNotInRange{T}(T,T?,T?,string?,string?)" />.</remarks>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange<T>(
        [NotNull] T? value,
        T? min = null,
        T? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
        where T : class, IComparable<T>, IConvertible
    {
        ThrowIfNull(value);
        ThrowIfNotInRange(value, min, max, paramName, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the <see cref="DateTime" /> value is not within the specified range.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange(
        DateTime value,
        DateTime? min = null,
        DateTime? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
    {
        if ((min.HasValue && value < min.Value) || (max.HasValue && value > max.Value))
            ThrowInvalidOperationOutsideRange(paramName, value, min, max, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the nullable <see cref="DateTime" /> value is null or not within the specified range.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange(
        DateTime? value,
        DateTime? min = null,
        DateTime? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
    {
        if (!value.HasValue)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? "Operation cannot be performed because a required DateTime value is missing."));

        ThrowIfNotInRange(value.Value, min, max, paramName, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the <see cref="TimeSpan" /> value is not within the specified range.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange(
        TimeSpan value,
        TimeSpan? min = null,
        TimeSpan? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
    {
        if ((min.HasValue && value < min.Value) || (max.HasValue && value > max.Value))
            ThrowInvalidOperationOutsideRange(paramName, value.ToString(), min?.ToString(), max?.ToString(), message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the nullable <see cref="TimeSpan" /> value is null or not within the specified range.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange(
        TimeSpan? value,
        TimeSpan? min = null,
        TimeSpan? max = null,
        [CallerArgumentExpression("value")] string? paramName = null,
        string? message = null)
    {
        if (!value.HasValue)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? "Operation cannot be performed because a required TimeSpan value is missing."));

        ThrowIfNotInRange(value.Value, min, max, paramName, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the array length is null or not within the specified range.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrNotInRange([NotNull] Array? array, long minLength, long maxLength, [CallerArgumentExpression("array")] string? paramName = null)
    {
        ThrowIfNull(array);
        ThrowIfNotInRange(array.LongLength, minLength, maxLength, paramName, $"Array length ({array.LongLength}) is not in the allowed range [{minLength}, {maxLength}].");
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the array length is not within the specified range.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotInRange(Array array, long minLength, long maxLength, [CallerArgumentExpression("array")] string? paramName = null)
        => ThrowIfNotInRange(array.LongLength, minLength, maxLength, paramName, $"Array length ({array.LongLength}) is not in the allowed range [{minLength}, {maxLength}].");

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is zero.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZero<T>(T value, string? message = null, [CallerArgumentExpression("value")] string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) == 0)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? $"Value cannot be zero.  Actual value: {value}."));
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if <paramref name="value" /> is null or negative; uses the same comparisons and default messages as
    /// <see cref="ArgumentHelpers.ThrowIfNegative{T}(T,string)" />.
    /// </summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative<T>(T? value, string? message = null, [CallerArgumentExpression("value")] string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value is null)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? "Operation cannot be performed because a required value is null."));

        if (value.CompareTo(default(T)!) < 0)
            ThrowInvalidOperationOutsideRange(paramName, value, null, null, message ?? $"Value cannot be negative. Actual value: {value}.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if <paramref name="value" /> is null, negative, or zero; parallel to
    /// <see cref="ArgumentHelpers.ThrowIfNegativeOrZero{T}(T,string)" />.
    /// </summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegativeOrZero<T>(T? value, string? message = null, [CallerArgumentExpression("value")] string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value is null)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? "Operation cannot be performed because a required value is null."));

        if (value.CompareTo(default(T)!) <= 0)
            ThrowInvalidOperationOutsideRange(paramName, value, 0, null, message ?? $"Value must be greater than zero. Actual value: {value}.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if <paramref name="value" /> is null or strictly positive; parallel to
    /// <see cref="ArgumentHelpers.ThrowIfPositive{T}(T,string)" />.
    /// </summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfPositive<T>(T? value, string? message = null, [CallerArgumentExpression("value")] string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value is null)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? "Operation cannot be performed because a required value is null."));

        if (value.CompareTo(default(T)!) > 0)
            ThrowInvalidOperationOutsideRange(paramName, value, null, 0, message ?? $"Value cannot be positive. Actual value: {value}.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if <paramref name="value" /> is null or not strictly negative; parallel to
    /// <see cref="ArgumentHelpers.ThrowIfPositiveOrZero{T}(T,string)" />.
    /// </summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfPositiveOrZero<T>(T? value, string? message = null, [CallerArgumentExpression("value")] string? paramName = null)
        where T : IComparable, IConvertible
    {
        if (value is null)
            ThrowInvalidOperation(WithParamHint(paramName, message ?? "Operation cannot be performed because a required value is null."));

        if (value.CompareTo(default(T)!) >= 0)
            ThrowInvalidOperationOutsideRange(paramName, value, null, 0, message ?? $"Value must be less than zero. Actual value: {value}.");
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is greater than the specified threshold.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThan<T>(T value, T threshold, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : IComparable<T>, IConvertible
    {
        if (value.CompareTo(threshold) > 0)
            ThrowInvalidOperationOutsideRange(paramName, value, null, threshold, message ?? $"Value must be less than or equal to {threshold}. Actual value: {value}.");
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is greater than or equal to the specified threshold.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThanOrEqual<T>(T value, T threshold, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : IComparable<T>, IConvertible
    {
        if (value.CompareTo(threshold) >= 0)
            ThrowInvalidOperationOutsideRange(paramName, value, null, threshold, message ?? $"Value must be strictly less than {threshold}. Actual value: {value}.");
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is less than the specified threshold.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThan<T>(T value, T threshold, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : IComparable<T>, IConvertible
    {
        if (value.CompareTo(threshold) < 0)
            ThrowInvalidOperationOutsideRange(paramName, value, threshold, null, message ?? $"Value must be greater than or equal to {threshold}. Actual value: {value}.");
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value is less than or equal to the specified threshold.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThanOrEqual<T>(T value, T threshold, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : IComparable<T>, IConvertible
    {
        if (value.CompareTo(threshold) <= 0)
            ThrowInvalidOperationOutsideRange(paramName, value, threshold, null, message ?? $"Value must be strictly greater than {threshold}. Actual value: {value}.");
    }

    /// <summary>When <paramref name="value" /> has a value, throws if it is less than or equal to <paramref name="threshold" />.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotNullAndLessThanOrEqual(double? value, double threshold, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
    {
        if (!value.HasValue)
            return;

        ThrowIfLessThanOrEqual(value.GetValueOrDefault(), threshold, paramName, message);
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value equals the specified other value.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEqual<T>(T value, T other, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : IEquatable<T>
    {
        if (value.Equals(other))
            ThrowInvalidOperation(WithParamHint(paramName, message ?? $"Value must not equal {other}. Actual value: {value}."));
    }

    /// <summary>Throws an <see cref="InvalidOperationException" /> if the value does not equal the specified other value.</summary>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual<T>(T value, T expected, [CallerArgumentExpression("value")] string? paramName = null, string? message = null)
        where T : IEquatable<T>
    {
        if (!value.Equals(expected))
            ThrowInvalidOperation(WithParamHint(paramName, message ?? $"Value must equal {expected}. Actual value: {value}."));
    }
}