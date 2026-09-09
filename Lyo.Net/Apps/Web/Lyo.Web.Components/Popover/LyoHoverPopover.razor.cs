using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.Popover;

/// <summary>
/// Popover that opens on hover and, unlike a tooltip, survives the pointer moving onto it, so the content can be read, scrolled, selected and copied. Clicking the
/// anchor pins it open until the user closes it.
/// </summary>
/// <remarks>
/// Use <see cref="Text" /> for plain text and <see cref="PopoverContent" /> for markup (links, chips, a nested grid). Requires <c>MudPopoverProvider</c> in the host
/// layout, which MudBlazor already needs for menus and selects. The anchor is focusable, so keyboard users get the same content on focus.
/// </remarks>
public partial class LyoHoverPopover : IDisposable
{
    private CancellationTokenSource? _transitionCts;
    private bool _hoverOpen;
    private bool _pinned;

    /// <summary>Anchor content the user hovers. The popover positions itself against this element.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Plain text displayed when <see cref="PopoverContent" /> is not set.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>Markup displayed instead of <see cref="Text" />. Interactive content works here because the popover stays open while the pointer is over it.</summary>
    [Parameter]
    public RenderFragment? PopoverContent { get; set; }

    /// <summary>Optional heading above the content. A pinned popover always displays the header row so the close button has somewhere to live.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Hover dwell before opening. Keeps the popover from flashing while the pointer crosses the anchor.</summary>
    [Parameter]
    public int OpenDelayMs { get; set; } = 150;

    /// <summary>Grace period after the pointer leaves the anchor or the popover. This is the window the user has to travel onto the popover.</summary>
    [Parameter]
    public int CloseDelayMs { get; set; } = 300;

    [Parameter]
    public Origin AnchorOrigin { get; set; } = Origin.BottomLeft;

    [Parameter]
    public Origin TransformOrigin { get; set; } = Origin.TopLeft;

    /// <summary>How the popover reacts when it would draw off-screen. Flipping keeps long cell text visible near viewport edges.</summary>
    [Parameter]
    public OverflowBehavior OverflowBehavior { get; set; } = OverflowBehavior.FlipOnOpen;

    /// <summary>Position the popover with <c>fixed</c> instead of <c>absolute</c>. Set true inside scroll containers that clip the popover.</summary>
    [Parameter]
    public bool Fixed { get; set; }

    /// <summary>CSS max-width for the content box. Long values wrap instead of stretching across the viewport.</summary>
    [Parameter]
    public string MaxWidth { get; set; } = "32rem";

    /// <summary>CSS max-height for the content box; taller content scrolls inside the popover.</summary>
    [Parameter]
    public string MaxHeight { get; set; } = "22rem";

    /// <summary>Allow clicking the anchor to pin the popover open until it is explicitly closed. Set false when the anchor has its own click behaviour.</summary>
    [Parameter]
    public bool Pinnable { get; set; } = true;

    /// <summary>Suppresses the popover entirely and draws only the anchor.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Extra CSS classes for the popover surface.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Extra CSS classes for the inline anchor element.</summary>
    [Parameter]
    public string? AnchorClass { get; set; }

    /// <summary>Fires whenever the popover opens or closes, including hover-driven changes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private bool HasContent => PopoverContent is not null || !string.IsNullOrWhiteSpace(Text);

    private bool IsOpen => !Disabled && HasContent && (_pinned || _hoverOpen);

    private string AnchorCssClass => string.IsNullOrWhiteSpace(AnchorClass) ? "lyo-hover-popover-anchor" : $"lyo-hover-popover-anchor {AnchorClass}";

    private string PopoverCssClass => string.IsNullOrWhiteSpace(Class) ? "lyo-hover-popover" : $"lyo-hover-popover {Class}";

    private string BodyStyle => $"max-width: {MaxWidth}; max-height: {MaxHeight};";

    public void Dispose()
    {
        _transitionCts?.Cancel();
        _transitionCts?.Dispose();
        _transitionCts = null;
    }

    /// <summary>Closes the popover and releases the pin.</summary>
    public async Task Close()
    {
        CancelPendingTransition();
        _pinned = false;
        await SetHoverOpen(false);
    }

    private void OnAnchorEnter() => ScheduleTransition(true, OpenDelayMs);

    private void OnAnchorLeave() => ScheduleTransition(false, CloseDelayMs);

    // Pointer reached the popover before the close grace period elapsed, so cancel the close and keep it open.
    private void OnPopoverEnter() => ScheduleTransition(true, 0);

    private void OnPopoverLeave() => ScheduleTransition(false, CloseDelayMs);

    private async Task OnAnchorClick()
    {
        if (Disabled || !HasContent || !Pinnable)
            return;

        CancelPendingTransition();
        _pinned = !_pinned;
        await SetHoverOpen(_pinned);
    }

    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
            await Close();
    }

    private void ScheduleTransition(bool open, int delayMs)
    {
        if (Disabled || !HasContent || (!open && _pinned))
            return;

        CancelPendingTransition();
        if (open == _hoverOpen)
            return;

        var cts = new CancellationTokenSource();
        _transitionCts = cts;
        _ = RunTransition(open, delayMs, cts.Token);
    }

    private async Task RunTransition(bool open, int delayMs, CancellationToken ct)
    {
        try {
            if (delayMs > 0)
                await Task.Delay(delayMs, ct);

            if (ct.IsCancellationRequested)
                return;

            await InvokeAsync(() => SetHoverOpen(open));
        }
        catch (OperationCanceledException) { }
    }

    private async Task SetHoverOpen(bool open)
    {
        if (_hoverOpen == open)
            return;

        _hoverOpen = open;
        StateHasChanged();
        await OpenChanged.InvokeAsync(IsOpen);
    }

    private void CancelPendingTransition()
    {
        _transitionCts?.Cancel();
        _transitionCts?.Dispose();
        _transitionCts = null;
    }
}
