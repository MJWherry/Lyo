using System.Linq.Expressions;
using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Column whose parent-row field is a foreign key. After the parent page loads, the grid batches a related QueryProject
/// (<c>Keys</c> = ids on the page) and this column renders typed <see cref="ChildContent" /> for each related <typeparamref name="TRes" />.
/// Use <see cref="Field" /> on projected grids (<typeparamref name="T" /> is <c>object?</c>) and <see cref="Property" /> on typed QueryConcrete grids.
/// </summary>
public partial class LyoIdColumn<T, TRes> where TRes : class
{
    [CascadingParameter(Name = "LyoDataGridProjectedColumnRegistry")]
    public ProjectedColumnRegistry? ProjectedRegistry { get; set; }

    [CascadingParameter(Name = "LyoDataGridPropertyColumnRegistry")]
    public ProjectedColumnRegistry? PropertyRegistry { get; set; }

    [CascadingParameter(Name = "LyoDataGridColumnVisibilityBinder")]
    public ColumnVisibilityBinder? VisibilityBinder { get; set; }

    [CascadingParameter(Name = "LyoDataGridRelatedColumnRegistry")]
    public RelatedColumnRegistry? RelatedRegistry { get; set; }

    [CascadingParameter(Name = "LyoDataGridRelatedEntityLookup")]
    public RelatedEntityLookup? Lookup { get; set; }

    /// <summary>Parent-row FK, for example <c>x => x.MostRecentAddressId</c>. When set, this path is used as <see cref="Field" />.</summary>
    [Parameter]
    public Expression<Func<T, object?>>? Property { get; set; }

    /// <summary>Parent projected or property path that holds the related entity's id (for example <c>MostRecentAddressId</c>).</summary>
    [Parameter]
    public string? Field { get; set; }

    /// <summary>Related entity QueryProject base route (for example <c>PersonAddress</c> → <c>POST PersonAddress/QueryProject</c>).</summary>
    [Parameter]
    [EditorRequired]
    public required string Route { get; set; }

    /// <summary>
    /// Related Select paths. When omitted, public scalar properties of <typeparamref name="TRes" /> are used. <c>Id</c> is always included.
    /// Paths must exist on the related QueryProject entity.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? Select { get; set; }

    /// <summary>Typed cell markup. Receives the related entity, or <c>null</c> when the FK is empty or the lookup missed. Default is compact <c>LyoIdField</c>.</summary>
    [Parameter]
    public RenderFragment<TRes?>? ChildContent { get; set; }

    /// <summary>Optional client for the related QueryProject. Defaults to the parent grid's <see cref="IApiClient" />.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Sort key sent to the parent API. Default is the FK path (not a related display field).</summary>
    [Parameter]
    public string? SortKey { get; set; }

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

    [Parameter]
    public string? HeaderStyle { get; set; }

    [Parameter]
    public string? HeaderClass { get; set; }

    [Parameter]
    public string? CellStyle { get; set; }

    [Parameter]
    public string? CellClass { get; set; }

    [Parameter]
    public bool HiddenByDefault { get; set; }

    [Parameter]
    public bool CardTitle { get; set; }

    private bool _hidden;
    private bool _hiddenSeeded;

    /// <summary>FK path used for registry, sort, and lookup.</summary>
    internal string EffectiveField => !string.IsNullOrWhiteSpace(Field) ? Field.Trim() : RelatedProjection.PropertyPath(Property) ?? string.Empty;

    private bool IsHidden => VisibilityBinder?.IsHidden(EffectiveField) ?? _hidden;

    protected override void OnParametersSet()
    {
        if (!_hiddenSeeded) {
            _hidden = HiddenByDefault;
            _hiddenSeeded = true;
        }

        var fkPath = EffectiveField;
        var select = RelatedProjection.InferSelect(typeof(TRes), Select);
        var hidden = VisibilityBinder?.IsHidden(fkPath) ?? _hidden;
        (ProjectedRegistry ?? PropertyRegistry)?.Register(fkPath, Title, null, HiddenByDefault, Cell, identifier: true, CardTitle, Sortable);
        RelatedRegistry?.Register(fkPath, Route, typeof(TRes), select, ApiClient, hidden);
    }

    private void OnHiddenChanged(bool hidden)
    {
        if (VisibilityBinder is not null)
            VisibilityBinder.SetHidden(EffectiveField, hidden);
        else
            _hidden = hidden;

        RelatedRegistry?.SetHidden(EffectiveField, hidden);
    }

    private object GetSortValue(T item) => RelatedProjection.GetFieldValue(item, EffectiveField) ?? string.Empty;

    internal string GetApiSortKey() => SortKey ?? EffectiveField;

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
