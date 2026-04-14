using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.

namespace Lyo.Exceptions;

/// <summary>Helper methods for operation validation that throw InvalidOperationException.</summary>
public static class OperationHelpers
{
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static void ThrowInvalidOperation(string message) => throw new InvalidOperationException(message);

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
    /// <exception cref="InvalidOperationException">Thrown when collection is null or empty.</exception>
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
}