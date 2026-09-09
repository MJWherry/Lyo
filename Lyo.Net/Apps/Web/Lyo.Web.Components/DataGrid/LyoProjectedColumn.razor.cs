using Lyo.Web.Components.Form;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoProjectedColumn
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

    /// <summary>Inline style for the header cell. Use with a width (for example <c>width: 90px;</c>) to keep narrow columns (bools, counts, ids) compact.</summary>
    [Parameter]
    public string? HeaderStyle { get; set; }

    /// <summary>CSS class for the header cell.</summary>
    [Parameter]
    public string? HeaderClass { get; set; }

    /// <summary>Inline style for body cells (for example <c>white-space: nowrap;</c> for timestamps).</summary>
    [Parameter]
    public string? CellStyle { get; set; }

    /// <summary>CSS class for body cells.</summary>
    [Parameter]
    public string? CellClass { get; set; }

    /// <summary>If true, the column starts hidden (and stays out of the query select) until the user shows it, unless visibility was restored from the client store.</summary>
    [Parameter]
    public bool HiddenByDefault { get; set; }

    /// <summary>
    /// If set, builds the cell text from the projected row and raw field value (same object <see cref="ProjectedValueHelper.GetValue" /> uses). Use when the default string
    /// representation is not enough (for example human-readable file size). Ignore the row with <c>_</c> if only the value matters.
    /// </summary>
    [Parameter]
    public Func<object?, object?, string>? FormattedValue { get; set; }

    /// <summary>
    /// If set, the cell renders <see cref="LyoTimestamp" /> for this field (UTC → browser time zone) instead of plain text / <see cref="FormattedValue" />.
    /// </summary>
    [Parameter]
    public LyoTimestampKind? Timestamp { get; set; }

    /// <summary>When true with <see cref="Timestamp" />, formats the latest value in a projected collection (for example last-run columns).</summary>
    [Parameter]
    public bool TimestampLatest { get; set; }

    /// <summary>± window forwarded to <see cref="LyoTimestamp.RelativeWindow" />. Default 24 hours.</summary>
    [Parameter]
    public TimeSpan TimestampRelativeWindow { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// If true, the cell renders compact <c>LyoIdField</c> (copy + suffix abbreviation) for this field instead of plain text.
    /// Ignored when <see cref="CellContent" /> or <see cref="Timestamp" /> is set.
    /// </summary>
    [Parameter]
    public bool Identifier { get; set; }

    /// <summary>
    /// Custom cell markup rendered instead of the default text (and instead of <see cref="FormattedValue" />). Receives the projected row object; read field values with
    /// <see cref="ProjectedValueHelper.GetValue" /> / <see cref="ProjectedValueHelper.GetDisplayValue" />. Quick-search highlighting is not applied to custom content.
    /// </summary>
    [Parameter]
    public RenderFragment<object?>? CellContent { get; set; }

    /// <summary>
    /// If set, default cell text longer than this is shown with an ellipsis; hover reveals the full value. Ignored when <see cref="CellContent" /> is set.
    /// </summary>
    [Parameter]
    public int? MaxDisplayLength { get; set; }

    /// <summary>
    /// Makes this column the heading of each card in the card layout instead of one more labelled field. Leave false to let the grid pick the most name-like column.
    /// </summary>
    [Parameter]
    public bool CardTitle { get; set; }

    protected override void OnParametersSet() => ColumnRegistry?.Register(Field, Title, QuickSearchPropertyName, HiddenByDefault, Cell, Identifier, CardTitle, Sortable);

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
