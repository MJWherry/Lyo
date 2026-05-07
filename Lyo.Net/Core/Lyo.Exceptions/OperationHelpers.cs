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
/// here). Methods document every exception they throw, including those produced by other overloads in this class that they call.
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

    /// <summary>Throws an InvalidOperationException if the value is zero.</summary>
    /// <typeparam name="T">A comparable, convertible numeric type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is zero.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZero<T>(T value, string? message = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) == 0)
            ThrowInvalidOperation(message ?? $"Operation cannot be performed because the value is zero. Actual value: {value}.");
    }

    /// <summary>Throws an InvalidOperationException if the value is negative.</summary>
    /// <typeparam name="T">A comparable, convertible numeric type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is negative.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative<T>(T value, string? message = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) < 0)
            ThrowInvalidOperation(message ?? $"Operation cannot be performed because the value is negative. Actual value: {value}.");
    }

    /// <summary>Throws an InvalidOperationException if the value is negative or zero.</summary>
    /// <typeparam name="T">A comparable, convertible numeric type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is negative or zero.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegativeOrZero<T>(T value, string? message = null)
        where T : IComparable, IConvertible
    {
        if (value.CompareTo(default(T)!) <= 0)
            ThrowInvalidOperation(message ?? $"Operation cannot be performed because the value must be greater than zero. Actual value: {value}.");
    }

    /// <summary>Throws an InvalidOperationException if the value is greater than the specified threshold.</summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="threshold">The inclusive upper bound; the value must be ≤ threshold.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is greater than threshold.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThan<T>(T value, T threshold, string? message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(threshold) > 0)
            ThrowInvalidOperation(message ?? $"Operation cannot be performed because the value must be less than or equal to {threshold}. Actual value: {value}.");
    }

    /// <summary>Throws an InvalidOperationException if the value is less than the specified threshold.</summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="threshold">The inclusive lower bound; the value must be ≥ threshold.</param>
    /// <param name="message">The error message. If null, a default message is used.</param>
    /// <exception cref="InvalidOperationException">Thrown when value is less than threshold.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThan<T>(T value, T threshold, string? message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(threshold) < 0)
            ThrowInvalidOperation(message ?? $"Operation cannot be performed because the value must be greater than or equal to {threshold}. Actual value: {value}.");
    }
}