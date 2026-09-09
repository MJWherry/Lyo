using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoTypedProjectedColumn<TValue>
{
    [CascadingParameter(Name = "LyoDataGridQuickSearchText")]
    public string? QuickSearchText { get; set; }

    [CascadingParameter(Name = "LyoDataGridProjectedColumnRegistry")]
    public ProjectedColumnRegistry? ColumnRegistry { get; set; }

    [CascadingParameter(Name = "LyoDataGridColumnVisibilityBinder")]
    public ColumnVisibilityBinder? VisibilityBinder { get; set; }

    /// <summary>Field name in the projected result (for example "Id", "FirstName", "Addresses.Count"). Used for display and API sort key.</summary>
    [Parameter]
    [EditorRequired]
    public required string Field { get; set; }

    /// <summary>Sort key sent to API. Default is Field. Use when API expects different name than Field.</summary>
    [Parameter]
    public string? SortKey { get; set; }

    /// <summary>If set, this column participates in quick search highlighting. Use the same property name as in QuickSearchProperties.</summary>
    [Parameter]
    public string? QuickSearchPropertyName { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public bool Sortable { get; set; } = true;

    [Parameter]
    public SortDirection InitialDirection { get; set; } = SortDirection.None;

    [Parameter]
    public bool Hideable { get; set; } = true;

    [Parameter]
    public bool Resizable { get; set; } = true;

    [Parameter]
    public Align Align { get; set; } = Align.Inherit;

    /// <summary>Inline style for the header cell.</summary>
    [Parameter]
    public string? HeaderStyle { get; set; }

    /// <summary>CSS class for the header cell.</summary>
    [Parameter]
    public string? HeaderClass { get; set; }

    /// <summary>Inline style for body cells.</summary>
    [Parameter]
    public string? CellStyle { get; set; }

    /// <summary>CSS class for body cells.</summary>
    [Parameter]
    public string? CellClass { get; set; }

    /// <summary>If true, the column starts hidden (and stays out of the query select) until the user shows it, unless visibility was restored from the client store.</summary>
    [Parameter]
    public bool HiddenByDefault { get; set; }

    /// <summary>
    /// If set, formats the cell from the field value alone (coerced to <typeparamref name="TValue" />). Prefer this over <see cref="FormattedValue" /> when you do not need the
    /// row.
    /// </summary>
    [Parameter]
    public Func<TValue, string>? Format { get; set; }

    /// <summary>
    /// When set (and <see cref="Format" /> is null), formats the cell using the projected row and the field value coerced to <typeparamref name="TValue" /> (see
    /// <see cref="ProjectedValueHelper.ConvertTo{T}" />).
    /// </summary>
    [Parameter]
    public Func<object?, TValue, string>? FormattedValue { get; set; }

    /// <summary>
    /// Makes this column the heading of each card in the card layout instead of one more labelled field. Leave false to let the grid pick the most name-like column.
    /// </summary>
    [Parameter]
    public bool CardTitle { get; set; }

    protected override void OnParametersSet() => ColumnRegistry?.Register(Field, Title, QuickSearchPropertyName, HiddenByDefault, Cell, false, CardTitle, Sortable);

    /// <summary>Formatted cell text for a projected row, preferring <see cref="Format" /> then <see cref="FormattedValue" /> then the raw display value.</summary>
    private string DisplayText(object? item)
    {
        if (Format is not null)
            return Format(ProjectedValueHelper.ConvertTo<TValue>(ProjectedValueHelper.GetValue(item, Field)));

        if (FormattedValue is not null)
            return FormattedValue(item, ProjectedValueHelper.ConvertTo<TValue>(ProjectedValueHelper.GetValue(item, Field)));

        return ProjectedValueHelper.GetDisplayValue(item, Field);
    }

    private void OnHiddenChanged(bool hidden) => VisibilityBinder?.SetHidden(Field, hidden);

    private object GetSortValue(object? item) => ProjectedValueHelper.GetValue(item, Field) ?? string.Empty;

    internal string GetApiSortKey() => SortKey ?? Field;

    private string EffectiveHeaderStyle => MergeAlignStyle(HeaderStyle, Align);

    private string EffectiveCellStyle => MergeAlignStyle(CellStyle, Align);

    private string EffectiveHeaderClass => MergeAlignClass(HeaderClass, Align);

    private string EffectiveCellClass => MergeAlignClass(CellClass, Align);

    private static string MergeAlignStyle(string? style, Align align)
        => align switch {
            Align.Center => AppendStyle(style, "text-align: center;"),
            Align.Right => AppendStyle(style, "text-align: right;"),
            Align.Left => AppendStyle(style, "text-align: left;"),
            var _ => style ?? string.Empty
        };

    private static string MergeAlignClass(string? cssClass, Align align)
        => align switch {
            Align.Center => AppendClass(cssClass, "lyo-dg-align-center"),
            Align.Right => AppendClass(cssClass, "lyo-dg-align-right"),
            var _ => cssClass ?? string.Empty
        };

    private static string AppendStyle(string? style, string snippet) => string.IsNullOrEmpty(style) ? snippet : $"{style.TrimEnd()} {snippet}";

    private static string AppendClass(string? cssClass, string extra) => string.IsNullOrWhiteSpace(cssClass) ? extra : $"{cssClass} {extra}";
}
