using System.Runtime.CompilerServices;

namespace Lyo.Exceptions;

/// <summary>
/// Subject plus a result that may be a different type. A chosen result, including a null reference, stays chosen. An open chain resolves through
/// <see cref="OrDefault(TResult)" /> or <see cref="OrThrow(System.Func{System.Exception})" />.
/// </summary>
/// <typeparam name="TSubject">Value the checks ran against.</typeparam>
/// <typeparam name="TResult">Value returned when a check passes.</typeparam>
public readonly struct ValueOrChain<TSubject, TResult>
{
    private const string DefaultNotChosen = "A required value was not chosen.";

    private readonly TResult _result;

    private ValueOrChain(TSubject subject, bool chosen, TResult result)
    {
        Subject = subject;
        IsChosen = chosen;
        _result = result;
    }

    internal TSubject Subject { get; }

    internal bool IsChosen { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueOrChain<TSubject, TResult> Chosen(TSubject subject, TResult result) => new(subject, true, result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueOrChain<TSubject, TResult> Miss(TSubject subject) => new(subject, false, default!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueOrChain<TSubject, TResult> From(TSubject subject, bool selected, TResult result) => selected ? Chosen(subject, result) : Miss(subject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueOrChain<TSubject, TResult> From(TSubject subject, bool selected, Func<TResult> result)
    {
        if (!selected)
            return Miss(subject);

        ArgumentHelpers.ThrowIfNull(result);
        return Chosen(subject, result());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueOrChain<TSubject, TResult> Choose(bool selected, TResult result) => IsChosen || !selected ? this : Chosen(Subject, result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueOrChain<TSubject, TResult> Choose(bool selected, Func<TResult> result)
    {
        if (IsChosen || !selected)
            return this;

        ArgumentHelpers.ThrowIfNull(result);
        return Chosen(Subject, result());
    }

    /// <summary>Returns the chosen result, or <paramref name="defaultValue" /> when the chain is still open.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult OrDefault(TResult defaultValue) => IsChosen ? _result : defaultValue;

    /// <summary>Returns the chosen result. Invokes <paramref name="defaultValue" /> only when the chain is still open.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult OrDefault(Func<TResult> defaultValue)
    {
        if (IsChosen)
            return _result;

        ArgumentHelpers.ThrowIfNull(defaultValue);
        return defaultValue();
    }

    /// <summary>Returns the chosen result, or throws the exception from <paramref name="createException" /> when the chain is still open.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult OrThrow(Func<Exception> createException)
    {
        if (IsChosen)
            return _result;

        ArgumentHelpers.ThrowIfNull(createException);
        throw createException();
    }

    /// <summary>Returns the chosen result, or throws <see cref="InvalidOperationException" /> when the chain is still open.</summary>
    /// <param name="message">Exception message when nothing was chosen. Omit or pass null for a built-in default.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult OrThrowInvalidOperation(string? message = null)
    {
        if (IsChosen)
            return _result;

        throw new InvalidOperationException(message ?? DefaultNotChosen);
    }
}

internal static class ValueWhenChecks
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInverted<T>(T min, T max)
        where T : IComparable<T>
        => ArgumentHelpers.ThrowIf(min.CompareTo(max) > 0, "Minimum must be less than or equal to maximum.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool InRange<T>(T value, T min, T max)
        where T : IComparable<T>
    {
        ThrowIfInverted(min, max);
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GreaterThan<T>(T value, T threshold)
        where T : IComparable<T>
        => value.CompareTo(threshold) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GreaterThanOrEqual<T>(T value, T threshold)
        where T : IComparable<T>
        => value.CompareTo(threshold) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool LessThan<T>(T value, T threshold)
        where T : IComparable<T>
        => value.CompareTo(threshold) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool LessThanOrEqual<T>(T value, T threshold)
        where T : IComparable<T>
        => value.CompareTo(threshold) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsZero<T>(T value)
        where T : struct, IComparable<T>
        => value.CompareTo(default) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNegative<T>(T value)
        where T : struct, IComparable<T>
        => value.CompareTo(default) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsNegativeOrZero<T>(T value)
        where T : struct, IComparable<T>
        => value.CompareTo(default) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPositive<T>(T value)
        where T : struct, IComparable<T>
        => value.CompareTo(default) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPositiveOrZero<T>(T value)
        where T : struct, IComparable<T>
        => value.CompareTo(default) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDefined<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
#if NETSTANDARD2_0
        return Enum.IsDefined(typeof(TEnum), value);
#else
        return Enum.IsDefined(value);
#endif
    }
}