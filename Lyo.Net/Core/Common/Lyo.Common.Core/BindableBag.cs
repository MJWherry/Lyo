using System.Collections;
using System.Reflection;
using Lyo.Exceptions;

namespace Lyo.Common.Core;

/// <summary>
/// String <see cref="ContextBag" /> that resolves a missing key from bound objects. <see cref="Bind" /> keeps the source and reads public properties on demand. A later
/// <see cref="Bind" /> wins. <see cref="ContextBag{TKey}.Set(TKey, object?)" /> shadows the bound object.
/// </summary>
public sealed class BindableBag : ContextBag
{
    private static readonly BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.Instance;
    private readonly bool _caseSensitive;
    private readonly List<object> _sources = [];

    /// <summary>Creates an empty bindable bag.</summary>
    /// <param name="caseSensitive"><see langword="false" /> matches property names without case. <see langword="true" /> uses ordinal names.</param>
    public BindableBag(bool caseSensitive = false)
        : base(caseSensitive)
        => _caseSensitive = caseSensitive;

    /// <summary>Invokes <paramref name="visit" /> for each readable public property of bound sources. Later sources overwrite earlier names. Local entries are not included.</summary>
    public void VisitBindings(Action<string, object?, Type> visit)
    {
        ArgumentHelpers.ThrowIfNull(visit);
        foreach (var source in _sources) {
            foreach (var property in source.GetType().GetProperties(PropertyFlags)) {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;

                object? value;
                try {
                    value = property.GetValue(source);
                }
                catch {
                    continue;
                }

                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                visit(property.Name, value, type);
            }
        }
    }

    /// <inheritdoc />
    public override void Import(ContextBag<string> source)
    {
        base.Import(source);
        if (source is not BindableBag bindable)
            return;

        foreach (var item in bindable._sources)
            _sources.Add(item);
    }

    /// <summary>Remembers <paramref name="source" />. A null source is ignored. Properties are read on lookup, not copied now.</summary>
    /// <returns>This bag.</returns>
    public BindableBag Bind(object? source)
    {
        if (IsFrozen)
            throw new ContextBagException("Context bag is frozen.");

        if (source is not null)
            _sources.Add(source);

        return this;
    }

    /// <summary>CLR type for <paramref name="key" />. A null property returns its property type with nullable unwrapped. A missing key returns <see cref="object" />.</summary>
    public Type GetBindingType(string key)
    {
        GuardKey(key);
        if (OwnsKey(key) && TryGetOwn(key, out var own) && own is not null)
            return own.GetType();

        if (TryFindProperty(key, out var property))
            return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return typeof(object);
    }

    /// <inheritdoc />
    public override bool TryGetValue(string key, out object? value)
    {
        GuardKey(key);
        if (TryGetOwn(key, out value))
            return true;

        if (TryGetBound(key, out value))
            return true;

        if (Parent is not null)
            return Parent.TryGetValue(key, out value);

        value = null;
        return false;
    }

    /// <inheritdoc />
    public override bool ContainsKey(string key)
    {
        GuardKey(key);
        if (OwnsKey(key) || TryFindProperty(key, out var _))
            return true;

        return Parent?.ContainsKey(key) == true;
    }

    /// <summary>Empty child that still reads this bag's bound properties and can bind its own sources.</summary>
    public new BindableBag CreateScope()
    {
        var child = new BindableBag(_caseSensitive);
        child.AttachTo(this);
        return child;
    }

    /// <summary>Read-only copy. Pending factories are not run. Bound sources stay live.</summary>
    public new BindableBag Freeze() => (BindableBag)base.Freeze();

    /// <inheritdoc />
    protected override ContextBag<string> CreateShell() => new BindableBag(_caseSensitive);

    /// <inheritdoc />
    protected override void CopyBindings(ContextBag<string> destination)
    {
        if (destination is not BindableBag bag)
            return;

        foreach (var source in _sources)
            bag._sources.Add(source);
    }

    /// <inheritdoc />
    protected override bool TryStep(object? current, string segment, out object? value)
    {
        if (current is ContextBag<string> or IReadOnlyDictionary<string, object?> or IDictionary)
            return base.TryStep(current, segment, out value);

        return TryReadProperty(current, segment, out value);
    }

    private bool TryGetBound(string key, out object? value)
    {
        for (var i = _sources.Count - 1; i >= 0; i--) {
            if (TryReadProperty(_sources[i], key, out value))
                return true;
        }

        value = null;
        return false;
    }

    private bool TryFindProperty(string key, out PropertyInfo property)
    {
        for (var i = _sources.Count - 1; i >= 0; i--) {
            if (TryMatch(_sources[i], key, out property))
                return true;
        }

        property = null!;
        return false;
    }

    private bool TryReadProperty(object? current, string segment, out object? value)
    {
        value = null;
        if (current is null || !TryMatch(current, segment, out var property))
            return false;

        try {
            value = property.GetValue(current);
            return true;
        }
        catch {
            return false;
        }
    }

    private bool TryMatch(object current, string name, out PropertyInfo property)
    {
        foreach (var candidate in current.GetType().GetProperties(PropertyFlags)) {
            if (!candidate.CanRead || candidate.GetIndexParameters().Length > 0)
                continue;

            if (!string.Equals(candidate.Name, name, KeyComparison))
                continue;

            property = candidate;
            return true;
        }

        property = null!;
        return false;
    }
}