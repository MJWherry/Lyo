using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

public sealed class ProjectedColumnRegistry
{
    /// <summary>Column titles that make a good card heading, most specific first. Matched against the declared title, then the field leaf.</summary>
    private static readonly string[] PreferredTitleLabels = ["name", "key", "title", "subject", "label", "displayname", "fullname", "fulladdress", "filename", "email"];

    private readonly List<ColumnEntry> _columns = [];

    /// <param name="cell">Cell markup for a row. Supplied by the Lyo column components so the card layout can draw the same content as the table.</param>
    /// <param name="identifier">Whether the column displays an identifier, which keeps it from being chosen as a card heading.</param>
    /// <param name="cardTitle">Explicitly makes this column the card heading, overriding the automatic pick.</param>
    /// <param name="sortable">Whether the column can be sorted, which decides if it appears in the card layout's sort menu.</param>
    public void Register(
        string field, string? title, string? quickSearchPropertyName, bool hiddenByDefault = false, RenderFragment<object?>? cell = null, bool identifier = false,
        bool cardTitle = false, bool sortable = false)
        => _columns.Add(new(field, title, quickSearchPropertyName, hiddenByDefault, cell, identifier, cardTitle, sortable));

    public void Clear() => _columns.Clear();

    /// <summary>
    /// Columns the card layout draws, in declaration order and filtered to the visible ones. Only columns that supplied cell markup are included, so ad-hoc
    /// <c>TemplateColumn</c>s (row action buttons and the like) stay out of the cards. Exactly zero or one entry is marked <see cref="LyoGridCardColumn.IsTitle" />.
    /// </summary>
    /// <param name="visibleFieldNames">Fields currently displayed by the grid, or null to include every registered column.</param>
    public IReadOnlyList<LyoGridCardColumn> GetCardColumns(IEnumerable<string>? visibleFieldNames = null)
    {
        var visible = visibleFieldNames?.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = _columns
            .Where(c => c.Cell is not null && !string.IsNullOrWhiteSpace(c.Field))
            .Where(c => visible is null || visible.Count == 0 || visible.Contains(c.Field.Trim()))
            .DistinctBy(c => c.Field.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var titleField = PickTitleField(candidates);
        return candidates
            .Select(c => new LyoGridCardColumn {
                Field = c.Field.Trim(),
                Title = c.Title,
                Cell = c.Cell!,
                IsTitle = string.Equals(c.Field.Trim(), titleField, StringComparison.OrdinalIgnoreCase),
                Sortable = c.Sortable
            })
            .ToList();
    }

    /// <summary>
    /// Explicit <c>CardTitle</c> wins; otherwise the first column whose label reads like a name, ignoring identifier columns. Null means the card gets no heading.
    /// </summary>
    private static string? PickTitleField(List<ColumnEntry> candidates)
    {
        if (candidates.FirstOrDefault(c => c.CardTitle) is { } explicitTitle)
            return explicitTitle.Field.Trim();

        foreach (var preferred in PreferredTitleLabels) {
            var match = candidates.FirstOrDefault(c => !c.Identifier && Normalize(c.Title ?? Leaf(c.Field)) == preferred);
            if (match is not null)
                return match.Field.Trim();
        }

        return null;
    }

    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string Leaf(string field) => field.LastIndexOf('.') is var dot and >= 0 ? field[(dot + 1)..] : field;

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
    /// Placeholder listing the column titles (or property leaves) that quick search ORs, for example <c>Search Name, Type, Id</c>.
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
        if (!string.IsNullOrWhiteSpace(match?.Title))
            return match.Title.Trim();

        var name = property.Trim();
        var dot = name.LastIndexOf('.');
        return dot >= 0 ? name[(dot + 1)..] : name;
    }

    /// <summary>True when the last dotted segment is <c>Id</c> (for example <c>Id</c>, <c>JobDefinition.Id</c>).</summary>
    public static bool IsIdField(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return false;

        var name = field.Trim();
        var leaf = name.LastIndexOf('.') is var dot and >= 0 ? name[(dot + 1)..] : name;
        return leaf.Equals("Id", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ColumnEntry(
        string Field, string? Title, string? QuickSearchPropertyName, bool HiddenByDefault, RenderFragment<object?>? Cell, bool Identifier, bool CardTitle,
        bool Sortable);
}