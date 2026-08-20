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

    /// <summary>
    /// Property paths OR-ed into quick search. Uses <paramref name="explicitProperties" /> when that list is non-empty; otherwise column
    /// <c>QuickSearchPropertyName</c> values. Always unions leaf <c>Id</c> fields so identifier paste-search works without per-grid wiring.
    /// </summary>
    public IReadOnlyList<string> GetQuickSearchProperties(IReadOnlyList<string>? explicitProperties = null)
    {
        var named = explicitProperties is { Count: > 0 }
            ? explicitProperties
            : _columns.Where(c => !string.IsNullOrWhiteSpace(c.QuickSearchPropertyName)).Select(c => c.QuickSearchPropertyName!.Trim());
        var ids = _columns.Where(c => IsIdField(c.Field)).Select(c => c.Field.Trim());
        return named.Concat(ids).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Placeholder listing the column titles (or property leaves) that quick search ORs, e.g. <c>Search Name, Type, Id</c>.
    /// Falls back to <c>Search...</c> when no properties are known yet.
    /// </summary>
    public string GetQuickSearchPlaceholder(IReadOnlyList<string>? explicitProperties = null)
    {
        var labels = GetQuickSearchProperties(explicitProperties)
            .Select(LabelForQuickSearch)
            .Where(static l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return labels.Count == 0 ? "Search..." : "Search " + string.Join(", ", labels);
    }

    private string LabelForQuickSearch(string property)
    {
        var match = _columns.FirstOrDefault(c =>
            c.Field.Equals(property, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(c.QuickSearchPropertyName)
                && c.QuickSearchPropertyName.Trim().Equals(property, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(match.Title))
            return match.Title.Trim();

        var name = property.Trim();
        var dot = name.LastIndexOf('.');
        return dot >= 0 ? name[(dot + 1)..] : name;
    }

    /// <summary>True when the last dotted segment is <c>Id</c> (e.g. <c>Id</c>, <c>JobDefinition.Id</c>).</summary>
    public static bool IsIdField(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return false;

        var name = field.Trim();
        var leaf = name.LastIndexOf('.') is var dot and >= 0 ? name[(dot + 1)..] : name;
        return leaf.Equals("Id", StringComparison.OrdinalIgnoreCase);
    }
}