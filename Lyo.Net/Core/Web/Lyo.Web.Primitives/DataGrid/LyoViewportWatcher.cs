namespace Lyo.Web.Primitives.DataGrid;

/// <summary>
/// Tracks the browser breakpoint for a component so layouts can switch between wide and narrow presentations. Wraps MudBlazor's <c>IBrowserViewportService</c>
/// subscription lifecycle, and stays inert when the host never registered MudBlazor services.
/// </summary>
/// <remarks>
/// Start it from <c>OnAfterRenderAsync(firstRender: true)</c>; the callback fires immediately with the current breakpoint and again on every resize. Until the first
/// callback arrives <see cref="IsAtOrBelow" /> returns false, so components render their wide layout during prerender instead of flashing the narrow one.
/// </remarks>
public sealed class LyoViewportWatcher : IAsyncDisposable
{
    private readonly Guid _observerId = Guid.NewGuid();
    private IBrowserViewportService? _service;

    /// <summary>Latest breakpoint reported by the browser, or <see cref="Breakpoint.None" /> before the first report.</summary>
    public Breakpoint Current { get; private set; } = Breakpoint.None;

    public async ValueTask DisposeAsync()
    {
        if (_service is null)
            return;

        var service = _service;
        _service = null;
        try {
            await service.UnsubscribeAsync(_observerId);
        }
        catch (Exception) {
            // Circuit already gone; the subscription dies with it.
        }
    }

    /// <summary>Subscribes to breakpoint changes and invokes <paramref name="onChanged" /> on the current breakpoint and every later change.</summary>
    /// <param name="service">Resolved with <c>GetService</c> by callers so hosts without MudBlazor services keep working; pass null to no-op.</param>
    /// <param name="onChanged">Invoked on the renderer's context; typically re-renders the component.</param>
    public async Task StartAsync(IBrowserViewportService? service, Func<Task> onChanged)
    {
        if (service is null || _service is not null)
            return;

        _service = service;
        try {
            await service.SubscribeAsync(
                _observerId, async args => {
                    Current = args.Breakpoint;
                    await onChanged();
                }, fireImmediately: true);
        }
        catch (Exception) {
            _service = null;
        }
    }

    /// <summary>True when the current viewport is at or below <paramref name="reference" /> (for example <c>Sm</c> matches phones and small tablets).</summary>
    public bool IsAtOrBelow(Breakpoint reference) => Rank(Current) is { } current && Rank(reference) is { } limit && current <= limit;

    private static int? Rank(Breakpoint breakpoint)
        => breakpoint switch {
            Breakpoint.Xs => 0,
            Breakpoint.Sm or Breakpoint.SmAndDown => 1,
            Breakpoint.Md or Breakpoint.MdAndDown => 2,
            Breakpoint.Lg or Breakpoint.LgAndDown => 3,
            Breakpoint.Xl or Breakpoint.XlAndDown => 4,
            Breakpoint.Xxl => 5,
            var _ => null
        };
}
