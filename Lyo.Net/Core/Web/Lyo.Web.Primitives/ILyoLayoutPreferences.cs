using Lyo.Web.Primitives.DataGrid;

namespace Lyo.Web.Primitives;

/// <summary>
/// The table/card layout the user last picked, shared by <see cref="LyoResponsiveTable" /> and the data grids so that choice follows them from one grid to the next.
/// </summary>
/// <remarks>
/// Implemented by <c>ClientStore</c> in <c>Lyo.Web.Components</c>, which persists the value in browser local storage. Components resolve it with
/// <c>GetService</c> rather than injecting it, so hosts that never register an implementation keep working and simply follow the host default plus the viewport.
/// Register it alongside the store with <c>AddLyoClientStore</c>.
/// </remarks>
public interface ILyoLayoutPreferences
{
    /// <summary>Layout last chosen in a grid toolbar, or null when the user has never chosen one.</summary>
    Task<LyoDataGridLayout?> GetDataGridLayoutAsync();

    /// <summary>Persists the table/card choice for every grid and responsive table.</summary>
    Task SetDataGridLayoutAsync(LyoDataGridLayout layout);
}
