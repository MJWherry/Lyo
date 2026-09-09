namespace Lyo.Web.Primitives.DataGrid;

/// <summary>
/// How <see cref="LyoDataGrid{T}" /> and <see cref="LyoDataGridProjected" /> present rows. Hosts pick the default through <c>AddLyoDataGrid</c>; users switch from the
/// grid toolbar and that choice is remembered per browser.
/// </summary>
public enum LyoDataGridLayout
{
    /// <summary>Table on wide screens, one card per row at or below the configured card breakpoint. That is what makes grids usable on phones.</summary>
    Auto = 0,

    /// <summary>Always the table, regardless of viewport. Columns scroll horizontally on narrow screens.</summary>
    Table = 1,

    /// <summary>Always one card per row, with each visible column rendered as a label plus value.</summary>
    Cards = 2
}
