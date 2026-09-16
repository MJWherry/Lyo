using System.Collections;
using System.Globalization;
using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;

namespace Lyo.Common.Core;

/// <summary>
/// Map of keys to values. A factory stored with <see cref="Set(TKey, Func{object?})" /> or <see cref="Set(TKey, Func{ContextBag{TKey}, object?})" /> runs on the first read and is
/// replaced by its result. <see cref="CreateScope" /> reads missing keys from its parent and writes only its own entries. One bag is used by a single execution. It is not thread-safe.
/// </summary>
/// <typeparam name="TKey">Key type. A null reference key is rejected.</typeparam>
public class ContextBag<TKey> : IReadOnlyDictionary<TKey, object?>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Slot> _slots;
    private readonly IEqualityComparer<TKey> _comparer;
    private readonly List<TKey> _reading = [];

    /// <summary>Parent consulted when this bag has no entry. A scope write does not change the parent.</summary>
    private ContextBag<TKey>? _parent;

    /// <summary>When true, <see cref="Set(TKey, object?)" />, <see cref="Remove" />, <see cref="Clear" />, and <see cref="Import" /> throw.</summary>
    private bool _frozen;

    /// <summary>Creates an empty bag. A null <paramref name="comparer" /> uses <see cref="EqualityComparer{T}.Default" />.</summary>
    /// <param name="comparer">Key comparer. Fixed for the life of the bag.</param>
    public ContextBag(IEqualityComparer<TKey>? comparer = null)
    {
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _slots = new Dictionary<TKey, Slot>(_comparer);
    }

    /// <summary>Number of entries stored on this bag. Parent entries are not included.</summary>
    public int Count => _slots.Count;

    /// <summary>Keys stored on this bag. Pending factories are not run. Parent keys are not included.</summary>
    public IEnumerable<TKey> Keys => _slots.Keys;

    /// <summary>Values stored on this bag. Pending factories run. Parent values are not included.</summary>
    public IEnumerable<object?> Values
    {
        get {
            foreach (var key in SnapshotKeys())
                yield return ReadLocal(key);
        }
    }

    /// <summary>Value for <paramref name="key" />, or null when the key is missing. A pending factory runs. A missing key is read from the parent.</summary>
    public object? this[TKey key] => TryGetValue(key, out var value) ? value : null;

    /// <summary>True when this bag rejects writes. A frozen bag still reads its parent, so a later parent <see cref="Set(TKey, object?)" /> is visible for keys this bag did not store.</summary>
    protected bool IsFrozen => _frozen;

    /// <summary>Parent consulted when this bag has no entry.</summary>
    protected ContextBag<TKey>? Parent => _parent;

    /// <summary>Stores <paramref name="value" /> under <paramref name="key" />, replacing any previous entry on this bag. A null literal is ambiguous with a factory overload; cast it to <see cref="object" />.</summary>
    public virtual void Set(TKey key, object? value)
    {
        GuardKey(key);
        ThrowIfFrozen();
        _slots[key] = Slot.FromValue(value);
    }

    /// <summary>Stores <paramref name="factory" /> under <paramref name="key" /> without running it. The first read replaces the factory with its result.</summary>
    /// <param name="key">Entry key.</param>
    /// <param name="factory">Produces the value. When it throws, the key is left unset.</param>
    public virtual void Set(TKey key, Func<object?> factory)
    {
        GuardKey(key);
        ArgumentHelpers.ThrowIfNull(factory);
        ThrowIfFrozen();
        _slots[key] = Slot.FromFactory(factory);
    }

    /// <summary>
    /// Stores <paramref name="factory" /> under <paramref name="key" /> without running it. The factory receives this bag and may read sibling keys. Reading <paramref name="key" />
    /// again while the factory is running throws <see cref="ContextBagException" /> and leaves the key unset.
    /// </summary>
    public virtual void Set(TKey key, Func<ContextBag<TKey>, object?> factory)
    {
        GuardKey(key);
        ArgumentHelpers.ThrowIfNull(factory);
        ThrowIfFrozen();
        _slots[key] = Slot.FromBagFactory(factory);
    }

    /// <summary>Reads <paramref name="key" /> on this bag, then the parent. A pending factory runs once and its result is stored.</summary>
    /// <returns><see langword="false" /> when the key is missing.</returns>
    public virtual bool TryGetValue(TKey key, out object? value)
    {
        GuardKey(key);
        if (TryGetOwn(key, out value))
            return true;
        if (_parent is not null)
            return _parent.TryGetValue(key, out value);

        value = null;
        return false;
    }

    /// <summary>
    /// Reads <paramref name="key" /> and converts it with <see cref="TypeConversion.TryConvertTo{T}(object?, out T?, bool)" />.
    /// A missing key, a null stored for a non-nullable value type, or a failed conversion returns <see langword="false" />.
    /// A stored null is success for a nullable value type or a reference type.
    /// </summary>
    public bool TryGetAs<T>(TKey key, out T? value)
    {
        if (!TryGetValue(key, out var raw)) {
            value = default;
            return false;
        }

        if (raw is null) {
            var type = typeof(T);
            if (type.IsValueType && Nullable.GetUnderlyingType(type) is null) {
                value = default;
                return false;
            }

            value = default;
            return true;
        }

        return TypeConversion.TryConvertTo(raw, out value);
    }

    /// <summary>Returns the converted value. A missing key, a null stored for a non-nullable value type, or a failed conversion throws <see cref="ContextBagException" /> naming <paramref name="key" />.</summary>
    public T? GetRequired<T>(TKey key)
    {
        GuardKey(key);
        if (!ContainsKey(key))
            throw new ContextBagException($"Key '{key}' is missing.");
        if (!TryGetAs<T>(key, out var value))
            throw new ContextBagException($"Key '{key}' cannot be read as {typeof(T).Name}.");
        return value;
    }

    /// <summary>Returns the stored value, or stores and runs <paramref name="factory" /> when <paramref name="key" /> is missing on this bag and its parents.</summary>
    public object? GetOrAdd(TKey key, Func<object?> factory)
    {
        GuardKey(key);
        ArgumentHelpers.ThrowIfNull(factory);
        if (TryGetValue(key, out var existing))
            return existing;

        Set(key, factory);
        TryGetValue(key, out var created);
        return created;
    }

    /// <summary>Removes <paramref name="key" /> from this bag. A parent entry with the same key becomes visible again.</summary>
    /// <returns><see langword="true" /> when an entry was removed.</returns>
    public virtual bool Remove(TKey key)
    {
        GuardKey(key);
        ThrowIfFrozen();
        return _slots.Remove(key);
    }

    /// <summary>Removes every entry stored on this bag. Parent entries are left in place.</summary>
    public virtual void Clear()
    {
        ThrowIfFrozen();
        _slots.Clear();
    }

    /// <summary>Copies entries stored on this bag, including pending factories, without running them. The copy uses the same comparer and does not keep the parent link.</summary>
    public virtual ContextBag<TKey> Copy()
    {
        var copy = CreateShell();
        copy.ImportSlots(this);
        CopyBindings(copy);
        return copy;
    }

    /// <summary>Empty child. A missing key is read from this bag. <see cref="Set(TKey, object?)" /> on the child does not change this bag.</summary>
    public virtual ContextBag<TKey> CreateScope()
    {
        var child = CreateShell();
        child._parent = this;
        return child;
    }

    /// <summary>
    /// Read-only copy of this bag's entries. Pending factories are copied, not run. The frozen bag keeps this bag's parent, so a later parent write is visible for keys the frozen bag
    /// did not store. <see cref="Set(TKey, object?)" />, <see cref="Remove" />, <see cref="Clear" />, and <see cref="Import" /> throw <see cref="ContextBagException" />.
    /// </summary>
    public virtual ContextBag<TKey> Freeze()
    {
        var frozen = CreateShell();
        frozen._parent = _parent;
        frozen.ImportSlots(this);
        CopyBindings(frozen);
        frozen._frozen = true;
        return frozen;
    }

    /// <summary>
    /// Copies entries from <paramref name="source" /> into this bag without running factories.
    /// Keys are stored with this bag's comparer, so an ignore-case bag folds keys from a case-sensitive source.
    /// </summary>
    public virtual void Import(ContextBag<TKey> source)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ThrowIfFrozen();
        source.ForEachSlot((key, slot) => _slots[key] = slot);
    }

    /// <inheritdoc />
    public virtual bool ContainsKey(TKey key)
    {
        GuardKey(key);
        return _slots.ContainsKey(key) || _parent?.ContainsKey(key) == true;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, object?>> GetEnumerator()
    {
        foreach (var key in SnapshotKeys())
            yield return new KeyValuePair<TKey, object?>(key, ReadLocal(key));
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Empty bag with the same comparer and runtime kind.</summary>
    protected virtual ContextBag<TKey> CreateShell() => new(_comparer);

    /// <summary>Copies kind-specific links, such as bound objects, onto <paramref name="destination" />.</summary>
    protected virtual void CopyBindings(ContextBag<TKey> destination) { }

    /// <summary>Points a scope child at its parent.</summary>
    protected void AttachTo(ContextBag<TKey> parent)
    {
        ArgumentHelpers.ThrowIfNull(parent);
        _parent = parent;
    }

    /// <summary>Reads an entry stored on this bag, ignoring the parent. A pending factory runs.</summary>
    protected bool TryGetOwn(TKey key, out object? value)
    {
        if (_slots.TryGetValue(key, out var slot)) {
            value = Materialize(key, slot);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>True when this bag stored <paramref name="key" />. The parent is not consulted and factories are not run.</summary>
    protected bool OwnsKey(TKey key) => _slots.ContainsKey(key);

    /// <summary>Copies raw slots from <paramref name="source" />. Factories are not run.</summary>
    protected void ImportSlots(ContextBag<TKey> source) => source.ForEachSlot((key, slot) => _slots[key] = slot);

    /// <summary>Visits entries stored on this bag without running factories and without walking the parent.</summary>
    protected void ForEachSlot(Action<TKey, Slot> action)
    {
        foreach (var pair in _slots)
            action(pair.Key, pair.Value);
    }

    /// <summary>Rejects a null reference key. An empty string key is rejected.</summary>
    protected static void GuardKey(TKey key)
    {
        ArgumentHelpers.ThrowIfNull((object?)key);
        if (key is string text)
            ArgumentHelpers.ThrowIfNullOrEmpty(text);
    }

    private TKey[] SnapshotKeys()
    {
        var keys = new TKey[_slots.Count];
        _slots.Keys.CopyTo(keys, 0);
        return keys;
    }

    private object? ReadLocal(TKey key)
    {
        TryGetValue(key, out var value);
        return value;
    }

    private object? Materialize(TKey key, Slot slot)
    {
        if (!slot.IsFactory)
            return slot.Value;
        if (IsReading(key))
            throw new ContextBagException($"Key '{key}' is already being read.");

        _reading.Add(key);
        try {
            var value = slot.BagFactory is not null ? slot.BagFactory(this) : slot.Factory!();
            _slots[key] = Slot.FromValue(value);
            return value;
        }
        catch {
            _slots.Remove(key);
            throw;
        }
        finally {
            _reading.RemoveAt(_reading.Count - 1);
        }
    }

    private bool IsReading(TKey key)
    {
        foreach (var item in _reading) {
            if (_comparer.Equals(item, key))
                return true;
        }

        return false;
    }

    private void ThrowIfFrozen()
    {
        if (_frozen)
            throw new ContextBagException("Context bag is frozen.");
    }

    /// <summary>One stored entry. A factory is pending until the first read.</summary>
    protected readonly struct Slot(bool isFactory, object? value, Func<object?>? factory, Func<ContextBag<TKey>, object?>? bagFactory)
    {
        /// <summary>True when the entry is a factory that has not run.</summary>
        public bool IsFactory { get; } = isFactory;

        /// <summary>Stored value when <see cref="IsFactory" /> is false.</summary>
        public object? Value { get; } = value;

        /// <summary>Factory that takes no bag. Null when this is a value or a bag factory.</summary>
        public Func<object?>? Factory { get; } = factory;

        /// <summary>Factory that receives the bag. Null when this is a value or a value factory.</summary>
        public Func<ContextBag<TKey>, object?>? BagFactory { get; } = bagFactory;

        /// <summary>Entry that stores <paramref name="value" />.</summary>
        public static Slot FromValue(object? value) => new(false, value, null, null);

        /// <summary>Pending factory that closes over its own inputs.</summary>
        public static Slot FromFactory(Func<object?> factory) => new(true, null, factory, null);

        /// <summary>Pending factory that reads sibling keys from the bag.</summary>
        public static Slot FromBagFactory(Func<ContextBag<TKey>, object?> factory) => new(true, null, null, factory);
    }
}

/// <summary>
/// String-keyed <see cref="ContextBag{TKey}" />. <c>new ContextBag()</c> ignores case. Pass <c>caseSensitive: true</c> for ordinal keys.
/// An empty string key is rejected. <see cref="TryGetPath" /> walks nested bags and dictionaries. <see cref="Prefix" /> reads and writes dotted keys on this bag.
/// </summary>
public class ContextBag : ContextBag<string>
{
    private readonly bool _caseSensitive;

    /// <summary>Creates an empty string bag.</summary>
    /// <param name="caseSensitive"><see langword="false" /> uses ordinal ignore-case keys. <see langword="true" /> uses ordinal keys.</param>
    public ContextBag(bool caseSensitive = false)
        : base(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
        => _caseSensitive = caseSensitive;

    /// <summary>Ordinal or ordinal-ignore-case comparison, matching this bag's constructor.</summary>
    protected StringComparison KeyComparison => _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <inheritdoc />
    protected override ContextBag<string> CreateShell() => new ContextBag(_caseSensitive);

    /// <summary>Reads a dotted path through nested <see cref="ContextBag{TKey}" /> values and dictionaries. Properties of other objects are not read.</summary>
    /// <returns><see langword="false" /> when any segment is missing.</returns>
    public virtual bool TryGetPath(string path, out object? value)
    {
        value = null;
        if (string.IsNullOrEmpty(path))
            return false;

        var segments = path.Split('.');
        if (!TryGetValue(segments[0], out var current))
            return false;

        for (var i = 1; i < segments.Length; i++) {
            if (!TryStep(current, segments[i], out current))
                return false;
        }

        value = current;
        return true;
    }

    /// <summary>View that reads and writes <c>prefix.key</c> on this bag.</summary>
    public ContextBag Prefix(string prefix)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(prefix);
        return new PrefixBag(this, prefix, _caseSensitive);
    }

    /// <summary>Reads <paramref name="segment" /> from a nested bag or dictionary. Other objects return false.</summary>
    protected virtual bool TryStep(object? current, string segment, out object? value)
    {
        value = null;
        switch (current) {
            case null:
                return false;
            case ContextBag<string> bag:
                return bag.TryGetValue(segment, out value);
            case IReadOnlyDictionary<string, object?> typed:
                return typed.TryGetValue(segment, out value);
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary) {
                    var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                    if (string.Equals(key, segment, KeyComparison)) {
                        value = entry.Value;
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private sealed class PrefixBag : ContextBag
    {
        private readonly ContextBag _root;
        private readonly string _prefix;

        public PrefixBag(ContextBag root, string prefix, bool caseSensitive)
            : base(caseSensitive)
        {
            _root = root;
            _prefix = prefix;
        }

        public override void Set(string key, object? value) => _root.Set(Map(key), value);

        public override void Set(string key, Func<object?> factory) => _root.Set(Map(key), factory);

        public override void Set(string key, Func<ContextBag<string>, object?> factory) => _root.Set(Map(key), factory);

        public override bool TryGetValue(string key, out object? value) => _root.TryGetValue(Map(key), out value);

        public override bool ContainsKey(string key) => _root.ContainsKey(Map(key));

        public override bool Remove(string key) => _root.Remove(Map(key));

        private string Map(string key)
        {
            GuardKey(key);
            return _prefix + "." + key;
        }
    }
}
