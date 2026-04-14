using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lyo.Common;

/// <summary>Represents an optional value that may or may not be present.</summary>
/// <typeparam name="T">The type of the value when present.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Option<T> : IEquatable<Option<T>>
{
    private readonly T? _value;

    private Option(T? value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    /// <summary>Gets a value indicating whether the option has a value.</summary>
    public bool HasValue { get; }

    /// <summary>Gets the value if present, otherwise default.</summary>
    public T? ValueOrDefault => HasValue ? _value : default;

    /// <summary>Creates an option with a value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Some(T value) => new(value, true);

    /// <summary>Creates an option with no value.</summary>
    public static Option<T> None() => new(default!, false);

    /// <summary>Creates Some(value) if value is not null, otherwise None.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> FromNullable(T? value) => value != null ? Some(value) : None();

    /// <summary>Pattern matching - returns a value based on whether the option has a value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Match<TResult>([NotNull] Func<T, TResult> onSome, [NotNull] Func<TResult> onNone) => HasValue ? onSome(_value!) : onNone();

    /// <summary>Executes an action if the option has a value.</summary>
    public Option<T> IfSome([NotNull] Action<T> action)
    {
        if (HasValue)
            action(_value!);

        return this;
    }

    /// <summary>Executes an action if the option has no value.</summary>
    public Option<T> IfNone([NotNull] Action action)
    {
        if (!HasValue)
            action();

        return this;
    }

    /// <summary>Maps the value to another type if present.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TOut> Map<TOut>([NotNull] Func<T, TOut> mapper) => HasValue ? Option<TOut>.Some(mapper(_value!)) : Option<TOut>.None();

    /// <summary>Binds the value to another option if present.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TOut> Bind<TOut>([NotNull] Func<T, Option<TOut>> binder) => HasValue ? binder(_value!) : Option<TOut>.None();

    /// <summary>Gets the value or returns the default value if none.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) => HasValue ? _value! : defaultValue;

    /// <summary>Gets the value or returns the result of the factory if none.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault([NotNull] Func<T> defaultValueFactory) => HasValue ? _value! : defaultValueFactory();

    /// <summary>Converts to Result - Some becomes Success, None becomes Failure.</summary>
    public Result<T> ToResult(string errorCode = "NONE", string errorMessage = "No value present")
        => HasValue ? Result<T>.Success(_value!) : Result<T>.Failure(errorMessage, errorCode);

    /// <summary>Converts to nullable.</summary>
    public T? ToNullable() => HasValue ? _value : default;

    /// <summary>Converts from nullable.</summary>
    public static Option<T> From(T? value) => FromNullable(value);

    public bool Equals(Option<T> other) => HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(_value!, other._value!);

    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    public override int GetHashCode() => HasValue && _value != null ? _value.GetHashCode() : 0;

    public override string ToString() => HasValue ? $"Some({_value})" : "None";

    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    public static implicit operator Option<T>(T? value) => FromNullable(value);
}