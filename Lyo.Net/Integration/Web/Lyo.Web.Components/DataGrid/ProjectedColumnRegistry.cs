namespace Lyo.Web.Components.DataGrid;

public sealed class ProjectedColumnRegistry
{
    private readonly List<(string Field, string? Title, string? QuickSearchPropertyName, bool HiddenByDefault)> _columns = [];

    public void Register(string field, string? title, string? quickSearchPropertyName, bool hiddenByDefault = false)
        => _columns.Add((field, title, quickSearchPropertyName, hiddenByDefault));

    public void Clear() => _columns.Clear();

    public IEnumerable<string> GetSelectFields() => _columns.Select(c => c.Field).Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f!.Trim()).Distinct();

    /// <summary>Field paths for columns declared with <c>HiddenByDefault</c> (projected grid).</summary>
    public IEnumerable<string> GetFieldsHiddenByDefault() => _columns.Where(c => c.HiddenByDefault && !string.IsNullOrWhiteSpace(c.Field)).Select(c => c.Field.Trim()).Distinct();

    /// <summary>Returns select fields filtered to only those whose columns are visible. Pass null to get all fields.</summary>
    public IEnumerable<string> GetSelectFieldsFilteredByVisibility(IEnumerable<string>? visibleFieldNames)
    {
        var all = GetSelectFields().ToHashSet();
        if (visibleFieldNames == null || !visibleFieldNames.Any())
            return all;

        var visible = visibleFieldNames.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f!.Trim()).ToHashSet();
        return visible.Count == 0 ? all : all.Where(f => visible.Contains(f));
    }

    public IReadOnlyList<string> GetQuickSearchProperties()
        => _columns.Where(c => !string.IsNullOrWhiteSpace(c.QuickSearchPropertyName)).Select(c => c.QuickSearchPropertyName!.Trim()).Distinct().ToList();
}