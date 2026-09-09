using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Control cluster above a table, editor, or panel body. Shorthand slots (<see cref="Start" />, <see cref="ChildContent" />, <see cref="End" />) draw one row.
/// <see cref="Rows" /> draws any number of <see cref="LyoToolbarRow" /> children instead. Unlike <c>MudToolBar</c> this has no fixed height and no surface of its
/// own, which is what lets it sit inside a <see cref="LyoSection" /> without doubling the padding.
/// </summary>
/// <remarks>
/// Wrappers (<see cref="LyoToolbarButton" />, <see cref="LyoToolbarIconButton" />, <see cref="LyoToolbarMenu" />, <see cref="LyoToolbarField" />) pick up
/// <see cref="Disabled" /> through a cascade. Raw MudBlazor controls remain legal inside a group.
/// <code>
/// &lt;LyoToolbar Dense="true" Disabled="@_loading"&gt;
///     &lt;Rows&gt;
///         &lt;LyoToolbarRow&gt;
///             &lt;LyoToolbarGroup&gt;
///                 &lt;LyoToolbarButton Icon="@Icons.Material.Filled.Add" Text="New" OnClick="AddAsync"/&gt;
///             &lt;/LyoToolbarGroup&gt;
///             &lt;LyoToolbarGroup AlignEnd="true"&gt;
///                 &lt;LyoToolbarIconButton Icon="@Icons.Material.Filled.Refresh" Tooltip="Refresh" OnClick="ReloadAsync"/&gt;
///             &lt;/LyoToolbarGroup&gt;
///         &lt;/LyoToolbarRow&gt;
///     &lt;/Rows&gt;
/// &lt;/LyoToolbar&gt;
/// </code>
/// </remarks>
public partial class LyoToolbar
{
    private int _collapsibleCount;
    private bool _toggleClaimed;

    /// <summary>Leading actions, normally the primary button and bulk operations. Ignored when <see cref="Rows" /> is set.</summary>
    [Parameter]
    public RenderFragment? Start { get; set; }

    /// <summary>Middle content that takes the remaining width, normally search and filter inputs. Ignored when <see cref="Rows" /> is set.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Trailing actions pinned to the end, normally refresh, export, and view toggles. Ignored when <see cref="Rows" /> is set.</summary>
    [Parameter]
    public RenderFragment? End { get; set; }

    /// <summary>
    /// Explicit rows. When set, <see cref="Start" />, <see cref="ChildContent" />, and <see cref="End" /> are not rendered. Put <see cref="LyoToolbarRow" />
    /// children inside.
    /// </summary>
    [Parameter]
    public RenderFragment? Rows { get; set; }

    /// <summary>Tightens padding and gaps, for a toolbar inside a dialog or a nested panel.</summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>Disables every wrapper that reads the cascade. Per-control <c>Disabled</c> still ORs in.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>When false, every <see cref="LyoToolbarRow.Collapsible" /> row is hidden. Default true.</summary>
    [Parameter]
    public bool RowsExpanded { get; set; } = true;

    /// <summary>Raised when the shared collapse chevron toggles <see cref="RowsExpanded" />.</summary>
    [Parameter]
    public EventCallback<bool> RowsExpandedChanged { get; set; }

    /// <summary>Shows the unfold chevron when at least one row is collapsible. On by default.</summary>
    [Parameter]
    public bool ShowRowToggle { get; set; } = true;

    /// <summary>CSS class forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the wrapper.</summary>
    [Parameter]
    public string? Style { get; set; }

    internal bool UseRows => Rows is not null;

    internal bool HasCollapsibleRows => _collapsibleCount > 0;

    internal bool ShowCollapseToggle => ShowRowToggle && HasCollapsibleRows;

    internal string RootCssClass
    {
        get
        {
            _toggleClaimed = false;
            return LyoToolbarLayout.Root(Dense);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Rows is not null && (Start is not null || ChildContent is not null || End is not null))
            Debug.WriteLine("LyoToolbar: Rows is set; Start, ChildContent, and End are ignored.");
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        // Children register Collapsible during this render; the first visible row can only claim the chevron on the next pass.
        if (firstRender && HasCollapsibleRows)
            StateHasChanged();
    }

    internal void RegisterRow(bool collapsible)
    {
        if (collapsible)
            _collapsibleCount++;
    }

    internal void UnregisterRow(bool collapsible)
    {
        if (collapsible && _collapsibleCount > 0)
            _collapsibleCount--;
    }

    internal bool TryClaimRowToggle()
    {
        if (!ShowCollapseToggle || _toggleClaimed)
            return false;
        _toggleClaimed = true;
        return true;
    }

    internal Task ToggleRowsExpandedAsync()
    {
        RowsExpanded = !RowsExpanded;
        return RowsExpandedChanged.InvokeAsync(RowsExpanded);
    }
}
