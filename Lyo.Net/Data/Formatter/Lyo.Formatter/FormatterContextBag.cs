using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Reflection;

namespace Lyo.Formatter;

/// <summary>
/// Case-insensitive context for SmartFormat and expressions. Resolves nested members and flat dotted keys
/// (<c>Docket.Number</c> on a dictionary) from one or more DTOs or dictionaries.
/// </summary>
internal sealed class FormatterContextBag : DynamicObject, IReadOnlyDictionary<string, object?>
{
    private static readonly BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
    private readonly Dictionary<string, Type> _types = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    public FormatterContextBag() { }

    public FormatterContextBag(object? context) => AddContext(context);

    public int Count => _values.Count;

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<object?> Values => _values.Values;

    public object? this[string key] => _values.TryGetValue(key, out var value) ? value : null;

    public void AddContext(object? context)
    {
        if (context is null)
            return;
        if (context is FormatterContextBag other) {
            foreach (var kv in other._values) {
                _values[kv.Key] = kv.Value;
                if (other._types.TryGetValue(kv.Key, out var type))
                    _types[kv.Key] = type;
            }
            return;
        }

        if (TryAddDictionary(context))
            return;

        foreach (var prop in context.GetType().GetProperties(PropertyFlags)) {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;
            try {
                _values[prop.Name] = prop.GetValue(context);
                _types[prop.Name] = UnwrapNullable(prop.PropertyType);
            }
            catch {
                // Skip indexer-like or throwing getters.
            }
        }
    }

    /// <summary>Expando copy for DynamicExpresso (better dynamic member/indexer support than a custom DynamicObject).</summary>
    public ExpandoObject ToExpando()
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;
        foreach (var kv in _values)
            dict[kv.Key] = kv.Value;
        return expando;
    }

    /// <summary>CLR type for an identifier, used when the stored value is null.</summary>
    public Type GetBindingType(string key)
    {
        if (_types.TryGetValue(key, out var type))
            return type;
        if (_values.TryGetValue(key, out var value) && value is not null)
            return value.GetType();
        return typeof(object);
    }

    private static Type UnwrapNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out object? value) => TryResolve(key, out value);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override bool TryGetMember(GetMemberBinder binder, out object? result) => TryResolve(binder.Name, out result);

    public override bool TryGetIndex(GetIndexBinder binder, object?[] indexes, out object? result)
    {
        result = null;
        if (indexes.Length != 1 || indexes[0] is not string key)
            return false;
        return TryResolve(key, out result);
    }

    /// <summary>Resolves a selector or dotted path against stored values, then nested members, then flat dotted keys.</summary>
    public bool TryResolve(string path, out object? value)
    {
        value = null;
        if (string.IsNullOrEmpty(path))
            return false;
        if (_values.TryGetValue(path, out value))
            return true;

        var segments = path.Split('.');
        if (segments.Length == 1)
            return TryResolveDottedPrefix(path, out value);

        object? current = this;
        foreach (var segment in segments) {
            if (!TryResolveOn(current, segment, out current) || current is null)
                return false;
        }

        value = current;
        return true;
    }

    private bool TryAddDictionary(object context)
    {
        if (context is IReadOnlyDictionary<string, object?> typed) {
            foreach (var kv in typed) {
                _values[kv.Key] = kv.Value;
                _types[kv.Key] = kv.Value?.GetType() ?? typeof(object);
            }
            return true;
        }

        if (context is IDictionary dictionary) {
            foreach (DictionaryEntry entry in dictionary) {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(key)) {
                    _values[key] = entry.Value;
                    _types[key] = entry.Value?.GetType() ?? typeof(object);
                }
            }

            return true;
        }

        return false;
    }

    private bool TryResolveDottedPrefix(string prefix, out object? value)
    {
        value = null;
        var nested = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var start = prefix + ".";
        foreach (var kv in _values) {
            if (!kv.Key.StartsWith(start, StringComparison.OrdinalIgnoreCase))
                continue;
            nested[kv.Key[start.Length..]] = kv.Value;
        }

        if (nested.Count == 0)
            return false;
        if (nested.Count == 1 && nested.TryGetValue(string.Empty, out value))
            return true;

        value = new FormatterContextBag();
        ((FormatterContextBag)value).TryAddDictionary(nested);
        return true;
    }

    private static bool TryResolveOn(object current, string segment, out object? value)
    {
        value = null;
        if (current is FormatterContextBag bag)
            return bag.TryResolve(segment, out value);

        if (current is IReadOnlyDictionary<string, object?> typed)
            return typed.TryGetValue(segment, out value);

        if (current is IDictionary dictionary) {
            foreach (DictionaryEntry entry in dictionary) {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                if (string.Equals(key, segment, StringComparison.OrdinalIgnoreCase)) {
                    value = entry.Value;
                    return true;
                }
            }

            return false;
        }

        var prop = current.GetType().GetProperty(segment, PropertyFlags);
        if (prop is null || !prop.CanRead || prop.GetIndexParameters().Length > 0)
            return false;
        try {
            value = prop.GetValue(current);
            return true;
        }
        catch {
            return false;
        }
    }
}
