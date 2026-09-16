using System.Collections;
using System.Dynamic;
using System.Globalization;
using Lyo.Common.Core;

namespace Lyo.Formatter;

/// <summary>
/// Case-insensitive context for SmartFormat and expressions. Resolves nested members and flat dotted keys
/// (<c>Docket.Number</c> on a dictionary) from one or more DTOs or dictionaries. Values live in a <see cref="BindableBag" />.
/// </summary>
internal sealed class FormatterContextBag : DynamicObject, IReadOnlyDictionary<string, object?>
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.OrdinalIgnoreCase);
    private readonly BindableBag _values = new();

    public FormatterContextBag() { }

    public FormatterContextBag(object? context) => AddContext(context);

    public int Count => Snapshot().Count;

    public IEnumerable<string> Keys => Snapshot().Keys;

    public IEnumerable<object?> Values => Snapshot().Values;

    public object? this[string key] => TryResolve(key, out var value) ? value : null;

    public void AddContext(object? context)
    {
        if (context is null)
            return;
        if (context is FormatterContextBag other) {
            _values.Import(other._values);
            foreach (var kv in other._types)
                _types[kv.Key] = kv.Value;
            return;
        }

        if (context is ContextBag bag) {
            _values.Import(bag);
            return;
        }

        if (TryAddDictionary(context))
            return;

        _values.Bind(context);
    }

    /// <summary>Expando copy for DynamicExpresso (better dynamic member/indexer support than a custom DynamicObject).</summary>
    public ExpandoObject ToExpando()
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;
        foreach (var kv in Snapshot())
            dict[kv.Key] = kv.Value;
        return expando;
    }

    /// <summary>CLR type for an identifier, used when the stored value is null.</summary>
    public Type GetBindingType(string key)
    {
        if (_types.TryGetValue(key, out var type))
            return type;
        return _values.GetBindingType(key);
    }

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out object? value) => TryResolve(key, out value);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Snapshot().GetEnumerator();

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
        return _values.TryGetPath(path, out value) && value is not null;
    }

    private bool TryAddDictionary(object context)
    {
        if (context is IReadOnlyDictionary<string, object?> typed) {
            foreach (var kv in typed) {
                _values.Set(kv.Key, kv.Value);
                _types[kv.Key] = kv.Value?.GetType() ?? typeof(object);
            }
            return true;
        }

        if (context is IDictionary dictionary) {
            foreach (DictionaryEntry entry in dictionary) {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(key))
                    continue;
                _values.Set(key, entry.Value);
                _types[key] = entry.Value?.GetType() ?? typeof(object);
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
        foreach (var key in _values.Keys) {
            if (!key.StartsWith(start, StringComparison.OrdinalIgnoreCase))
                continue;
            nested[key[start.Length..]] = _values[key];
        }

        if (nested.Count == 0)
            return false;
        if (nested.Count == 1 && nested.TryGetValue(string.Empty, out value))
            return true;

        value = new FormatterContextBag();
        ((FormatterContextBag)value).TryAddDictionary(nested);
        return true;
    }

    private Dictionary<string, object?> Snapshot()
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _values.VisitBindings((name, value, type) => {
            merged[name] = value;
            _types[name] = type;
        });
        foreach (var kv in _values)
            merged[kv.Key] = kv.Value;
        return merged;
    }
}
