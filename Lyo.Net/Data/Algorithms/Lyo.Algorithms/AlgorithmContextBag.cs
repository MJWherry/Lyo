using System.Collections;
using System.Globalization;
using Lyo.Common.Core;
using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>
/// Case-insensitive name/value bag for expressions that are not bound to a DTO. Catalog formulas execute against this. Names are expression identifiers (<c>Atk</c>), not
/// dotted paths.
/// </summary>
public sealed class AlgorithmContextBag : IReadOnlyDictionary<string, object?>
{
    private readonly ContextBag _values = new();

    /// <summary>Number of entries.</summary>
    public int Count => _values.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys => _values.Keys;

    /// <inheritdoc />
    public IEnumerable<object?> Values => _values.Values;

    /// <summary>Value for <paramref name="key" />, or null when missing. A pending factory runs.</summary>
    public object? this[string key] => _values.TryGetValue(key, out var value) ? value : null;

    /// <inheritdoc />
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Sets or replaces <paramref name="key" />. Names must be expression identifiers (<c>Atk</c>), not dotted paths. A null literal is ambiguous with a factory overload; cast
    /// it to <see cref="object" />.
    /// </summary>
    public void Set(string key, object? value)
    {
        RequireIdentifier(key);
        _values.Set(key, value);
    }

    /// <summary>Stores <paramref name="factory" /> under an identifier without running it. The first read, including <see cref="ReadDouble" />, replaces it with the result.</summary>
    public void Set(string key, Func<object?> factory)
    {
        RequireIdentifier(key);
        ArgumentHelpers.ThrowIfNull(factory);
        _values.Set(key, factory);
    }

    /// <summary>Stores <paramref name="factory" /> under an identifier. The factory receives the shared bag and can read sibling identifiers when a formula materializes this key.</summary>
    public void Set(string key, Func<ContextBag, object?> factory)
    {
        RequireIdentifier(key);
        ArgumentHelpers.ThrowIfNull(factory);
        Func<ContextBag<string>, object?> adapted = bag => factory((ContextBag)bag);
        _values.Set(key, adapted);
    }

    /// <summary>Reads <paramref name="key" /> and converts it. See <see cref="ContextBag{TKey}.TryGetAs{T}" />.</summary>
    public bool TryGetAs<T>(string key, out T? value) => _values.TryGetAs(key, out value);

    /// <summary>Reads <paramref name="name" /> as <typeparamref name="T" />. The stored value must already be <typeparamref name="T" />. Compiled formulas call this.</summary>
    public static T Read<T>(AlgorithmContextBag bag, string name)
    {
        ArgumentHelpers.ThrowIfNull(bag);
        if (!bag.TryGetValue(name, out var value) || value is not T)
            return AlgorithmHelpers.Throw<T>($"Expression identifier '{name}' is missing or is not {typeof(T).Name}.");

        return (T)value;
    }

    /// <summary>Reads <paramref name="name" /> as a double. Compiled catalog formulas call this. A pending factory runs once.</summary>
    public static double ReadDouble(AlgorithmContextBag bag, string name)
    {
        ArgumentHelpers.ThrowIfNull(bag);
        AlgorithmHelpers.ThrowIf(!bag.TryGetValue(name, out var value) || value is null, $"Expression identifier '{name}' is missing.");
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Reads <paramref name="name" /> as text. Compiled formulas call this for identifiers that end in <c>_key</c>.</summary>
    public static string ReadString(AlgorithmContextBag bag, string name)
    {
        ArgumentHelpers.ThrowIfNull(bag);
        AlgorithmHelpers.ThrowIf(!bag.TryGetValue(name, out var value) || value is not string, $"Expression identifier '{name}' is missing.");
        return (string)value!;
    }

    /// <summary>True when <paramref name="name" /> can be an expression identifier. Dotted paths are not identifiers.</summary>
    public static bool IsIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
            return false;

        for (var i = 1; i < name.Length; i++) {
            if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_'))
                return false;
        }

        return true;
    }

    private static void RequireIdentifier(string key)
    {
        if (!IsIdentifier(key))
            throw new ArgumentException($"'{key}' is not an expression identifier.", nameof(key));
    }
}