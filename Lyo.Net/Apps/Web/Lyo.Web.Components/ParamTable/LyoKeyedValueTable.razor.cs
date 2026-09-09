using Lyo.Parameters;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamTable;

public partial class LyoKeyedValueTable
{
    /// <summary>Values to list, in any order. Draws <see cref="EmptyText" /> when null or empty.</summary>
    [Parameter]
    public IReadOnlyList<ILyoParameterValue>? Items { get; set; }

    /// <summary>Displays the type column as a <c>LyoTypeChip</c>. Turn off where every row shares one obvious type.</summary>
    [Parameter]
    public bool ShowType { get; set; } = true;

    /// <summary>Displays the description column. Turn off for value sets that never carry descriptions.</summary>
    [Parameter]
    public bool ShowDescription { get; set; } = true;

    /// <summary>Sorts rows by key. Off keeps the order given, which matters when the caller ordered by something meaningful.</summary>
    [Parameter]
    public bool SortByKey { get; set; } = true;

    /// <summary>Text displayed instead of the table when there is nothing to list.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "No parameters";

    /// <summary>Extra CSS classes for the table.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Extra CSS classes for key cells, typically a monospace utility.</summary>
    [Parameter]
    public string? KeyClass { get; set; }

    private IEnumerable<ILyoParameterValue> Sorted
        => SortByKey ? (Items ?? []).OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase) : Items ?? [];
}
