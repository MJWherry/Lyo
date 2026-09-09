using System.Linq.Expressions;
using Lyo.Common.Core;
using Lyo.Web.Components.Form;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

public partial class LyoPropertyColumn<T, TProperty>
{
    [CascadingParameter(Name = "LyoDataGridQuickSearchText")]
    public string? QuickSearchText { get; set; }

    [CascadingParameter(Name = "LyoDataGridPropertyColumnRegistry")]
    public ProjectedColumnRegistry? ColumnRegistry { get; set; }

    /// <summary>If set, this column participates in quick search. Property name used for API query.</summary>
    [Parameter]
    public string? QuickSearchPropertyName { get; set; }

    [Parameter]
    public Expression<Func<T, TProperty>>? Property { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public bool Sortable { get; set; } = true;

    [Parameter]
    public SortDirection InitialDirection { get; set; } = SortDirection.None;

    [Parameter]
    public bool Hideable { get; set; } = true;

    /// <summary>If true, the column starts hidden; the user can show it from the columns panel.</summary>
    [Parameter]
    public bool HiddenByDefault { get; set; }

    /// <summary>If true, the cell renders compact <c>LyoIdField</c> instead of the property's default string.</summary>
    [Parameter]
    public bool Identifier { get; set; }

    [Parameter]
    public bool Resizable { get; set; } = true;

    [Parameter]
    public Align Align { get; set; } = Align.Inherit;

    /// <summary>
    /// Makes this column the heading of each card in the card layout instead of one more labelled field. Leave false to let the grid pick the most name-like column.
    /// </summary>
    [Parameter]
    public bool CardTitle { get; set; }

    private bool _hidden;
    private bool _hiddenSeeded;

    protected override void OnParametersSet()
    {
        if (!_hiddenSeeded) {
            _hidden = HiddenByDefault;
            _hiddenSeeded = true;
        }

        ColumnRegistry?.Register(PropertyPath(Property) ?? "", Title, QuickSearchPropertyName, HiddenByDefault, Cell, Identifier, CardTitle, Sortable);
    }

    /// <summary>Property value as text for a row the grid hands over as <c>object?</c> (the card layout is not generic over <typeparamref name="T" />).</summary>
    private string DisplayText(object? item) => item is T typed && Property is not null ? Property.Compile().Invoke(typed)?.ToString() ?? string.Empty : string.Empty;

    private static string? PropertyPath(LambdaExpression? expr) => expr.TryGetMemberPath();

    private string EffectiveHeaderStyle => MergeAlignStyle(null, Align);

    private string EffectiveCellStyle => MergeAlignStyle(null, Align);

    private static string MergeAlignStyle(string? style, Align align)
        => align switch {
            Align.Center => AppendStyle(style, "text-align: center;"),
            Align.Right => AppendStyle(style, "text-align: right;"),
            Align.Left => AppendStyle(style, "text-align: left;"),
            var _ => style ?? string.Empty
        };

    private static string AppendStyle(string? style, string snippet) => string.IsNullOrEmpty(style) ? snippet : $"{style.TrimEnd()} {snippet}";
}
