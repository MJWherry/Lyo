using Lyo.Exceptions;

namespace Lyo.Web.Primitives.DataGrid;

/// <summary>
/// Host defaults for every <see cref="LyoDataGrid{T}" /> and <see cref="LyoDataGridProjected" />. Register with <c>AddLyoDataGrid</c>; hosts that never register it
/// receive <see cref="LyoDataGridLayout.Auto" /> with the toolbar toggle available.
/// </summary>
public sealed class LyoDataGridOptions
{
    /// <summary>Configuration section bound by <c>AddLyoDataGridFromConfiguration</c>.</summary>
    public const string SectionName = "DataGrid";

    /// <summary>Layout used until the user picks a different one. A stored user choice always beats this.</summary>
    public LyoDataGridLayout Layout { get; set; } = LyoDataGridLayout.Auto;

    /// <summary>Show the layout toggle in the grid toolbar. Set false to pin every grid onto <see cref="Layout" />.</summary>
    public bool AllowLayoutToggle { get; set; } = true;

    /// <summary>
    /// Widest viewport that still gets cards under <see cref="LyoDataGridLayout.Auto" />. <c>Sm</c> covers phones and small tablets; raise to <c>Md</c> to card
    /// tablets as well.
    /// </summary>
    public Breakpoint CardBreakpoint { get; set; } = Breakpoint.Sm;

    /// <summary>Throws when a value is not a declared enum member, which configuration binding can emit.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotDefined(Layout);
        ArgumentHelpers.ThrowIfNotDefined(CardBreakpoint);
    }
}
