using Lyo.Exceptions;

namespace Lyo.Seed;

/// <summary>State for one run: generated items by CLR type, the active transport, and persist helpers for <c>After</c> callbacks.</summary>
public sealed class SeedSession
{
    private readonly Dictionary<Type, List<object>> _generated = [];
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

    internal SeedSession(ISeedTransport transport, SeedOptions options)
    {
        Transport = transport;
        Options = options;
    }

    /// <summary>Persist backend used for this run.</summary>
    public ISeedTransport Transport { get; }

    /// <summary>Options the contributor already received in <see cref="SeedContributor.Configure"/>.</summary>
    public SeedOptions Options { get; }

    /// <summary>Items built for <typeparamref name="T"/> so far (root <c>Entity</c> plus <c>ForEach</c> children).</summary>
    public IReadOnlyList<T> Generated<T>()
        where T : class
        => _generated.TryGetValue(typeof(T), out var list) ? list.Cast<T>().ToArray() : [];

    /// <summary>Writes <paramref name="items"/> through <see cref="Transport"/> and records them as generated.</summary>
    public async Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
        where T : class
    {
        ArgumentHelpers.ThrowIfNull(items);
        if (items.Count == 0)
            return;

        await Transport.PersistAsync(items, ct).ConfigureAwait(false);
        Record(items);
    }

    internal void Record<T>(IReadOnlyList<T> items)
        where T : class
    {
        if (items.Count == 0)
            return;

        if (!_generated.TryGetValue(typeof(T), out var list)) {
            list = [];
            _generated[typeof(T)] = list;
        }

        foreach (var item in items)
            list.Add(item);

        var name = typeof(T).Name;
        _counts[name] = _counts.GetValueOrDefault(name) + items.Count;
    }

    internal IReadOnlyDictionary<string, int> Counts => _counts;
}
