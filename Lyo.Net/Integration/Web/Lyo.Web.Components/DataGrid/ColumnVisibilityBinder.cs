namespace Lyo.Web.Components.DataGrid;

/// <summary>Tracks which columns are hidden for the projected grid. Persisted in ClientStore.</summary>
public sealed class ColumnVisibilityBinder
{
    private readonly HashSet<string> _hiddenFields = [];
    private readonly bool _loadedGridStateFromStore;
    private readonly Func<ColumnVisibilityBinder, Task> _onChanged;
    private bool _defaultHiddenFromColumnsApplied;

    /// <param name="persistedHiddenIfAny">Saved hidden fields when <paramref name="loadedGridStateFromStore" /> is true; ignored when false.</param>
    /// <param name="loadedGridStateFromStore">True if any grid state was loaded from the client store for this grid.</param>
    public ColumnVisibilityBinder(HashSet<string>? persistedHiddenIfAny, Func<ColumnVisibilityBinder, Task> onChanged, bool loadedGridStateFromStore)
    {
        _onChanged = onChanged;
        _loadedGridStateFromStore = loadedGridStateFromStore;
        if (!loadedGridStateFromStore || persistedHiddenIfAny == null)
            return;

        foreach (var f in persistedHiddenIfAny)
            _hiddenFields.Add(f);
    }

    /// <summary>
    /// Applies <see cref="LyoProjectedColumn.HiddenByDefault" /> when no grid state was loaded from the store (first visit). Call once after column registration (e.g. first
    /// render).
    /// </summary>
    public void ApplyDefaultHiddenFromColumns(IEnumerable<string> fieldNames)
    {
        if (_loadedGridStateFromStore || _defaultHiddenFromColumnsApplied)
            return;

        _defaultHiddenFromColumnsApplied = true;
        var added = false;
        foreach (var f in fieldNames) {
            if (string.IsNullOrWhiteSpace(f))
                continue;

            if (_hiddenFields.Add(f.Trim()))
                added = true;
        }

        if (added)
            _ = _onChanged(this);
    }

    public bool IsHidden(string field) => !string.IsNullOrWhiteSpace(field) && _hiddenFields.Contains(field.Trim());

    public void SetHidden(string field, bool hidden)
    {
        if (string.IsNullOrWhiteSpace(field))
            return;

        var f = field.Trim();
        var wasHidden = _hiddenFields.Contains(f);
        if (hidden)
            _hiddenFields.Add(f);
        else
            _hiddenFields.Remove(f);

        if (wasHidden != hidden)
            _ = _onChanged(this);
    }

    public HashSet<string> GetHiddenFields() => [.._hiddenFields];

    public IEnumerable<string> GetVisibleFieldNames(IEnumerable<string> allFields)
        => allFields.Where(f => !string.IsNullOrWhiteSpace(f) && !_hiddenFields.Contains(f.Trim())).Select(f => f!.Trim());
}