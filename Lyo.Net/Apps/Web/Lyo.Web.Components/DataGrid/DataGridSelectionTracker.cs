using Lyo.Exceptions;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Cross-page row selection keyed by <c>object[]</c> identity (the same shape as query <c>Keys</c>). MudBlazor's <c>SelectedItems</c> is a <see cref="HashSet{T}" /> of row
/// instances, and server grids mint new instances on every load, so checkboxes and bulk actions drift unless keys are the store and the page set is derived from them.
/// </summary>
public sealed class DataGridSelectionTracker
{
    private readonly List<object[]> _keys = [];

    /// <summary>Selected keys across every page, in the order they were added.</summary>
    public IReadOnlyList<object[]> Keys => _keys;

    /// <summary>Number of selected keys.</summary>
    public int Count => _keys.Count;

    /// <summary>True when <paramref name="key" /> is in the selection. Empty keys never match.</summary>
    public bool Contains(object[]? key) => key is { Length: > 0 } && IndexOf(key) >= 0;

    /// <summary>Replaces the entire selection, used when restoring persisted grid state.</summary>
    public void ReplaceAll(IEnumerable<object[]>? keys)
    {
        _keys.Clear();
        if (keys is null)
            return;

        foreach (var key in keys)
            AddIfMissing(key);
    }

    /// <summary>
    /// Drops every key that appears on <paramref name="pageItems" />, then adds keys of <paramref name="selectedOnPage" /> that still belong to that page. Call this from a
    /// user checkbox event, never from a server reload — Mud often clears or select-alls the page while data is in flight.
    /// </summary>
    public void ReplacePage<T>(IEnumerable<T> pageItems, IEnumerable<T> selectedOnPage, Func<T, object[]> keySelector)
    {
        ArgumentHelpers.ThrowIfNull(pageItems);
        ArgumentHelpers.ThrowIfNull(selectedOnPage);
        ArgumentHelpers.ThrowIfNull(keySelector);

        var pageKeys = new List<object[]>();
        foreach (var row in pageItems) {
            var key = keySelector(row);
            RemoveMatching(key);
            if (key is { Length: > 0 })
                pageKeys.Add(key);
        }

        foreach (var item in selectedOnPage) {
            var key = keySelector(item);
            if (key is not { Length: > 0 })
                continue;

            if (pageKeys.Any(pageKey => KeysEqual(pageKey, key)))
                AddIfMissing(key);
        }
    }

    /// <summary>Current-page rows whose keys are selected. Always a new set of the instances in <paramref name="pageItems" />, never leftover objects from a previous load.</summary>
    public HashSet<T> ItemsOnPage<T>(IEnumerable<T> pageItems, Func<T, object[]> keySelector)
    {
        ArgumentHelpers.ThrowIfNull(pageItems);
        ArgumentHelpers.ThrowIfNull(keySelector);

        var result = new HashSet<T>();
        foreach (var item in pageItems) {
            if (Contains(keySelector(item)))
                result.Add(item);
        }

        return result;
    }

    /// <summary>Adds <paramref name="key" /> when it is missing, or removes it when it is present.</summary>
    public void Toggle(object[]? key)
    {
        if (key is not { Length: > 0 })
            return;

        if (!RemoveMatching(key))
            _keys.Add(Copy(key));
    }

    /// <summary>Drops each of <paramref name="keys" /> from the selection.</summary>
    public void Remove(IEnumerable<object[]> keys)
    {
        ArgumentHelpers.ThrowIfNull(keys);
        foreach (var key in keys)
            RemoveMatching(key);
    }

    /// <summary>Clears every selected key.</summary>
    public void Clear() => _keys.Clear();

    private void AddIfMissing(object[]? key)
    {
        if (key is not { Length: > 0 } || Contains(key))
            return;

        _keys.Add(Copy(key));
    }

    private bool RemoveMatching(object[]? key)
    {
        var index = IndexOf(key);
        if (index < 0)
            return false;

        _keys.RemoveAt(index);
        return true;
    }

    private int IndexOf(object[]? key)
    {
        if (key is not { Length: > 0 })
            return -1;

        for (var i = 0; i < _keys.Count; i++) {
            if (KeysEqual(_keys[i], key))
                return i;
        }

        return -1;
    }

    private static bool KeysEqual(object[] left, object[] right) => left.Length == right.Length && left.SequenceEqual(right);

    private static object[] Copy(object[] key) => [.. key];
}
