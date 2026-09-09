using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// Card presentation of grid rows for narrow viewports: one card per row, each visible column rendered as a label and the very same cell markup the table uses.
/// Driven by <see cref="LyoDataGrid{T}" /> and <see cref="LyoDataGridProjected" />, which keep owning paging, sorting, selection and data loading.
/// </summary>
public partial class LyoDataGridCards
{
    /// <summary>Rows on the current page, already fetched by the grid.</summary>
    [Parameter]
    [EditorRequired]
    public required IReadOnlyList<object?> Items { get; set; }

    /// <summary>Visible columns from <see cref="ProjectedColumnRegistry.GetCardColumns" />, in declaration order.</summary>
    [Parameter]
    [EditorRequired]
    public required IReadOnlyList<LyoGridCardColumn> Columns { get; set; }

    /// <summary>Menu items for a row, shown behind the card's overflow button. Usually the same fragment the table's action column draws.</summary>
    [Parameter]
    public RenderFragment<object?>? RowMenu { get; set; }

    /// <summary>Show a selection checkbox on each card. Mirrors the grid's multi-selection feature.</summary>
    [Parameter]
    public bool Selectable { get; set; }

    /// <summary>Currently selected rows, used by each card's checkbox when <see cref="ItemIsSelected" /> is not set.</summary>
    [Parameter]
    public IReadOnlyCollection<object?>? SelectedItems { get; set; }

    /// <summary>Key-based selection check. Preferred over <see cref="SelectedItems" /> because projected rows are new objects after every load.</summary>
    [Parameter]
    public Func<object?, bool>? ItemIsSelected { get; set; }

    /// <summary>Raised with the row whose checkbox was clicked; the grid owns the resulting selection state.</summary>
    [Parameter]
    public EventCallback<object?> SelectionToggled { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public RenderFragment? NoRecordsContent { get; set; }

    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    private LyoGridCardColumn? TitleColumn => Columns.FirstOrDefault(c => c.IsTitle);

    private IEnumerable<LyoGridCardColumn> FieldColumns => Columns.Where(c => !c.IsTitle);

    private bool ShowHeader => TitleColumn is not null || Selectable || RowMenu is not null;

    private bool IsSelected(object? item) => ItemIsSelected?.Invoke(item) ?? SelectedItems?.Contains(item) == true;

    private string CardCssClass(object? item) => IsSelected(item) ? "lyo-dg-card lyo-dg-card-selected" : "lyo-dg-card";
}
