using Lyo.Web.Primitives.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
namespace Lyo.Web.Primitives;

/// <summary>
/// Shows a table on wide viewports and cards on narrow ones. Use it around hand-rolled tables (<c>MudSimpleTable</c>, <c>MudTable</c>) that cannot get the card
/// layout the shared data grids build in; pair <see cref="CardContent" /> with <see cref="LyoRecordCard" /> so the cards match the ones the grids already render.
/// </summary>
/// <remarks>
/// Follows the same preferences as the data grids: the host default from <c>AddLyoDataGrid</c>, then whatever layout the user last chose from a grid toolbar, then
/// the viewport. Hosts that register neither <c>AddLyoDataGrid</c> nor <see cref="ILyoLayoutPreferences" /> still work and simply follow the viewport.
/// </remarks>
public partial class LyoResponsiveTable : IDisposable
{
    private readonly LyoViewportWatcher _viewport = new();
    private LyoDataGridLayout? _userLayout;

    /// <summary>Existing table markup, rendered on wide viewports.</summary>
    [Parameter]
    public RenderFragment? TableContent { get; set; }

    /// <summary>Card markup for narrow viewports, typically a loop of <see cref="LyoRecordCard" />.</summary>
    [Parameter]
    public RenderFragment? CardContent { get; set; }

    /// <summary>Pins this table to one layout. Leave null to follow the host default and the user choice.</summary>
    [Parameter]
    public LyoDataGridLayout? Layout { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private LyoDataGridOptions GridOptions => Services.GetService<LyoDataGridOptions>() ?? new LyoDataGridOptions();

    private bool ShowCards
        => (Layout ?? _userLayout ?? GridOptions.Layout) switch {
            LyoDataGridLayout.Cards => true,
            LyoDataGridLayout.Table => false,
            var _ => _viewport.IsAtOrBelow(GridOptions.CardBreakpoint)
        };

    public void Dispose() => _ = _viewport.DisposeAsync();

    /// <summary>Local storage and the viewport are only reachable once the component is interactive, so both are read after the first render.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (!firstRender)
            return;

        await _viewport.StartAsync(Services.GetService<IBrowserViewportService>(), () => InvokeAsync(StateHasChanged));

        if (Services.GetService<ILyoLayoutPreferences>() is not { } preferences)
            return;

        try {
            _userLayout = await preferences.GetDataGridLayoutAsync();
        }
        catch (Exception) {
            return;
        }

        if (_userLayout is not null)
            StateHasChanged();
    }
}
