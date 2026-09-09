using Lyo.Exceptions;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Per-page cache of related entities keyed by QueryProject route, result type, and id. Cascaded into <c>LyoIdColumn</c> cells after the parent load.
/// </summary>
public sealed class RelatedEntityLookup
{
    private readonly Dictionary<(string Route, string Type, string Id), object> _items = [];

    /// <summary>Drops every cached related row. Called at the start of each parent page load.</summary>
    public void Clear() => _items.Clear();

    /// <summary>Stores a related entity for later cell lookup.</summary>
    public void Set(string route, Type resType, object id, object value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(route);
        ArgumentHelpers.ThrowIfNull(resType);
        ArgumentHelpers.ThrowIfNull(id);
        ArgumentHelpers.ThrowIfNull(value);
        var key = RelatedProjection.IdKey(id);
        if (key is null)
            return;

        _items[(route.Trim().TrimEnd('/'), TypeKey(resType), key)] = value;
    }

    /// <summary>Returns the related entity or <c>null</c> when the id is empty or the fetch missed it.</summary>
    public TRes? Get<TRes>(string route, object? id) where TRes : class => TryGet<TRes>(route, id, out var value) ? value : default;

    /// <summary>Resolves a related <typeparamref name="TRes" /> for <paramref name="id" /> on <paramref name="route" />.</summary>
    public bool TryGet<TRes>(string route, object? id, out TRes? value) where TRes : class
    {
        value = default;
        var key = RelatedProjection.IdKey(id);
        if (key is null || string.IsNullOrWhiteSpace(route))
            return false;

        if (!_items.TryGetValue((route.Trim().TrimEnd('/'), TypeKey(typeof(TRes)), key), out var boxed) || boxed is not TRes typed)
            return false;

        value = typed;
        return true;
    }

    private static string TypeKey(Type resType) => resType.FullName ?? resType.Name;
}
