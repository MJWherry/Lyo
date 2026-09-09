using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// One wrapping flex row inside <see cref="LyoToolbar.Rows" />. Set <see cref="Collapsible" /> to hide the row when the parent <see cref="LyoToolbar.RowsExpanded" />
/// is false. Collapsed rows stay mounted so search fields keep their text and focus state.
/// </summary>
public partial class LyoToolbarRow : IDisposable
{
    /// <summary>Controls in this row. Usually <see cref="LyoToolbarGroup" /> children.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Hides this row when the parent toolbar is collapsed.</summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>CSS class forwarded onto the row.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline style forwarded onto the row.</summary>
    [Parameter]
    public string? Style { get; set; }

    [CascadingParameter]
    private LyoToolbar? Owner { get; set; }

    private bool _registered;
    private bool _registeredCollapsible;

    internal bool IsCollapsed => Collapsible && Owner is { RowsExpanded: false };

    internal string RowCssClass => LyoToolbarLayout.Row(IsCollapsed);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Owner is null)
            return;
        Owner.RegisterRow(Collapsible);
        _registered = true;
        _registeredCollapsible = Collapsible;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Owner is null || !_registered || _registeredCollapsible == Collapsible)
            return;
        Owner.UnregisterRow(_registeredCollapsible);
        Owner.RegisterRow(Collapsible);
        _registeredCollapsible = Collapsible;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_registered)
            Owner?.UnregisterRow(_registeredCollapsible);
    }
}
