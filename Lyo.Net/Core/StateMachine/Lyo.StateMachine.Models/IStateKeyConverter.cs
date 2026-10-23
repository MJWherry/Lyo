namespace Lyo.StateMachine.Models;

/// <summary>Converts a state (or trigger) to and from the string used in stores and <see cref="StateMachineDocument" />s.</summary>
/// <remarks>Keys must stay stable: renaming an enum member (or changing a custom mapping) orphans rows and documents that use the old key.</remarks>
public interface IStateKeyConverter<TValue>
    where TValue : notnull
{
    /// <summary>String written for <paramref name="value" />.</summary>
    string ToKey(TValue value);

    /// <summary>Value for a stored <paramref name="key" />.</summary>
    /// <exception cref="ArgumentException">The key does not map to a value.</exception>
    TValue FromKey(string key);
}

/// <summary>Stores enum values by member name.</summary>
public sealed class EnumStateKeyConverter<TEnum> : IStateKeyConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Shared instance.</summary>
    public static EnumStateKeyConverter<TEnum> Instance { get; } = new();

    /// <inheritdoc />
    public string ToKey(TEnum value) => value.ToString();

    /// <inheritdoc />
    public TEnum FromKey(string key)
    {
        if (Enum.TryParse<TEnum>(key, false, out var value) && Enum.IsDefined(typeof(TEnum), value))
            return value;

        throw new ArgumentException($"'{key}' is not a member of {typeof(TEnum).Name}.", nameof(key));
    }
}

/// <summary>Identity converter for machines whose states or triggers are plain strings (fully data-driven machines).</summary>
public sealed class StringStateKeyConverter : IStateKeyConverter<string>
{
    /// <summary>Shared instance.</summary>
    public static StringStateKeyConverter Instance { get; } = new();

    /// <inheritdoc />
    public string ToKey(string value) => value;

    /// <inheritdoc />
    public string FromKey(string key) => key;
}