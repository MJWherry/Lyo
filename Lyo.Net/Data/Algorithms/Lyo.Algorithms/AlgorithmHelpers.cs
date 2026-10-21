using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Diagnostics;
#endif

namespace Lyo.Algorithms;

/// <summary>Guards that throw <see cref="AlgorithmException" />. Same shape as <see cref="Lyo.Exceptions.OperationHelpers" />.</summary>
public static class AlgorithmHelpers
{
    /// <summary>Throws <see cref="AlgorithmException" />.</summary>
    /// <param name="message">Exception message.</param>
    /// <param name="code">Stable catalog code, when this failure is mapped to a result.</param>
    /// <exception cref="AlgorithmException">Always thrown.</exception>
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static void Throw(string message, string? code = null) => throw new AlgorithmException(message, code);

    /// <summary>Throws <see cref="AlgorithmException" />. The type parameter lets a call sit where a value is required.</summary>
    /// <typeparam name="T">Type of the expression this call replaces.</typeparam>
    /// <param name="message">Exception message.</param>
    /// <param name="code">Stable catalog code, when this failure is mapped to a result.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="AlgorithmException">Always thrown.</exception>
    [DoesNotReturn]
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    public static T Throw<T>(string message, string? code = null) => throw new AlgorithmException(message, code);

    /// <summary>Throws <see cref="AlgorithmException" /> when <paramref name="condition" /> is true.</summary>
    /// <param name="condition">When true, <see cref="AlgorithmException" /> is thrown.</param>
    /// <param name="message">Exception message.</param>
    /// <param name="code">Stable catalog code, when this failure is mapped to a result.</param>
    /// <exception cref="AlgorithmException">Thrown if <paramref name="condition" /> is true.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string message, string? code = null)
    {
        if (condition)
            Throw(message, code);
    }

    /// <summary>Throws <see cref="AlgorithmException" /> when <paramref name="value" /> is null; otherwise returns it.</summary>
    /// <param name="value">Value under test.</param>
    /// <param name="message">Exception message.</param>
    /// <param name="code">Stable catalog code, when this failure is mapped to a result.</param>
    /// <returns><paramref name="value" /> when it is not null.</returns>
    /// <exception cref="AlgorithmException">Thrown if <paramref name="value" /> is null.</exception>
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfNull<T>([NotNull] T? value, string message, string? code = null)
        where T : class
    {
        if (value is null)
            Throw(message, code);

        return value;
    }
}