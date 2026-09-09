using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lyo.Result;

/// <summary>Optional value that may or may not be present.</summary>
/// <typeparam name="T">Type of the value when present.</typeparam>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Option<T> : IEquatable<Option<T>>
{
    private readonly T? _value;

    private Option(T? value, bool hasValue)
    {
        _value = value;
        HasValue = hasValue;
    }

    /// <summary>True when the option has a value.</summary>
    public bool HasValue { get; }

    /// <summary>Value if present, otherwise default.</summary>
    public T? ValueOrDefault => HasValue ? _value : default;

    /// <summary>Builds an option with a value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Some(T value) => new(value, true);

    /// <summary>Builds an option with no value.</summary>
    public static Option<T> None() => new(default!, false);

    /// <summary>Builds Some(value) when value is not null, otherwise None.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> FromNullable(T? value) => value != null ? Some(value) : None();

    /// <summary>Pattern matching — returns a value based on whether the option has a value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) => HasValue ? onSome(_value!) : onNone();

    /// <summary>Runs an action when the option has a value.</summary>
    public Option<T> IfSome(Action<T> action)
    {
        if (HasValue)
            action(_value!);

        return this;
    }

    /// <summary>Runs an action when the option has no value.</summary>
    public Option<T> IfNone(Action action)
    {
        if (!HasValue)
            action();

        return this;
    }

    /// <summary>Maps the value to another type when present.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TOut> Map<TOut>(Func<T, TOut> mapper) => HasValue ? Option<TOut>.Some(mapper(_value!)) : Option<TOut>.None();

    /// <summary>Binds the value to another option when present.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TOut> Bind<TOut>(Func<T, Option<TOut>> binder) => HasValue ? binder(_value!) : Option<TOut>.None();

    /// <summary>Returns the value, or the default when none.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) => HasValue ? _value! : defaultValue;

    /// <summary>Returns the value, or the factory result when none.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(Func<T> defaultValueFactory) => HasValue ? _value! : defaultValueFactory();

    /// <summary>Converts to Result — Some becomes Success, None becomes Failure.</summary>
    public Result<T> ToResult(string errorCode = "NONE", string errorMessage = "No value present")
        => HasValue ? Result<T>.Success(_value!) : Result<T>.Failure(errorMessage, errorCode);

    /// <summary>Converts to a nullable.</summary>
    public T? ToNullable() => HasValue ? _value : default;

    /// <summary>Converts from a nullable.</summary>
    public static Option<T> From(T? value) => FromNullable(value);

    public bool Equals(Option<T> other) => HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(_value!, other._value!);

    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    public override int GetHashCode() => HasValue && _value != null ? _value.GetHashCode() : 0;

    public override string ToString() => HasValue ? $"Some({_value})" : "None";

    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    public static implicit operator Option<T>(T? value) => FromNullable(value);
}